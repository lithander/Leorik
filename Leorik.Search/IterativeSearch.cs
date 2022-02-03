using Leorik.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Search
{
    public class IterativeSearch
    {
        private const int MAX_PLY = 99;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private BoardState[] Positions;
        private Move[] Moves;

        const int QUERY_TC_FREQUENCY = 100;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move[] PrincipalVariation { get; private set; } = Array.Empty<Move>();

        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get(NodesVisited % QUERY_TC_FREQUENCY == 0);
        public bool GameOver => Evaluation.IsCheckmate(Score);

        private KillSwitch _killSwitch;
        private long _maxNodes;

        public IterativeSearch(BoardState board, long maxNodes = long.MaxValue)
        {
            _maxNodes = maxNodes;

            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];

            Positions[0].Copy(board);
        }

        public void Search(int maxDepth)
        {
            while (Depth < maxDepth)
                SearchDeeper();
        }

        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            Depth++;
            _killSwitch = new KillSwitch(killSwitch);

            BoardState root = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int alpha = -Evaluation.CheckmateScore;
            int beta = Evaluation.CheckmateScore;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.Collect(root); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(root, ref Moves[i]))
                {
                    int score = -Evaluate(1, Depth - 1, -beta, -alpha, moveGen);
                    //int score = stm * next.Eval.Score;
                    if (score > alpha)
                    {
                        best = i;
                        alpha = score;
                    }
                }
            }
            //checkmate or draw?
            if (best == -1)
            {
                Score = root.IsChecked(root.SideToMove) ? Evaluation.Checkmate(root.SideToMove, 0) : 0;
                PrincipalVariation = Array.Empty<Move>();
            }
            else
            {
                Score = (int)root.SideToMove * alpha;
                PrincipalVariation = new Move[1] { Moves[best] };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickBestMove(int first, int end)
        {
            //we need to find the best move...
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
            //... and swap it with the first
            if (best != first)
            {
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
        }

        private int Evaluate(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            if (remaining == 0)
                return EvaluateQuiet(depth, alpha, beta, moveGen);

            NodesVisited++;
            if (Aborted)
                return 0;

            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            bool movesPlayed = false;


            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);

                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    score = -Evaluate(depth + 1, remaining - 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    score = -Evaluate(depth + 1, remaining - 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            //checkmate or draw?
            if (!movesPlayed)
                return current.IsChecked(current.SideToMove) ? Evaluation.Checkmate(depth) : 0;

            return alpha;
        }

        private int EvaluateQuiet(int depth, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;
            if (Aborted)
                return 0;

            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];

            bool inCheck = current.IsChecked(current.SideToMove);

            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = (int)current.SideToMove * current.Eval.Score;

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                    alpha = standPatScore;
            }

            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);
                if (next.PlayAndUpdate(current, ref Moves[i]))
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
                    if (next.PlayAndUpdate(current, ref Moves[i]))
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