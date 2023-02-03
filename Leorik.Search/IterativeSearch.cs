using Leorik.Core;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace Leorik.Search
{
    public struct SearchOptions
    {
        public byte MidgameRandomness;
        public byte EndgameRandomness;
        public byte FutilityMargin;
        public byte LateFutilityMargin;
        public long MaxNodes;

        internal int Randomness(float phase)
        {
            return (int)(MidgameRandomness + (EndgameRandomness - MidgameRandomness) * phase);
        }

        public static SearchOptions Default = new();

        public SearchOptions()
        {
            MaxNodes = long.MaxValue;
            MidgameRandomness = 0;
            EndgameRandomness = 0;
            FutilityMargin = 90;
            LateFutilityMargin = 50;
        }
    }

    public class IterativeSearch
    {
        public const int MAX_PLY = 99;

        private const int R_NULL_MOVE = 2; //how much do we reduce the search depth after passing a move? (null move pruning)
        private const int MIN_ALPHA = -Evaluation.CheckmateScore;
        private const int MAX_BETA = Evaluation.CheckmateScore;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058

        private BoardState[] Positions;
        private Move[] Moves;
        private int[] RootMoveOffsets;
        private Move[] PrincipalVariations;
        private KillSwitch _killSwitch;
        private History _history;
        private KillerMoves _killers;
        private StaticExchange _see = new StaticExchange();

        SearchOptions _options;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public bool Aborted { get; private set; }
        public Move BestMove => PrincipalVariations[0];
        public Span<Move> PrincipalVariation => GetFirstPVfromBuffer(PrincipalVariations, Depth);


        public IterativeSearch(BoardState board, SearchOptions options)
        {
            _options = options;
            _killers = new KillerMoves(2);
            _history = new History();

            Moves = new Move[MAX_PLY * MAX_MOVES];

            //PV-length = depth + (depth - 1) + (depth - 2) + ... + 1
            const int d = MAX_PLY + 1;
            PrincipalVariations = new Move[(d * d + d) / 2];

            //Initialize BoardState Stack
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Positions[0].Copy(board);

            //Initialize a random bonus added to each root move
            Random random = new Random();
            int maxRandomCpBonus = _options.Randomness(board.Eval.Phase);
            RootMoveOffsets = new int[MAX_MOVES];
            for (int i = 0; i < MAX_MOVES; i++)
                RootMoveOffsets[i] = random.Next(maxRandomCpBonus);
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
            Move bestMove = PrincipalVariations[0];
            MoveGen moveGen = new MoveGen(Moves, 0);
            int score = EvaluateRoot(Depth, moveGen, ref bestMove);

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
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FailLow(int ply, int remaining, int alpha, MoveGen moveGen)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -alpha - 1, -alpha, moveGen) <= alpha;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FailHigh(int ply, int remaining, int beta, MoveGen moveGen)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -beta, -beta + 1, moveGen) >= beta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateTT(int ply, int remaining, int alpha, int beta, MoveGen moveGen)
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PlayState InitPlay(ref MoveGen moveGen, ref Move pvMove)
        {
            return new PlayState { Stage = Stage.New, Next = moveGen.Collect(pvMove) };
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
            for (int i = ply - 4; i >= 0; i -= 2)
                if (Positions[i].ZobristHash == Positions[ply].ZobristHash)
                    return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StripKillers(int first, ref MoveGen moveGen, Span<Move> span)
        {
            //find the best move...
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
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
            return bestScore;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AllowNullMove(int ply)
        {
            //if the previous iteration found a mate we do the first few plys without null move to try and find the shortest mate or escape
            return !Evaluation.IsCheckmate(Score) || (ply > Depth / 4);
        }

        private int EvaluateRoot(int depth, MoveGen moveGen, ref Move bestMove)
        {
            NodesVisited++;

            BoardState root = Positions[0];
            BoardState next = Positions[1];
            bool inCheck = root.InCheck();
            int alpha = MIN_ALPHA;

            //init staged move generation and play all moves
            PlayState playState = InitPlay(ref moveGen, ref bestMove);
            while (Play(0, ref playState, ref moveGen))
            {
                ref Move move = ref Moves[playState.Next - 1];
                _history.Played(depth, ref move);

                //Scoring Root Moves with a random bonus: https://www.chessprogramming.org/Ronald_de_Man
                int bonus = Evaluation.IsCheckmate(Score) ? 0 : RootMoveOffsets[playState.PlayedMoves - 1];

                //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                if (depth >= 2 && playState.PlayedMoves > 1)
                {
                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    int R = (inCheck || next.InCheck() || playState.Stage < Stage.Quiets) ? 0 : 2;

                    //if (FailLow(0, depth - R, alpha, moveGen)) BUT WHIT BONUS!
                    if (bonus - EvaluateTT(1, depth - R - 1, bonus - alpha - 1, bonus - alpha, moveGen) <= alpha)
                        continue;
                }

                //Scoring Root Moves with a random bonus: https://www.chessprogramming.org/Ronald_de_Man
                int score = bonus - EvaluateTT(1, depth - 1, bonus - MAX_BETA, bonus - alpha, moveGen);

                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;
                    ExtendPV(0, depth, bestMove);

                    if (playState.Stage >= Stage.Killers)
                    {
                        _history.Good(depth, ref bestMove);
                        _killers.Add(0, bestMove);
                    }
                }
            }

            //checkmate or draw?
            if (playState.PlayedMoves == 0)
                return inCheck ? Evaluation.Checkmate(0) : 0;

            return alpha;
        }

        private int Evaluate(int ply, int remaining, int alpha, int beta, MoveGen moveGen, ref Move bestMove)
        {
            NodesVisited++;

            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            bool inCheck = current.InCheck();

            //consider null move pruning first
            if (!inCheck && remaining >= 2 && beta < MAX_BETA && current.RelativeScore() > alpha && !current.IsEndgame() && AllowNullMove(ply))
            {
                //if stm can skip a move and the position is still "too good" we can assume that this position, after making a move, would also fail high
                next.PlayNullMove(current);
                if (FailHigh(ply, remaining - R_NULL_MOVE, beta, moveGen))
                    return beta;
            }

            //init staged move generation and play all moves
            PlayState playState = InitPlay(ref moveGen, ref bestMove);
            while (Play(ply, ref playState, ref moveGen))
            {
                ref Move move = ref Moves[playState.Next - 1];
                _history.Played(remaining, ref move);

                bool interesting = playState.Stage == Stage.New || inCheck || next.InCheck();

                //some nodes near the leaves that appear hopeless can be skipped without evaluation
                if (!interesting && !Evaluation.IsCheckmate(Score))
                {
                    //if the static eval looks much worse than alpha also skip it
                    float futilityMargin = alpha - remaining * (playState.Stage == Stage.Quiets ? _options.LateFutilityMargin : _options.FutilityMargin);
                    if (next.RelativeScore(current.SideToMove) < futilityMargin)
                        continue;
                }

                //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                if (remaining >= 2 && playState.PlayedMoves > 1)
                {
                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    int R = interesting || playState.Stage < Stage.Quiets ? 0 : 2;
                    if (FailLow(ply, remaining - R, alpha, moveGen))
                        continue;
                }

                //...but if it does not we have to research it!
                int score = -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, moveGen);

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

            Aborted |= ForcedCut(ply);
            if (Aborted)
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

            //TODO: if (!inCheck || movesPlayed)
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

            //NOTE: this kind of stale-mate detection was a loss for Leorik 1.0!
            //if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
            //    return 0;
        }
    }
}