using Leorik.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using static System.Formats.Asn1.AsnWriter;

namespace Leorik.Search
{
    public class IterativeSearch : ISearch
    {
        public const int MAX_PLY = 99;
        private const int MIN_ALPHA = -Evaluation.CheckmateScore;
        private const int MAX_BETA = Evaluation.CheckmateScore;
        private const int MAX_MOVES = 225; //https://www.stmintz.com/ccc/index.php?id=425058

        private readonly BoardState[] Positions;
        private readonly Move[] Moves;
        private readonly Move[] RootMoves;
        private readonly Move[] PrincipalVariations;
        private readonly History _history;
        private readonly KillerMoves _killers;
        private readonly StaticExchange _see = new();
        private readonly ulong[] _legacy; //hashes of positons that we need to eval as repetitions
        private readonly SearchOptions _options;

        private KillSwitch _killSwitch;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public bool Aborted { get; private set; }
        public Span<Move> PrincipalVariation => GetFirstPVfromBuffer(PrincipalVariations, Depth);

        public IterativeSearch(BoardState board, SearchOptions options, IEnumerable<BoardState> history)
        {
            _options = options;
            _killers = new KillerMoves(2);
            _history = new History();
            _legacy = SelectMoveHistory(history);

            Moves = new Move[MAX_PLY * MAX_MOVES];
            MoveGen moveGen = new(Moves, 0);
            moveGen.Collect(board);
            RootMoves = new Move[moveGen.Next];
            Array.Copy(Moves, RootMoves, RootMoves.Length);

            //PV-length = depth + (depth - 1) + (depth - 2) + ... + 1
            const int d = MAX_PLY + 1;
            PrincipalVariations = new Move[(d * d + d) / 2];

            //Initialize BoardState Stack
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Positions[0].Copy(board);
        }

        private static ulong[] SelectMoveHistory(IEnumerable<BoardState> history)
        {
            if(history == null)
                return Array.Empty<ulong>();

            List<ulong> reps = new();
            foreach (BoardState state in history) 
            {
                if (state.HalfmoveClock == 0)
                    reps.Clear();
                reps.Add(state.ZobristHash);
            }
            return reps.ToArray();
        }

