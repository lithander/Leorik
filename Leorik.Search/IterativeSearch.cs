using Leorik.Core;
using System.Runtime.CompilerServices;
using static Leorik.Core.Evaluation;

namespace Leorik.Search
{
    public class IterativeSearch : ISearch
    {
        public const int MAX_PLY = 99;
        private const int MAX_MOVES = 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private const int ASPIRATION_WINDOW = 40;
        private const float HISTORY_SCALE = 1.5f;
        private const int NORMALIZE_TO_PAWN_VALUE = 306;

        private readonly Move[] RootMoves;
        private readonly BoardState[] Positions;
        private readonly Move[] Moves;
        private readonly Move[] PrincipalVariations;
        private readonly int[] RootMoveOffsets;
        private readonly History _history;
        private readonly StaticExchange _see = new();
        private readonly ulong[] _legacy; //hashes of positons that we need to eval as repetitions
        private readonly SearchOptions _options;

        private long _bestMoveNodes;
        private long _totalNodes;
        private KillSwitch _killSwitch;

        private int Eval { get; set; }
        public int Score => IsCheckmate(Eval) ? Eval : (Eval * 100) / NORMALIZE_TO_PAWN_VALUE;
        public long NodesVisited { get; private set; }
        public float Stability { get; private set; }
        public int Depth { get; private set; }
        public bool Aborted { get; private set; }
        public Span<Move> PrincipalVariation => GetFirstPVfromBuffer(PrincipalVariations, Depth);

        public IterativeSearch(BoardState board, SearchOptions options, ulong[]? history, Move[]? moves)
        {
            _options = options;
            _history = new History();
            _legacy = history ?? Array.Empty<ulong>();

            Moves = new Move[MAX_PLY * MAX_MOVES];
            MoveGen moveGen = new(Moves, 0);
            if (moves?.Length > 0)
            {
                RootMoves = moves;
            }
            else
            {
                moveGen.CollectAll(board);
                RootMoves = new Move[moveGen.Next];
                Array.Copy(Moves, RootMoves, RootMoves.Length);
            }

            //PV-length = depth + (depth - 1) + (depth - 2) + ... + 1
            const int d = MAX_PLY + 1;
            PrincipalVariations = new Move[(d * d + d) / 2];

            //Initialize BoardState Stack
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Positions[0].Copy(board);

            //Initialize a random bonus added to each root move
            Random random = _options.Seed >= 0 ? new(_options.Seed) : Random.Shared;
            RootMoveOffsets = new int[RootMoves.Length];
            for (int i = 0; i < RootMoveOffsets.Length; i++)
                RootMoveOffsets[i] = random.Next(_options.Temperature);
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

        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            Depth++;

            _bestMoveNodes = 0;
            _totalNodes = 1;
            _killSwitch = new KillSwitch(killSwitch);

            int stm = (int)Positions[0].SideToMove;
            int eval = stm * Eval;
            int window = ASPIRATION_WINDOW;
            while (!Aborted)
            {
                //set aspiration window
                int alpha = eval - window;
                int beta = eval + window;
                
                eval = EvaluateRoot(Depth, alpha, beta);
                
                //result within aspiration window?
                if (eval > alpha && eval < beta)
                {
                    Eval = stm * eval;
                    Stability = _bestMoveNodes / (float)_totalNodes;
                    return;
                }

                window *= 2;
            }
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
            BoardState current = Positions[ply];

            if (Aborted |= ForcedCut(ply))
                return current.SideToMoveScore();

            //Mate distance pruning
            alpha = Math.Max(alpha, MatedScore(ply));
            beta = Math.Min(beta, MateScore(ply + 1));
            if (alpha >= beta)
                return beta;

            //Drop into QSearch
            if (remaining <= 0)
                return EvaluateQuiet(ply, alpha, beta, moveGen);

            TruncatePV(ply);

            //Handle draws!
            if (IsInsufficientMatingMaterial(current))
                return 0;

            if (current.HalfmoveClock > 99)
                return 0; //TODO: checkmate > draw?

            if (IsRepetition(ply))
                return 0; //TODO: is scoring *any* repetition as zero premature?

            //Transposition table lookup
            ulong hash = current.ZobristHash;
            if (Transpositions.GetScore(hash, remaining, ply, alpha, beta, out Move bm, out int ttScore))
                return ttScore;

            //Main Search!
            int score = Evaluate(ply, remaining, alpha, beta, moveGen, ref bm);
            if (Aborted)
                return score;

            //Update correction history!
            int staticEval = _history.GetAdjustedStaticEval(current);
            int delta = score - staticEval;
            if ((bm.CapturedPiece() == Piece.None) && //Best move either does not exist or is not a capture
                !IsCheckmate(score) &&                //checkmate scores are excluded!
                !(score <= alpha && delta > 0) &&     //fail-lows should not cause positive adjustment
                !(score >= beta && delta < 0) &&      //fail-highs should not cause negative adjustment
                !current.InCheck())                   //exclude positons that are in check!
            {
                _history.UpdateCorrection(current, remaining, delta);
            }

            //Update transposition table
            Transpositions.Store(hash, remaining, ply, alpha, beta, score, bm);

            return score;
        }

