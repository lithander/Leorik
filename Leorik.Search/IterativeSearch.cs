using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class IterativeSearch
    {
        private const int MIN_ALPHA = -Evaluation.CheckmateScore;
        private const int MAX_BETA = Evaluation.CheckmateScore;
        private const int MAX_PLY = 99;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058

        private BoardState[] Positions;
        private Move[] Moves;
        private Move[] PrincipalVariations;
        private KillSwitch _killSwitch;
        private long _maxNodes;

        public static int MaxDepth => MAX_PLY;
        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move BestMove => PrincipalVariations[0];
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get();
        public bool GameOver => Evaluation.IsCheckmate(Score);
        public Span<Move> PrincipalVariation => GetFirstPVfromBuffer(PrincipalVariations, Depth);


        public IterativeSearch(BoardState board, long maxNodes = long.MaxValue)
        {
            _maxNodes = maxNodes;

            Moves = new Move[MAX_PLY * MAX_MOVES];

            //PV-length = depth + (depth - 1) + (depth - 2) + ... + 1
            const int d = MAX_PLY + 1;
            PrincipalVariations = new Move[(d * d + d) / 2];

            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Positions[0].Copy(board);
        }

        public void Search(int maxDepth)
        {
            while (Depth < maxDepth)
                SearchDeeper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ForcedCut(int depth)
        {
            return depth >= MAX_PLY - 1 || NodesVisited >= _maxNodes || _killSwitch.Get();
        }

        private static Span<Move> GetFirstPVfromBuffer(Move[] pv, int depth)
        {
            //return moves until the first is 'default' move but not more than 'depth' number of moves
            int end = Array.IndexOf(pv, default, 0, depth);
            return new Span<Move>(pv, 0, end >= 0 ? end : depth);
        }

        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            Transpositions.StorePV(Positions[0], PrincipalVariation, Depth, Score);
            Depth++;
            _killSwitch = new KillSwitch(killSwitch);
            Move bestMove = PrincipalVariations[0];
            MoveGen moveGen = new MoveGen(Moves, 0);
            PVHead pv = new PVHead(PrincipalVariations, Depth);
            Score = Evaluate(0, Depth, MIN_ALPHA, MAX_BETA, moveGen, pv, ref bestMove);
            Transpositions.Store(Positions[0].ZobristHash, Depth, 0, MIN_ALPHA, MAX_BETA, Score, bestMove);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickBestMove(int first, int end)
        {
            //we want to swap the first move with the best move
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
            //swap best with first
            if (best != first)
            {
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FailLow(int ply, int remaining, int alpha, MoveGen moveGen, PVHead pv)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -alpha - 1, -alpha, moveGen, pv) <= alpha;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateTT(int ply, int remaining, int alpha, int beta, MoveGen moveGen, PVHead pv)
        {
            if (remaining == 0)
                return EvaluateQuiet(ply, alpha, beta, moveGen);

            if (ForcedCut(ply))
                return Positions[ply].SignedScore();

            pv.Truncate();
            ulong hash = Positions[ply].ZobristHash;
            if (Transpositions.GetScore(hash, remaining, ply, alpha, beta, out Move bm, out int ttScore))
                return ttScore;

            int score = Evaluate(ply, remaining, alpha, beta, moveGen, pv, ref bm);
            Transpositions.Store(hash, remaining, ply, alpha, beta, score, bm);
            return score;
        }

        private int Evaluate(int ply, int remaining, int alpha, int beta, MoveGen moveGen, PVHead pv, ref Move bm)
        {
            NodesVisited++;
            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            int score;
            bool movesPlayed = false;

            if (bm != default)
            {
                if (next.Play(current, ref bm))
                {
                    movesPlayed = true;
                    score = -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, moveGen, pv.NextDepth);

                    if (score > alpha)
                    {
                        alpha = score;
                        pv.Extend(bm);
                    }

                    if (score >= beta)
                        return beta;
                }
            }

            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);
                if (next.Play(current, ref Moves[i]))
                {
                    //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                    if (movesPlayed && remaining > 1 && FailLow(ply, remaining, alpha, moveGen, pv.NextDepth))
                        continue;

                    //...but if it does not we have to research it!
                    score = -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, moveGen, pv.NextDepth);

                    movesPlayed = true;
                    if (score > alpha)
                    {
                        bm = Moves[i];
                        pv.Extend(bm);
                        alpha = score;
                    }

                    if (score >= beta)
                        return beta;
                }
            }
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.Play(current, ref Moves[i]))
                {
                    //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                    if (movesPlayed && remaining > 1 && FailLow(ply, remaining, alpha, moveGen, pv.NextDepth))
                        continue;

                    //...but if it does not we have to research it!
                    score = -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, moveGen, pv.NextDepth);

                    movesPlayed = true;
                    if (score > alpha)
                    {
                        bm = Moves[i];
                        pv.Extend(bm);
                        alpha = score;
                    }

                    if (score >= beta)
                        return beta;
                }
            }

            //checkmate or draw?
            if (!movesPlayed)
                return current.IsChecked(current.SideToMove) ? Evaluation.Checkmate(ply) : 0;

            return alpha;
        }

        private int EvaluateQuiet(int depth, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;

            BoardState current = Positions[depth];
            bool inCheck = current.IsChecked(current.SideToMove);
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = current.SignedScore();

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                    alpha = standPatScore;
            }

            if (ForcedCut(depth))
                return current.SignedScore();

            BoardState next = Positions[depth + 1];
            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);
                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            if (inCheck)
            {
                for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
                {
                    if (next.PlayWithoutHash(current, ref Moves[i]))
                    {
                        movesPlayed = true;
                        int score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                        if (score >= beta)
                            return beta;

                        if (score > alpha)
                            alpha = score;
                    }
                }

                if (!movesPlayed)
                    return Evaluation.Checkmate(depth);
            }

            //stalemate?
            //if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
            //    return 0;

            return alpha;
        }
    }
}