using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Search
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Leorik Search v1");
            Console.WriteLine();
            var file = File.OpenText("wac.epd");
            CompareBestMove(file);
            Console.WriteLine();
            Console.WriteLine("Press any key to quit");//stop command prompt from closing automatically on windows
            Console.ReadKey();
        }

        private static void CompareBestMove(StreamReader file)
        {
            //Init/Warmup
            Search(Notation.GetStartingPosition());

            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            while (!file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out List<Move> bestMoves) > 0)
            {
                long t0 = Stopwatch.GetTimestamp();
                Move pvMove = Search(board);
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

        private const int MAX_PLY = 10;
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

        private static Move Search(BoardState board)
        {
            NodesVisited = 0;
            Positions[0].Copy(board);
            BoardState current = Positions[0];
            BoardState next = Positions[0 + 1];

            int best = -1;
            int bestScore = int.MinValue;
            int stm = (int)board.SideToMove;
            MoveGen moveGen = new MoveGen(Moves, 0);
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    NodesVisited++;
                    int score = -NegaMax(1, moveGen);
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

        private static int NegaMax(int depth, MoveGen moveGen)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            int max = int.MinValue;
            int stm = (int)current.SideToMove;
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayAndUpdate(current, ref Moves[i]))
                {
                    NodesVisited++;
                    if (depth < 4)
                        score = -NegaMax(depth + 1, moveGen);
                    else
                        score = stm * next.Eval.Score;

                    if (score > max)
                        max = score;
                }
            }
            return max;
        }
    }
}