        enum Stage { Best, Captures, Killers, Continuation, SortedQuiets, Quiets }

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
                        case Stage.Best:
                            state.Next = moveGen.CollectCaptures(current);
                            state.Stage = Stage.Captures;
                            continue;
                        case Stage.Captures:
                            state.Next = moveGen.CollectQuiets(current);
                            state.Stage = Stage.Killers;
                            continue;
                        default:
                            return false;
                    }
                }

                switch (state.Stage)
                {
                    case Stage.Captures:
                        PickBestCapture(state.Next, moveGen.Next);
                        break;
                    case Stage.Killers:
                        PickKiller(ref state, ply, moveGen.Next);
                        break;
                    case Stage.Continuation:
                        PickContinuation(ref state, ply, moveGen.Next);
                        break;
                    case Stage.SortedQuiets:
                        PickBestHistory(ref state, moveGen.Next);
                        break;
                }

                if (next.Play(current, ref Moves[state.Next++]))
                {
                    state.PlayedMoves++;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickKiller(ref PlayState state, int ply, int end)
        {
            state.Stage = Stage.Continuation;

            if (PickMove(state.Next, end, _history.GetKiller(ply)))
                return;

            PickContinuation(ref state, ply, end);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickContinuation(ref PlayState state, int ply, int end)
        {
            for (int i = 0; i < History.ContDepth; i++)
                if (PickMove(state.Next, end, _history.GetContinuation(ply, i)))
                    return;

            PickBestHistory(ref state, end);
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
        private bool PickMove(int first, int end, Move move)
        {
            if (move == default)
                return false;

            //find the move...
            for (int i = first + 1; i < end; i++)
            {
                if (Moves[i] == move)
                {
                    //...swap best with first
                    (Moves[first], Moves[i]) = (Moves[i], Moves[first]);
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickBestHistory(ref PlayState state, int end)
        {
            //find the best move...
            int first = state.Next;
            int best = first;
            float bestScore = _history.Value(ref Moves[best]);
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

            float threshold = HISTORY_SCALE * state.PlayedMoves;
            state.Stage = bestScore > threshold ? Stage.SortedQuiets : Stage.Quiets;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AllowNullMove(int ply)
        {
            //if the previous iteration found a mate we do the first few plys without null move to try and find the shortest mate or escape
            return !IsCheckmate(Eval) || (ply > Depth / 4);
        }

        private int EvaluateRoot(int depth, int alpha, int beta)
        {
            NodesVisited++;

            BoardState root = Positions[0];
            BoardState next = Positions[1];
            MoveGen moveGen = new(Moves, 0);

            //init staged move generation and play all moves
            for (int i = 0; i < RootMoves.Length; i++)
            {
                Move move = RootMoves[i];
                if (!next.Play(root, ref move))
                    continue;
                                
                long preNodes = NodesVisited;

                //Scoring Root Moves with a random bonus: https://www.chessprogramming.org/Ronald_de_Man
                int bonus = IsCheckmate(Eval) ? 0 : RootMoveOffsets[i];
                //moves after the PV move are searched with a null-sized window and if non-tactical with reduced depth
                int R = (move.CapturedPiece() != Piece.None || next.InCheck()) ? 0 : 2;
                bool fullSearch = i == 0 || EvaluateNext(0, depth - R, alpha - bonus, alpha + 1 - bonus, moveGen) + bonus > alpha;
                int score = fullSearch ? EvaluateNext(0, depth, alpha - bonus, beta - bonus, moveGen) + bonus : alpha;

                _totalNodes += NodesVisited - preNodes;

                if (score > alpha)
                {
                    _bestMoveNodes = NodesVisited - preNodes;
                    alpha = score;
                    ExtendPV(0, depth, move);
                    PromoteBestMove(i);

                    if (score >= beta)
                        return beta;
                }
            }

            //checkmate or draw?
            if (alpha <= -CheckmateScore)
                return root.InCheck() ? MatedScore(0) : 0;

            return alpha;
        }

        private void PromoteBestMove(int i)
        {
            if (i <= 0) return; // already at the front

            Move move = RootMoves[i];
            int offset = RootMoveOffsets[i];
            //move all moves down one position overwriting best move at index i
            for (int j = i; j > 0; j--)
            {
                RootMoves[j] = RootMoves[j - 1];
                RootMoveOffsets[j] = RootMoveOffsets[j - 1];
            }
            //put the best move at the front
            RootMoves[0] = move;
            RootMoveOffsets[0] = offset;
        }

        private int Evaluate(int ply, int remaining, int alpha, int beta, MoveGen moveGen, ref Move bestMove)
        {
            NodesVisited++;

            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            bool inCheck = current.InCheck();
            int staticEval = _history.GetAdjustedStaticEval(current);

            //consider null move pruning first
            if (!inCheck && staticEval > beta && beta <= alpha + 1 && !current.IsEndgame() && AllowNullMove(ply))
            {
                //if remaining is [1..5] a nullmove reduction of 4 will mean it goes directly into Qsearch. Skip the effort for obvious situations...
                if (remaining < 6 && _history.IsExpectedFailHigh(staticEval, beta))
                    return beta;

                //if stm can skip a move and the position is still "too good" we can assume that this position, after making a move, would also fail high
                next.PlayNullMove(current);

                if (EvaluateNext(ply, remaining - 4, beta - 1, beta, moveGen) >= beta)
                    return beta;

                if (remaining >= 6)
                    _history.NullMovePass(staticEval, beta);
            }

            //init staged move generation and play all moves
            PlayState playState = new(moveGen.CollectMove(bestMove));
            while (Play(ply, ref playState, ref moveGen))
            {
                //skip late quiet moves when almost in Qsearch depth
                if (!inCheck && playState.Stage == Stage.Quiets && remaining <= 2 && alpha == beta - 1)
                    return alpha;

                ref Move move = ref Moves[playState.Next - 1];
                _history.Played(ply, remaining, ref move);

                //moves after the PV are searched with a null-window around alpha expecting the move to fail low
                if (!inCheck && remaining > 1 && playState.Stage > Stage.Best)
                {
                    int maxR = Math.Min(4, remaining - 1);
                    int R = 0;

                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    if (playState.Stage >= Stage.Quiets && !next.InCheck())
                        R = 2;

                    //a reduced quiet move that doesn't look promising in the static evaluation gets reduced further
                    if (R < maxR && R > 0 && -_history.GetAdjustedStaticEval(next) < staticEval)
                        R += 2;

                    //if it's not already a bad quiet move we may reduce because of bad SEE
                    if (R < maxR && _see.IsBad(current, ref move))
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
                _history.Good(ply, remaining, ref bestMove);

                //beta cutoff?
                if (score >= beta)
                    return beta;
            }

            //checkmate or draw?
            if (playState.PlayedMoves == 0)
                return inCheck ? MatedScore(ply) : 0;

            return alpha;
        }

        private int EvaluateQuiet(int ply, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;

            BoardState current = Positions[ply];

            if (IsInsufficientMatingMaterial(current))
                return 0;

            bool inCheck = current.InCheck();
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = _history.GetAdjustedStaticEval(current);

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                    alpha = standPatScore;
            }

            if (Aborted |= ForcedCut(ply))
                return current.SideToMoveScore();

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

            return movesPlayed ? alpha : MatedScore(ply);
        }
    }
}