using Leorik.Core;
using Leorik.Search;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leorik.Test
{
    class Program
    {
        const int WAC_COUNT = 999;
        const int MATE_COUNT = 999;
        const bool DETAILS = true;

        static void Main()
        {
            Console.WriteLine("Leorik Tests v13");
            Console.WriteLine();
            unsafe
            {
                Console.WriteLine("sizeof(Move) = " + sizeof(Move));
                Console.WriteLine("sizeof(HashEntry) = " + sizeof(Transpositions.HashEntry));
                Console.WriteLine("sizeof(Evaluation) = " + sizeof(Evaluation));
                //Console.WriteLine("sizeof(BoardStateProxy) = " + sizeof(BoardStateProxy));
                Console.WriteLine();
            }

            RunWacTests();
            //RunMateTests();

            Console.WriteLine("Press ESC key to quit");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
        }

        private static void RunWacTests()
        {
            for (int i = 3; i <= 4; i++)
            {
                int budget = (int)Math.Pow(10, i);
                CompareBestMove(File.OpenText("wac.epd"), budget, WAC_COUNT, DETAILS);
            }
        }

        private delegate Span<Move> SearchDelegate(BoardState state, int depth);

        private static void CompareBestMove(StreamReader file, int depth, int maxCount, SearchDelegate search, string label, bool logDetails)
        {
            Console.WriteLine($"Searching {label}({depth})");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            while (count < maxCount && !file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out List<Move> bestMoves) > 0)
            {
                Transpositions.Clear();
                long t0 = Stopwatch.GetTimestamp();
                Span<Move> pv = search(board, depth);
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;

                count++;
                totalTime += dt;
                totalNodes += NodesVisited;
                string pvString = string.Join(' ', pv.ToArray());
                bool foundBestMove = bestMoves.Contains(pv[0]);
                if (foundBestMove)
                    foundBest++;

                if (logDetails)
                {
                    //Console.WriteLine(pvString);
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

        private static void CompareBestMove(StreamReader file, int timeBudgetMs, int maxCount, bool logDetails)
        {
            Console.WriteLine($"Searching {timeBudgetMs}ms per position!");
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            while (count < maxCount && !file.EndOfStream && ParseEpd(file.ReadLine(), out BoardState board, out List<Move> bestMoves) > 0)
            {
                Transpositions.Clear();
                Move pvMove = default;
                var search = new IterativeSearch(board);
                long t0 = Stopwatch.GetTimestamp();
                long tStop = t0 + (timeBudgetMs * Stopwatch.Frequency) / 1000;
                //search until running out of time
                while (true)
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


                int min = int.MaxValue;
                int max = int.MinValue;
                long sum = 0;
                for (int i = 0; i < PawnStructure.PawnHashTable.Length; i++)
                {
                    int cnt = PawnStructure.PawnHashTable[i].StoreCount;
                    min = Math.Min(cnt, min);
                    max = Math.Max(cnt, max);
                    sum += cnt;
                }
                int avg = (int)(sum / PawnStructure.PawnHashTable.Length);

                int over2xAvg = 0;
                for (int i = 0; i < PawnStructure.PawnHashTable.Length; i++)
                {
                    int cnt = PawnStructure.PawnHashTable[i].StoreCount;
                    if (cnt > 2*avg)
                        over2xAvg++;
                }
                Console.WriteLine($"Min {min} Max {max} Average {avg} >2xAverage {over2xAvg}/{PawnStructure.HASH_TABLE_SIZE} {(over2xAvg*100)/PawnStructure.HASH_TABLE_SIZE}%");
                float ratio = PawnStructure.TableHits / (float)(PawnStructure.TableHits + PawnStructure.TableMisses);
                Console.WriteLine($"Hits {PawnStructure.TableHits} Misses {PawnStructure.TableMisses} Hitrate {ratio*100:0.00}%");
                PawnStructure.Clear();
                Console.WriteLine();
            }

            double nps = totalNodes / (totalTime / freq);
            Console.WriteLine();
            Console.WriteLine($"Searched {count} positions for {timeBudgetMs}ms each.");
            Console.WriteLine($"{totalNodes / 1000}K nodes visited. Took {totalTime / freq:0.###} seconds!");
            Console.WriteLine($"{(int)(nps / 1000)}K NPS.");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions!");
            Console.WriteLine();
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
                Move bestMove = Notation.GetMove(board, token);
                //Console.WriteLine($"{bmString} => {bestMove}");
                bestMoves.Add(bestMove);
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
                var search = new IterativeSearch(board);
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
            var search = new IterativeSearch(board);
            search.Search(depth);
            Score = search.Score;
            NodesVisited = search.NodesVisited;
            return search.PrincipalVariation;
        }

        /*********************/
        /***    NegaMax     ***/
        /*********************/

        private static Span<Move> NegaMaxSearch(BoardState board, int depth)
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
                if (next.PlayWithoutHash(current, ref Moves[i]))
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
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayWithoutHash(current, ref Moves[i]))
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

        private static Span<Move> AlphaBetaSearch(BoardState board, int depth)
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
                if (next.PlayWithoutHash(current, ref Moves[i]))
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
            for (int i = moveGen.Collect(current); i < moveGen.Next; i++)
            {
                if (next.PlayWithoutHash(current, ref Moves[i]))
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

        private static Span<Move> MvvLvaSearch(BoardState board, int depth)
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
                if (next.PlayWithoutHash(current, ref Moves[i]))
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

                if (next.PlayWithoutHash(current, ref Moves[i]))
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
                if (next.PlayWithoutHash(current, ref Moves[i]))
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

        /*********************/
        /***    QSearch    ***/
        /*********************/

        private static Span<Move> QuiescenceSearch(BoardState board, int depth)
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
                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    int score = -QSearch(1, depth - 1, -beta, -alpha, moveGen);
                    if (score > alpha)
                    {
                        best = i;
                        alpha = score;
                    }
                }
            }
            Score = stm * alpha;
            return new Span<Move>(Moves, best, 1);
        }


        private static int QSearch(int depth, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            if (remaining == 0)
                return EvaluateQuiet(depth, alpha, beta, moveGen);

            NodesVisited++;
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int score;
            bool movesPlayed = true;

            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);

                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    score = -QSearch(depth + 1, remaining - 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    score = -QSearch(depth + 1, remaining - 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            //checkmate or draw?
            if (!movesPlayed)
                return current.InCheck() ? Evaluation.Checkmate(current.SideToMove, depth) : 0;

            return alpha;
        }

        private static int EvaluateQuiet(int depth, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];

            bool inCheck = current.InCheck();

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
                    return Evaluation.Checkmate(current.SideToMove, depth);
            }

            //stalemate?
            //if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
            //    return 0;

            return alpha;
        }
    }
}
