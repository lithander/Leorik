using Leorik.Core;
using Leorik.Search;
using System.Diagnostics;

namespace Leorik.Test
{
    class Program
    {
        const int WAC_COUNT = 300;
        const int MATE_COUNT = 999;
        const bool DETAILS = true;

        static void Main()
        {
            Network.LoadDefaultNetwork();

            Console.WriteLine("Leorik Tests v15");
            Console.WriteLine();
            unsafe
            {
                Console.WriteLine("sizeof(Move) = " + sizeof(Move));
                Console.WriteLine("sizeof(HashEntry) = " + sizeof(Transpositions.HashEntry));
                Console.WriteLine("sizeof(Evaluation) = " + sizeof(Evaluation));
                Console.WriteLine();
            }

            //CompareBestMove(File.OpenText("arasan21.epd"), 1000, WAC_COUNT, DETAILS);
            //RunSeeTests();

            Console.WriteLine("Depth:");
            if (!int.TryParse(Console.ReadLine(), out int depth))
                depth = 15;

            Console.WriteLine("Number of positions:");
            if (!int.TryParse(Console.ReadLine(), out int count))
                count = WAC_COUNT;

            Console.WriteLine("HashSize in MB:");
            if (int.TryParse(Console.ReadLine(), out int hashSize))
                Transpositions.Resize(hashSize);

            Console.WriteLine("Threads:");
            if (!int.TryParse(Console.ReadLine(), out int threads))
                threads = 1;

            Console.WriteLine("Temperature:");
            if (!int.TryParse(Console.ReadLine(), out int temp))
                temp = 0;

            CompareBestMove(File.OpenText("wac.epd"), depth, count, threads, temp, ParallelSearch, DETAILS);
            //CompareBestMove(File.OpenText("otsv4-mea.epd"), depth, count, IterativeSearch, "", DETAILS);
            //RunWacTestsDepth();
            //RunWacTestsTime();
            //RunMateTests();

            Console.WriteLine("Press ESC key to quit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
        }

        private static void RunWacTestsTime()
        {
            CompareBestMove(File.OpenText("wac.epd"), 100, WAC_COUNT, DETAILS);
            CompareBestMove(File.OpenText("wac.epd"), 300, WAC_COUNT, DETAILS);
            CompareBestMove(File.OpenText("wac.epd"), 1000, WAC_COUNT, DETAILS);
        }

        private static void RunWacTestsDepth()
        {
            for (int depth = 14; depth <= 20; depth += 2)
            {
                CompareBestMove(File.OpenText("wac.epd"), depth, WAC_COUNT, 1, -1, ParallelSearch, DETAILS);
            }
        }

        private delegate Span<Move> SearchDelegate(BoardState state, int depth, int threads, int temperature);

        private static void CompareBestMove(StreamReader file, int depth, int maxCount, int threads, int temp, SearchDelegate search, bool logDetails)
        {
            Console.WriteLine($"Searching {maxCount} positions on {threads} thread(s) to depth {depth}...");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            int totalScore = 0;
            while (count < maxCount && !file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out Dictionary<Move, int> bestMoves) > 0)
            {
                //Transpositions.Clear();
                Transpositions.IncreaseAge();
                long t0 = Stopwatch.GetTimestamp();
                Span<Move> pv = search(board, depth, threads, temp);
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                count++;
                totalTime += dt;
                totalNodes += NodesVisited;
                string pvString = string.Join(' ', pv.ToArray());
                bool foundBestMove = bestMoves.TryGetValue(pv[0], out int score);
                if (foundBestMove)
                {
                    foundBest++;
                    totalScore += score;
                }

                if (logDetails)
                {
                    Console.WriteLine($"{count,4}. {(foundBestMove ? "[X]" : "[ ]")} {pvString} = {Score:+0.00;-0.00}, {NodesVisited} nodes, { (int)(1000 * dt / freq)}ms");
                    Console.WriteLine($"{totalNodes,14} nodes, { (int)(totalTime / freq)} seconds, {foundBest} solved. ({totalScore}/{count*100})");
                }
                else
                    Console.Write('.');
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"{threads} thread(s) searched {count} positions to depth {depth}.");
            Console.WriteLine($"{totalNodes} nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions! Score: {totalScore}/{count * 100}");
            Console.WriteLine();
        }