        public void Search(int maxDepth)
        {
            while (Depth < maxDepth)
                SearchDeeper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ForcedCut(int depth)
        {
            return depth >= MAX_PLY - 1 || NodesVisited >= _options.MaxNodes || _killSwitch.Get();
        }

        private static Span<Move> GetFirstPVfromBuffer(Move[] pv, int depth)
        {
            //return moves until the first is 'default' move but not more than 'depth' number of moves
            int end = Array.IndexOf(pv, default, 0, depth);
            return new Span<Move>(pv, 0, end >= 0 ? end : depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexPV(int ply)
        {
            //Detailed Description:
            //https://github.com/lithander/Leorik/blob/b3236087fbc87e1915725c23ff349e46dfedd0f2/Leorik.Search/IterativeSearchNext.cs
            return Depth * ply - (ply * ply - ply) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtendPV(int ply, int remaining, Move move)
        {
            int index = IndexPV(ply);
            PrincipalVariations[index] = move;
            int stride = Depth - ply;
            int from = index + stride - 1;
            for (int i = 1; i < remaining; i++)
                PrincipalVariations[index + i] = PrincipalVariations[from + i];

            for (int i = remaining; i < stride; i++)
                PrincipalVariations[index + i] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TruncatePV(int ply)
        {
            PrincipalVariations[IndexPV(ply)] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            Depth++;
            _killers.Expand(Depth);
            _killSwitch = new KillSwitch(killSwitch);
            int score = EvaluateRoot(Depth);
            if(!Aborted)
                Score = (int)Positions[0].SideToMove * score;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickBestCapture(int first, int end)
        {
            //find the best move...
            int best = first;
            int bestScore = Moves[first].MvvLvaScore();
            for (int i = first + 1; i < end; i++)
            {
                int score = Moves[i].MvvLvaScore();
                if (score >= bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            //...swap best with first
            if (best != first)
            {
                (Moves[first], Moves[best]) = (Moves[best], Moves[first]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateNext(int ply, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, ref moveGen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateTT(int ply, int remaining, int alpha, int beta, ref MoveGen moveGen)
        {
            if (Aborted)
                return Positions[ply].RelativeScore();

            if (remaining <= 0)
                return EvaluateQuiet(ply, alpha, beta, moveGen);

            TruncatePV(ply);

            if (Positions[ply].HalfmoveClock > 99)
                return 0; //TODO: checkmate > draw?

            if (IsRepetition(ply))
                return 0; //TODO: is scoring *any* repetion as zero premature?

            ulong hash = Positions[ply].ZobristHash;
            if (Transpositions.GetScore(hash, remaining, ply, alpha, beta, out Move bm, out int ttScore))
                return ttScore;

            int score = Evaluate(ply, remaining, alpha, beta, moveGen, ref bm);

            if (!Aborted)
                Transpositions.Store(hash, remaining, ply, alpha, beta, score, bm);

            return score;
        }

        enum Stage { New, Captures, Killers, SortedQuiets, Quiets }

        struct PlayState
        {
            public Stage Stage;
            public int Next;
            public byte PlayedMoves;
            public PlayState(int next)
            {
                Next = next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Play(int ply, ref PlayState state, ref MoveGen moveGen)
        {
            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];

            while (true)
            {
                if (state.Next == moveGen.Next)
                {
                    switch (state.Stage)
                    {
                        case Stage.New:
                            state.Next = moveGen.CollectCaptures(current);
                            state.Stage = Stage.Captures;
                            continue;
                        case Stage.Captures:
                            state.Next = moveGen.CollectPlayableQuiets(current, _killers.GetSpan(ply));
                            state.Stage = Stage.Killers;
                            continue;
                        case Stage.Killers:
                            state.Next = moveGen.CollectQuiets(current);
                            state.Stage = Stage.SortedQuiets;
                            StripKillers(state.Next, ref moveGen, _killers.GetSpan(ply));
                            continue;
                        case Stage.SortedQuiets:
                        case Stage.Quiets:
                            return false;
                    }
                }

                if (state.Stage == Stage.Captures)
                {
                    PickBestCapture(state.Next, moveGen.Next);
                }
                else if (state.Stage == Stage.SortedQuiets)
                {
                    float historyValue = PickBestHistory(state.Next, moveGen.Next);
                    double historyThreshold = Math.Sqrt(state.PlayedMoves);
                    if (historyValue < historyThreshold)
                        state.Stage = Stage.Quiets;
                }

                if (next.Play(current, ref Moves[state.Next++]))
                {
                    state.PlayedMoves++;
                    return true;
                }
            }
        }
         
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsRepetition(int ply)
        {
            ulong hash = Positions[ply].ZobristHash;
            //start with the positions we've been searching
            for (int i = ply - 4; i >= 0; i -= 2)
            {
                if (Positions[i].ZobristHash == hash)
                    return true;

                //captures and pawn moves reset the halfmove clock for the purpose of enforcing the 50-move rule and also make a repetition impossible
                if (Positions[i].HalfmoveClock <= 1)
                    return false;
            }
            //continue with the history of positions from the startpos, truncated based on the half-move clock
            int start = _legacy.Length - 1 - (ply & 1);
            for (int i = start; i >= 0; i -= 2)
                if (_legacy[i] == hash)
                    return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StripKillers(int first, ref MoveGen moveGen, Span<Move> span)
        {
            for (int i = first; i < moveGen.Next; i++)
            {
                ref Move move = ref Moves[i];
                if (span[0] == move || span[1] == move)
                    Moves[i--] = Moves[--moveGen.Next];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float PickBestHistory(int first, int end)
        {
            //find the best move...
            int best = first;
            float bestScore = _history.Value(ref Moves[first]);
            for (int i = first + 1; i < end; i++)
            {
                float score = _history.Value(ref Moves[i]);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            //...swap best with first
            if (best != first)
            {
                (Moves[first], Moves[best]) = (Moves[best], Moves[first]);
            }
            return bestScore;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AllowNullMove(int ply)
        {
            //if the previous iteration found a mate we do the first few plys without null move to try and find the shortest mate or escape
            return !Evaluation.IsCheckmate(Score) || (ply > Depth / 4);
        }

        private int EvaluateRoot(int depth)
        {
            int eval = (int)Positions[0].SideToMove * Score;
            int window = 40;
            while (true)
            {
                int alpha = eval - window;
                int beta = eval + window;
                eval = EvaluateRoot(depth, alpha, beta);
                if (eval > alpha && eval < beta)
                    break;

                window *= 2;
            }
            return eval;
        }

        private int EvaluateRoot(int depth, int alpha, int beta)
        {
            NodesVisited++;

            BoardState root = Positions[0];
            BoardState next = Positions[1];
            MoveGen moveGen = new(Moves, 0);
            bool inCheck = root.InCheck();

            //init staged move generation and play all moves
            for(int i = 0; i < RootMoves.Length; i++)
            {
                Move move = RootMoves[i];
                if (!next.Play(root, ref move))
                    continue;

                //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                if (depth >= 2 && i > 0)
                {
                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    int R = (move.CapturedPiece() != Piece.None || inCheck || next.InCheck()) ? 0 : 2;

                    //Fail low but with BONUS!
                    if (EvaluateNext(0, depth - R, alpha, alpha + 1, moveGen) <= alpha)
                        continue;
                }

                //Scoring Root Moves with a random bonus: https://www.chessprogramming.org/Ronald_de_Man
                int score = EvaluateNext(0, depth, alpha, beta, moveGen);

                if (score > alpha)
                {
                    alpha = score;
                    ExtendPV(0, depth, move);
                    //promote new best move to the front
                    for (int j = i; j > 0; j--)
                        RootMoves[j] = RootMoves[j - 1];
                    RootMoves[0] = move;
                }
            }

            //checkmate or draw?
            if (alpha <= MIN_ALPHA)
                return root.InCheck() ? Evaluation.Checkmate(0) : 0;

            return alpha;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEndgame(BoardState board)
        {
            if (board.SideToMove == Color.White)
                return board.White == (board.White & (board.Kings | board.Pawns));
            else
                return board.Black == (board.Black & (board.Kings | board.Pawns));
        }

        private static LegalMoveCount legalMoves = new LegalMoveCount();

        private int Evaluate(int ply, int remaining, int alpha, int beta, MoveGen moveGen, ref Move bestMove)
        {
            NodesVisited++;

            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            bool inCheck = current.InCheck();
            int eval = current.RelativeScore();

            //JUST COUNTING MOVES BUT WITHOUT CHECKING THEIR LEGALITY!
            //--> MoveCount.HasMoves()

            //tc=10+0.1 CC=12
            //if (!inCheck && eval > beta && MoveCount.HasMoves(current, 8))
            //Score of Leorik-2.5.3a vs Leorik-2.5.1a: 1071 - 1259 - 2670  [0.481] 5000
            //...      Leorik-2.5.3a playing White: 637 - 547 - 1316  [0.518] 2500
            //...      Leorik-2.5.3a playing Black: 434 - 712 - 1354  [0.444] 2500
            //...      White vs Black: 1349 - 981 - 2670  [0.537] 5000
            //Elo difference: -13.1 +/- 6.6, LOS: 0.0 %, DrawRatio: 53.4 %

            //if (!inCheck && eval > beta && MoveCount.HasMoves(current, 9))
            //tc=10+0.1 CC=12
            //Score of Leorik-2.5.3a vs Leorik-2.5.1a: 1142 - 1144 - 2714  [0.500] 5000
            //...      Leorik-2.5.3a playing White: 677 - 463 - 1360  [0.543] 2500<
            //...      Leorik-2.5.3a playing Black: 465 - 681 - 1354  [0.457] 2500
            //...      White vs Black: 1358 - 928 - 2714  [0.543] 5000
            //Elo difference: -0.1 +/- 6.5, LOS: 48.3 %, DrawRatio: 54.3 %
            //tc=10+0.2 CC=20
            //Score of Leorik-2.5.3a vs Leorik-2.5.1a: 1303 - 1454 - 3243  [0.487] 6000
            //...      Leorik-2.5.3a playing White: 783 - 606 - 1611  [0.529] 3000
            //...      Leorik-2.5.3a playing Black: 520 - 848 - 1632  [0.445] 3000
            //...      White vs Black: 1631 - 1126 - 3243  [0.542] 6000
            //Elo difference: -8.7 +/- 6.0, LOS: 0.2 %, DrawRatio: 54.0 %

            //if (!inCheck && eval > beta && MoveCount.HasMoves(current, 10))
            //tc=10+0.1 CC=12
            //Score of Leorik-2.5.3a vs Leorik-2.5.1a: 1104 - 1157 - 2739  [0.495] 5000
            //...      Leorik-2.5.3a playing White: 617 - 500 - 1383  [0.523] 2500
            //...      Leorik-2.5.3a playing Black: 487 - 657 - 1356  [0.466] 2500
            //...      White vs Black: 1274 - 987 - 2739  [0.529] 5000
            //Elo difference: -3.7 +/- 6.5, LOS: 13.3 %, DrawRatio: 54.8 %

            //if (!inCheck && eval > beta && legalMoves.HasLegalMoves(current, 10))
            //tc=10+0.2 CC=12
            //Score of Leorik-2.5.3b vs Leorik-2.5.1a: 1057 - 1151 - 2792  [0.491] 5000
            //...      Leorik-2.5.3b playing White: 606 - 512 - 1382  [0.519] 2500
            //...      Leorik-2.5.3b playing Black: 451 - 639 - 1410  [0.462] 2500
            //...      White vs Black: 1245 - 963 - 2792  [0.528] 5000
            //Elo difference: -6.5 +/- 6.4, LOS: 2.3 %, DrawRatio: 55.8 %

            //consider null move pruning first
            if (!inCheck && eval > beta && legalMoves.HasLegalMoves(current, 10))
            //if (!inCheck && eval > beta && MoveCount.HasMoves(current, 8))
            //if (!inCheck && eval > beta && !IsEndgame(current) && AllowNullMove(ply))
            {
                //if remaining is [1..5] a nullmove reduction of 4 will mean it goes directly into Qsearch. Skip the effort for obvious situations...
                if (remaining < 6 && eval > beta + _options.NullMoveCutoff)
                    return beta;

                //if stm can skip a move and the position is still "too good" we can assume that this position, after making a move, would also fail high
                next.PlayNullMove(current);
                if (EvaluateNext(ply, remaining - 4, beta - 1, beta, moveGen) >= beta)
                    return beta;
            }

            //init staged move generation and play all moves
            PlayState playState = new(moveGen.Collect(bestMove));
            while (Play(ply, ref playState, ref moveGen))
            {
                //skip late quiet moves when almost in Qsearch depth
                if (!inCheck && playState.Stage == Stage.Quiets && remaining <= 2 && alpha == beta - 1)
                    return alpha;

                ref Move move = ref Moves[playState.Next - 1];
                _history.Played(remaining, ref move);

                //moves after the PV are searched with a null-window around alpha expecting the move to fail low
                if (remaining > 1 && playState.PlayedMoves > 1)
                {
                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    int R = 0;
                    if (!inCheck && playState.Stage >= Stage.Quiets && !next.InCheck())
                        R += 2;
                    //when not in check moves with a negative SEE score are reduced further
                    if (!inCheck && _see.IsBad(current, ref move))
                        R += 2;

                    //early out if reduced search doesn't beat alpha
                    if (EvaluateNext(ply, remaining - R, alpha, alpha + 1, moveGen) <= alpha)
                        continue;
                }

                //finally a full window search without reduction
                int score = EvaluateNext(ply, remaining, alpha, beta, moveGen);
                if (score <= alpha)
                    continue;

                alpha = score;
                bestMove = move;
                ExtendPV(ply, remaining, bestMove);

                if (playState.Stage >= Stage.Killers)
                {
                    _history.Good(remaining, ref bestMove);
                    _killers.Add(ply, bestMove);
                }

                //beta cutoff?
                if (score >= beta)
                    return beta;
            }

            //checkmate or draw?
            if (playState.PlayedMoves == 0)
                return inCheck ? Evaluation.Checkmate(ply) : 0;

            return alpha;
        }

        private int EvaluateQuiet(int ply, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;

            BoardState current = Positions[ply];
            bool inCheck = current.InCheck();
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = current.RelativeScore();

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                    alpha = standPatScore;
            }

            if (Aborted |= ForcedCut(ply))
                return current.RelativeScore();

            //To quiesce a position play all the Captures!
            BoardState next = Positions[ply + 1];
            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestCapture(i, moveGen.Next);

                //skip playing bad captures when not in check
                if (!inCheck && _see.IsBad(current, ref Moves[i]))
                    continue;

                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            if (!inCheck)
                return alpha;

            //Play Quiets only when in check!
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            return movesPlayed ? alpha : Evaluation.Checkmate(ply);
        }
    }
}