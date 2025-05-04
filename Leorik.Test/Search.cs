using Leorik.Core;
using Leorik.Search;
using System.Runtime.CompilerServices;

namespace Leorik.Test
{
    public static class Search
    {
        /*********************/
        /***    Search     ***/
        /*********************/

        private const int MAX_PLY = 99;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private static BoardState[] Positions;
        private static Move[] Moves;

        public static long NodesVisited { get; private set; }
        public static int Score { get; private set; }

        static Search()
        {
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];
        }

        /*****************************/
        /***    Search Instance    ***/
        /*****************************/

        public static SearchOptions Options = SearchOptions.Default;

        public static Span<Move> Iterative(BoardState board, int depth)
        {
            ISearch search = Options.Threads > 1 ? new ParallelSearch(board, Options, null, null) : new IterativeSearch(board, Options, null, null);
            search.Search(depth);
            Score = search.Score;
            NodesVisited = search.NodesVisited;
            return search.PrincipalVariation;
        }

        /*********************/
        /***    NegaMax     ***/
        /*********************/

        public static Span<Move> NegaMax(BoardState board, int depth)
        {
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int bestScore = -Evaluation.CheckmateScore;
            int stm = (int)board.SideToMove;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.CollectAll(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    int score = -NegaMax(1, depth - 1, moveGen);
                    //int score = stm * next.Eval.Score;
                    if (score <= bestScore)
                        continue;

                    best = i;
                    bestScore = score;
                }
            }
            Score = stm * bestScore;
            return new Span<Move>(Moves, best, 1);
        }

        private static int NegaMax(int depth, int remaining, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int max = -Evaluation.CheckmateScore;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.CollectAll(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (remaining > 1)
                        score = -NegaMax(depth + 1, remaining - 1, moveGen);
                    else
                        score = stm * next.Eval.Score;

                    if (score > max)
                        max = score;
                }
            }
            return max;
        }

        /************************/
        /***    AlphaBeta     ***/
        /************************/

        public static Span<Move> AlphaBeta(BoardState board, int depth)
        {
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int alpha = -Evaluation.CheckmateScore;
            int beta = Evaluation.CheckmateScore;
            int stm = (int)board.SideToMove;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.CollectAll(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    int score = -NegaAlphaBeta(1, depth - 1, -beta, -alpha, moveGen);
                    //int score = stm * next.Eval.Score;
                    if (score <= alpha)
                        continue;

                    best = i;
                    alpha = score;
                }
            }
            Score = stm * alpha;
            return new Span<Move>(Moves, best, 1);
        }

        private static int NegaAlphaBeta(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.CollectAll(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (remaining > 1)
                        score = -NegaAlphaBeta(depth + 1, remaining - 1, -beta, -alpha, moveGen);
                    else
                        score = stm * next.Eval.Score;

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            return alpha;
        }

        /*********************/
        /***    MvvLva     ***/
        /*********************/

        public static Span<Move> MvvLva(BoardState board, int depth)
        {
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int alpha = -Evaluation.CheckmateScore;
            int beta = Evaluation.CheckmateScore;
            int stm = (int)board.SideToMove;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.CollectAll(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    int score = -NegaMvvLva(1, depth - 1, -beta, -alpha, moveGen);
                    //int score = stm * next.Eval.Score;
                    if (score <= alpha)
                        continue;

                    best = i;
                    alpha = score;
                }
            }
            Score = stm * alpha;
            return new Span<Move>(Moves, best, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PickBestMove(int first, int end)
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

        private static int NegaMvvLva(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);

                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (remaining > 1)
                        score = -NegaMvvLva(depth + 1, remaining - 1, -beta, -alpha, moveGen);
                    else
                        score = stm * next.Eval.Score;

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (remaining > 1)
                        score = -NegaMvvLva(depth + 1, remaining - 1, -beta, -alpha, moveGen);
                    else
                        score = stm * next.Eval.Score;

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            return alpha;
        }

        /****************************/
        /***    BestNodeSearch    ***/
        /****************************/

        public static Span<Move> BestNodeSearch(BoardState board, int depth)
        {
            //Find the best node but not it's value: https://en.wikipedia.org/wiki/Best_node_search
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            int stm = (int)board.SideToMove;

            MoveGen moveGen = new MoveGen(Moves, 0);
            int moveStart = moveGen.CollectAll(current);
            int moveEnd = moveGen.Next;
            int totalChildren = moveEnd - moveStart;
            if (totalChildren == 0)
                return new Span<Move>(Moves, 0, 0);

            // Initially, the number of subtrees is simply the total legal moves.
            int subtreeCount = totalChildren;
            int bestIndex = -1;
            int alpha = -Evaluation.CheckmateScore;
            int beta = Evaluation.CheckmateScore;

            // Iteratively search until either the window is small or exactly one child exceeds the test value.
            while ((beta - alpha) >= 2)
            {
                // Compute the next separation (test) value using a linear distribution:
                int test = alpha + ((beta - alpha) * (subtreeCount - 1)) / subtreeCount;
                int betterCount = 0;
                int candidateIndex = -1;

                for (int i = moveStart; i < moveEnd; i++)
                {
                    // Try move 'i' from the current position.
                    if (Positions[1].QuickPlay(current, ref Moves[i]))
                    {
                        NodesVisited++;

                        // Use a zero-window [test-1, test]
                        int score = -NegaMvvLva(1, depth - 1, -test, -(test - 1), moveGen);
                        // child is only "good" if it returns >= test.
                        if (score >= test)
                        {
                            betterCount++;
                            candidateIndex = i;
                        }
                    }
                }

                if (betterCount == 1)
                {
                    // This is the one child that beats the test value: we've found the best node.
                    bestIndex = candidateIndex;
                    break;
                }
                else if (betterCount == 0)
                {
                    // No child exceeded the test value: lower the beta bound.
                    beta = test;
                }
                else // betterCount > 1
                {
                    // Several children exceeded the test: raise the alpha bound and search the subtree again
                    alpha = test;
                    subtreeCount = betterCount;
                }
            }

            // If no candidate was uniquely identified, default to the first move.
            if (bestIndex == -1)
                bestIndex = moveStart;

            Score = stm * alpha;
            return new Span<Move>(Moves, bestIndex, 1);
        }
    }
}