        private static void CompareBestMove(StreamReader file, int timeBudgetMs, int maxCount, bool logDetails)
        {
            Console.WriteLine($"Searching {timeBudgetMs}ms per position!");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            int totalScore = 0;
            while (count < maxCount && !file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out Dictionary<Move, int> bestMoves) > 0)
            {
                Transpositions.Clear();
                Move pvMove = default;
                var search = new IterativeSearch(board, SearchOptions.Default, null);
                long t0 = Stopwatch.GetTimestamp();
                long tStop = t0 + (timeBudgetMs * Stopwatch.Frequency) / 1000;
                //search until running out of time
                while (search.Depth < 99)
                {
                    search.SearchDeeper(() => Stopwatch.GetTimestamp() > tStop);
                    if (search.Aborted)
                        break;
                    pvMove = search.PrincipalVariation[0];
                }
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                count++;
                totalTime += dt;
                totalNodes += search.NodesVisited;
                string pvString = string.Join(' ', search.PrincipalVariation.ToArray());
                bool foundBestMove = bestMoves.TryGetValue(pvMove, out int score);
                if (foundBestMove)
                {
                    foundBest++;
                    totalScore += score;
                }

                if (logDetails)
                {
                    Console.WriteLine($"{count,4}. {(foundBestMove ? "[X]" : "[ ]")} {pvString} = {Score:+0.00;-0.00}, {NodesVisited} nodes, { (int)(1000 * dt / freq)}ms");
                    Console.WriteLine($"{totalNodes,14} nodes, {(int)(totalTime / freq)} seconds, {foundBest} solved. ({totalScore}/{count * 100})");
                }
                else
                    Console.Write('.');
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"Searched {count} positions for {timeBudgetMs}ms each.");
            Console.WriteLine($"{totalNodes / 1000}K nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions! Score: {totalScore}/{count * 100}");
            Console.WriteLine();
        }

        private static int ParseEpd(string epd, out BoardState board, out Dictionary<Move, int> bestMoves)
        {
            //The parser expects a fen-string with bm delimited by a ';'
            //Example: 2q1r1k1/1ppb4/r2p1Pp1/p4n1p/2P1n3/5NPP/PP3Q1K/2BRRB2 w - - bm f7+; id "ECM.001";
            int bmStart = epd.IndexOf("bm") + 3;
            int bmEnd = epd.IndexOf(';', bmStart);

            string fen = epd.Substring(0, bmStart);
            string bmString = epd.Substring(bmStart, bmEnd - bmStart);

            board = Notation.GetBoardState(fen);
            bestMoves = new Dictionary<Move, int>();
            foreach (var token in bmString.Split())
            {
                Move bestMove = Notation.GetMove(board, token);
                //Console.WriteLine($"{bmString} => {bestMove}");
                bestMoves[bestMove] = 100;
            }

            //Example: r1bq1rk1/pp2bppp/2n1pn2/8/2Pp4/N2P1NP1/PP3PBP/R1BQ1RK1 w - - bm Re1; c0 "Re1=100, Qe2=100, Bg5=100, Bf4=100, Nc2=96, Bd2=94, Rb1=91"; acd 25; Ae "Stockfish 2019.04.16";
            int c0Start = epd.IndexOf("c0");
            if(c0Start != -1)
            {
                c0Start = epd.IndexOf('"', c0Start);
                int c0End = epd.IndexOf('"', c0Start + 1);
                string scoreString = epd.Substring(c0Start+1, c0End - c0Start - 1).Replace(",", string.Empty);
                foreach(var token in scoreString.Split()) 
                {
                    int split = token.IndexOf('=');
                    if (split == -1)
                        break;

                    string moveStr = token.Substring(0, split);
                    string scoreStr = token.Substring(split+1);
                    Move bestMove = Notation.GetMove(board, moveStr);
                    int score = int.Parse(scoreStr);
                    bestMoves[bestMove] = score;
                    //Console.WriteLine($"{bestMove}={score}");
                }
            }

            return bestMoves.Count;
        }

        private static void RunMateTests()
        {
            Console.WriteLine("~======================~");
            Console.WriteLine("|    Mate in X Tests   |");
            Console.WriteLine("~======================~");
            Console.WriteLine();

            FindMate(File.OpenText("mate_in_1.epd"), 1, MATE_COUNT);
            FindMate(File.OpenText("mate_in_2.epd"), 2, MATE_COUNT);
            FindMate(File.OpenText("mate_in_3.epd"), 3, MATE_COUNT);
            FindMate(File.OpenText("mate_in_4.epd"), 4, MATE_COUNT);
            FindMate(File.OpenText("mate_in_5.epd"), 5, MATE_COUNT);
            FindMate(File.OpenText("mate_in_6.epd"), 6, MATE_COUNT);
        }

