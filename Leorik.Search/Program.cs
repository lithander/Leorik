using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Search
{
    class Program
    {
        const int DEPTH = 5;

        static void Main()
        {
            Console.WriteLine("Leorik Search v3");
            Console.WriteLine();

            CompareBestMove(File.OpenText("wac.epd"), DEPTH, SearchMinMax, "MinMax", false);
            CompareBestMove(File.OpenText("wac.epd"), DEPTH, SearchAlphaBeta, "AlphaBeta", false);
            CompareBestMove(File.OpenText("wac.epd"), DEPTH, SearchQSearch, "QSearch", false);

            Console.WriteLine("Press any key to quit");//stop command prompt from closing automatically on windows
            Console.ReadKey();
        }

        private delegate Move SearchDelegate(BoardState state, int depth);

        private static void CompareBestMove(StreamReader file, int depth, SearchDelegate search, string label, bool logDetails)
        {
            Console.WriteLine($"Searching {label}({depth})");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            while (!file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out List<Move> bestMoves) > 0)
            {
                long t0 = Stopwatch.GetTimestamp();
                Move pvMove = search(board, depth);
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                count++;
                totalTime += dt;
                totalNodes += NodesVisited;
                string pvString = Notation.GetMoveName(pvMove);
                bool foundBestMove = bestMoves.Contains(pvMove);
                if (foundBestMove)
                    foundBest++;

                if (logDetails)
                {
                    Console.WriteLine($"{count,4}. {(foundBestMove ? "[X]" : "[ ]")} {pvString} = {Score:+0.00;-0.00}, {NodesVisited / 1000}K nodes, { 1000 * dt / freq}ms");
                    Console.WriteLine($"{totalNodes,14} nodes, { (int)(totalTime / freq)} seconds, {foundBest} solved.");
                }
                else 
                    Console.Write('.');
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"Searched {count} positions with {label}({depth})");
            Console.WriteLine($"{totalNodes / 1000}K nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions!");
            Console.WriteLine();
        }

        private static void CompareBestMoveQSearch(StreamReader file, int depth)
        {
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            while (!file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out List<Move> bestMoves) > 0)
            {
                long t0 = Stopwatch.GetTimestamp();
                Move pvMove = SearchQSearch(board, depth);
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                count++;
                totalTime += dt;
                totalNodes += NodesVisited;
                string pvString = Notation.GetMoveName(pvMove);
                bool foundBestMove = bestMoves.Contains(pvMove);
                if (foundBestMove)
                    foundBest++;

                Console.WriteLine($"{count,4}. {(foundBestMove ? "[X]" : "[ ]")} {pvString} = {Score:+0.00;-0.00}, {NodesVisited / 1000}K nodes, { 1000 * dt / freq}ms");
                Console.WriteLine($"{totalNodes,14} nodes, { (int)(totalTime / freq)} seconds, {foundBest} solved.");
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"Searching {count} positions with QSearch({depth})");
            Console.WriteLine($"{totalNodes / 1000}K nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions!");
        }

        private static int ParseEpd(string epd, out BoardState board, out List<Move> bestMoves)
        {
            //The parser expects a fen-string with bm delimited by a ';'
            //Example: 2q1r1k1/1ppb4/r2p1Pp1/p4n1p/2P1n3/5NPP/PP3Q1K/2BRRB2 w - - bm f7+; id "ECM.001";
            int bmStart = epd.IndexOf("bm") + 3;
            int bmEnd = epd.IndexOf(';', bmStart);

            string fen = epd.Substring(0, bmStart);
            string bmString = epd.Substring(bmStart, bmEnd - bmStart);

            board = Notation.GetBoardState(fen);
            bestMoves = new List<Move>();
            foreach (var token in bmString.Split())
            {
                Move bestMove = Notation.ToMove(board, token);
                //Console.WriteLine($"{bmString} => {bestMove}");
                bestMoves.Add(bestMove);
            }
            return bestMoves.Count;
        }

        /*********************/
        /***    Search     ***/
        /*********************/

        private const int MAX_PLY = 99;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private static BoardState[] Positions;
        private static Move[] Moves;

        public static long NodesVisited { get; private set; }
        public static int Score { get; private set; }

        static Program()
        {
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];
        }               

        private static Move SearchMinMax(BoardState board, int depth)
        {
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int bestScore = -Evaluation.CheckmateScore;
            int stm = (int)board.SideToMove;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
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
            return Moves[best];
        }

        private static int NegaMax(int depth, int remaining, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int max = -Evaluation.CheckmateScore;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
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

        private static Move SearchAlphaBeta(BoardState board, int depth)
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
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
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
            return Moves[best];
        }

        private static int NegaAlphaBeta(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
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

        private static Move SearchQSearch(BoardState board, int depth)
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
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    NodesVisited++;
                    int score = -QSearch(1, depth - 1, -beta, -alpha, moveGen);
                    //int score = stm * next.Eval.Score;
                    if (score <= alpha)
                        continue;

                    best = i;
                    alpha = score;
                }
            }
            Score = stm * alpha;
            return Moves[best];
        }

        private static int QSearch(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (remaining > 1)
                        score = -QSearch(depth + 1, remaining - 1, -beta, -alpha, moveGen);
                    else
                        score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            return alpha;
        }

        private static int EvaluateQuiet(int depth, int alpha, int beta, MoveGen moveGen)
        {
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
            for (int i = inCheck ? moveGen.Collect(current) : moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    NodesVisited++;
                    int score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            if (inCheck && !movesPlayed)
                return Evaluation.Checkmate(current.SideToMove, depth);

            //stalemate?
            //if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
            //    return 0;

            return alpha;
        }
    }
}