        private static void FindMate(StreamReader file, int mateDepth, int maxCount)
        {
            Console.WriteLine($"Searching mates in {mateDepth} moves");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundMate = 0;
            while (count < maxCount && !file.EndOfStream)
            {
                //The parser expects a fen-string with bm delimited by a ';'
                //Example: 2q1r1k1/1ppb4/r2p1Pp1/p4n1p/2P1n3/5NPP/PP3Q1K/2BRRB2 w - - bm f7+; id "ECM.001";
                string epd = file.ReadLine();
                int bmStart = epd.IndexOf("bm");
                string fen = epd.Substring(0, bmStart);

                BoardState board = Notation.GetBoardState(fen);

                Transpositions.Clear();
                long t0 = Stopwatch.GetTimestamp();
                var search = new IterativeSearch(board, SearchOptions.Default, null);
                search.Search(mateDepth * 2);
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                if (Evaluation.IsCheckmate(search.Score))
                {
                    Console.Write('.');
                    foundMate++;
                }
                else
                    Console.Write('!');

                count++;
                totalTime += dt;
                totalNodes += search.NodesVisited;
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"{totalNodes / 1000}K nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Mate found in {foundMate} / {count} positions!");
            Console.WriteLine();
        }

        /*********************/
        /***    Search     ***/
        /*********************/


        private static void RunSeeTests()
        {
            string QLabel(int sign)
            {
                if (sign > 0) return "GOOD";
                if (sign < 0) return "BAD";
                return "NEUTRAL";
            }

            StreamReader file = File.OpenText("see.epd");
            int count = 0;
            int correct = 0;
            while (!file.EndOfStream)
            {
                //Example: 2r1r1k1/pp1bppbp/3p1np1/q3P3/2P2P2/1P2B3/P1N1B1PP/2RQ1RK1 b - -; dxe5; 100; Pawn;
                string line = file.ReadLine();
                if (line.Length == 0)
                    break;
                if (line.StartsWith("//"))
                    continue;

                count++;
                var tokens = line.Split(';');
                string fen = tokens[0];
                BoardState position = Notation.GetBoardState(fen);

                string moveString = tokens[1].Trim();
                Move move = Notation.GetMove(position, moveString);
                Print(position, move);
                Console.WriteLine(fen);
                int seeRef = int.Parse(tokens[2]);
                int seeValue = (int)position.SideToMove * StaticExchange.Evaluate(position, move);
                int sign = Math.Sign(seeValue);
                int sign2 = (int)position.SideToMove * StaticExchange.EvaluateSign(position, move);
                Debug.Assert(seeRef == seeValue);
                Debug.Assert(sign == sign2);
                Console.WriteLine($"{count,4}. [{(seeRef == seeValue ? "X" : " ")}] {QLabel(sign)}, SEE({move}) = {seeValue} Solution: {seeRef} ({tokens[3]})");
                if (seeRef == seeValue)
                    correct++;
                Console.WriteLine();
            }
            Console.WriteLine($"Correct SEE in {correct} / {count} positions!");
        }

        private static void Print(BoardState board, Move move = default)
        {
            Console.WriteLine("   A B C D E F G H");
            Console.WriteLine(" .----------------.");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    Piece piece = board.GetPiece(square);
                    SetColor(piece, rank, file, move);
                    Console.Write(Notation.GetChar(piece));
                    Console.Write(' ');
                }
                Console.ResetColor();
                Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine(" '----------------'");
        }

        private static void SetColor(Piece piece, int rank, int file, Move move)
        {
            if ((rank + file) % 2 == 1)
                Console.BackgroundColor = ConsoleColor.DarkGray;
            else
                Console.BackgroundColor = ConsoleColor.Black;

            if (move != default)
            {
                int index = rank * 8 + file;
                //highlight squares if they belong to the move
                if (move.FromSquare == index)
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                else if(move.ToSquare == index)
                    Console.BackgroundColor = ConsoleColor.DarkRed;
            }

            if ((piece & Piece.ColorMask) == Piece.White)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Gray;
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


        /*****************************/
        /***    Search Instance    ***/
        /*****************************/

        private static Span<Move> IterativeSearch(BoardState board, int depth)
        {
            var search = new IterativeSearch(board, SearchOptions.Default, null);
            search.Search(depth);
            Score = search.Score;
            NodesVisited = search.NodesVisited;
            return search.PrincipalVariation;
        }

        private static Span<Move> ParallelSearch(BoardState board, int depth, int threads, int temperature)
        {
            var settings = SearchOptions.Default;
            settings.Threads = threads;
            settings.Temperature = temperature;
            ISearch search = threads > 1 ? new ParallelSearch(board, settings, null) : new IterativeSearch(board, settings, null);
            search.Search(depth);
            Score = search.Score;
            NodesVisited = search.NodesVisited;
            return search.PrincipalVariation;
        }
    }
}
