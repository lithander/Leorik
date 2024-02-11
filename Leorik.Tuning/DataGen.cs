using Leorik.Core;
using Leorik.Search;
using System.Diagnostics;

namespace Leorik.Tuning
{
    interface IPlayoutWriter
    {
        void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves, List<short> scores);
    }

    class BinaryPlayoutWriter : IPlayoutWriter, IDisposable
    {
        BinaryWriter _stream;
        MarlinFormat _packedBoard = new();

        public BinaryPlayoutWriter(string filePath)
        {
            _stream = new BinaryWriter(new FileStream(filePath, FileMode.Create));
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves, List<short> scores)
        {
            byte moveCount = (byte)moves.Count;
            _packedBoard.Pack(position, (short)randomMoves, (short)position.Score(), wdl, moveCount);
            _packedBoard.Write(_stream);
            for (int i = 0; i < moveCount; i++)
            {
                _stream.Write((int)moves[i]);
                _stream.Write(scores[i]);
            }
            _stream.Flush();
        }
    }

    class TextPlayoutWriter : IPlayoutWriter
    {
        TextWriter _stream;

        public TextPlayoutWriter(string filePath)
        {
            _stream = File.CreateText(filePath);
        }

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves, List<short> scores)
        {
            byte moveCount = (byte)moves.Count;
            _stream.WriteLine(Notation.GetFen(position, randomMoves));
            _stream.WriteLine(position.Score());
            _stream.WriteLine(wdl);
            _stream.WriteLine(moveCount);
            for (int i = 0; i < moveCount; i++)
            {
                if (i > 0)
                    _stream.Write(' ');
                _stream.Write(Notation.GetMoveName(moves[i]));
                _stream.Write(' ');
                _stream.Write(scores[i]);
            }
            _stream.WriteLine();
            _stream.Flush();
        }
    }


    class DoublePlayoutWriter : IPlayoutWriter
    {
        IPlayoutWriter _writerA;
        IPlayoutWriter _writerB;

        public DoublePlayoutWriter(string path, string fileName)
        {
            _writerA = new TextPlayoutWriter($"{path}/{fileName}.playout.txt");
            _writerB = new BinaryPlayoutWriter($"{path}/{fileName}.playout.bin");
        }

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves, List<short> scores)
        {
            _writerA.Write(position, randomMoves, wdl, moves, scores);
            _writerB.Write(position, randomMoves, wdl, moves, scores);
        }

        public static void ValidatePlayout(string filePath)
        {
            var binFile = File.OpenRead(filePath + ".bin");
            var txtFile = File.OpenRead(filePath + ".txt");

            var binReader = new BinaryReader(binFile);
            var txtReader = new StreamReader(txtFile);

            MarlinFormat packed = new MarlinFormat();
            while (binFile.Position < binFile.Length)
            {
                packed.Read(binReader);
                BoardState board = packed.Unpack(out short fullMoveNumber, out short eval, out byte wdl, out byte extra);
                string fen = Notation.GetFen(board, fullMoveNumber);
                string fen2 = txtReader.ReadLine();
                Debug.Assert(fen == fen2);
                short eval2 = short.Parse(txtReader.ReadLine());
                Debug.Assert(eval == eval2);
                byte wdl2 = byte.Parse(txtReader.ReadLine());
                Debug.Assert(wdl == wdl2);
                byte moveCount = byte.Parse(txtReader.ReadLine());
                Debug.Assert(extra == moveCount);

                string[] movesAndScores = txtReader.ReadLine().Split(' ');
                for (int iMove = 0; iMove < extra; iMove++)
                {
                    Move move = (Move)binReader.ReadInt32();
                    string move2 = movesAndScores[iMove * 2];
                    Debug.Assert(move2 == Notation.GetMoveName(move));

                    short score = binReader.ReadInt16();
                    short score2 = short.Parse(movesAndScores[iMove * 2 + 1]);
                    Debug.Assert(score == score2);
                }
            }
            Console.WriteLine("All good!");
        }
    }

    internal class DataGen
    {
        const int VERSION = 6;
        const int RANDOM_MOVES = 8;
        const int HOT_MOVES = 6;
        const int NODE_COUNT = 10_000;

        public static void RunPrompt()
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine($" Leorik DataGen v{VERSION}");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine();

            Query("Threads", 1, out int threads);
            Query("Nodes", NODE_COUNT, out int nodes);
            Query("Number of random moves", RANDOM_MOVES, out int randomMoves);
            Query("Number of hot moves", HOT_MOVES, out int hotMoves);
            Query("Temperature", 0, out int temp);
            Query("Path", AppDomain.CurrentDomain.BaseDirectory, out string path);

            string suffix = nodes == int.MaxValue ? "" : $"_{nodes / 1000}K";
            if (hotMoves > 0 && temp > 0)
                suffix += $"_{randomMoves}R_{hotMoves}T{temp}_v{VERSION}";
            else
                suffix += $"_{randomMoves}R_v{VERSION}";

            string fileName = DateTime.Now.ToString("s").Replace(':', '.') + suffix;
            Query("File", fileName, out fileName);

            DoublePlayoutWriter writer = new DoublePlayoutWriter(path, fileName);
            //int nodes, int randomMoves, int hotMoves, int temp, int threads, IPlayoutWriter writer)
            RunDatagen(nodes, 5 * nodes, randomMoves, hotMoves, temp, threads, writer);
        }

        private static void Query(string label, int devaultValue, out int value)
        {
            Console.Write($"{label}: ");
            if (!int.TryParse(Console.ReadLine(), out value))
            {
                value = devaultValue;
                Console.WriteLine(value);
            }
            Console.WriteLine();
        }

        private static void Query(string label, string devaultValue, out string value)
        {
            Console.Write($"{label}: ");
            value = Console.ReadLine();
            if (string.IsNullOrEmpty(value))
            {
                value = devaultValue;
                Console.WriteLine($" {value}");
            }
            else
                Console.WriteLine();
        }

        private static void RunDatagen(int minNodes, int maxNodes, int randomMoves, int hotMoves, int temp, int threads, IPlayoutWriter writer)
        {
            long totalPositionCount = 0;
            long totalStartTicks = Stopwatch.GetTimestamp();
            long[] startTicks = new long[threads];

            Parallel.For(0, threads, i =>
            {
                BoardState startPos = Notation.GetStartingPosition();
                BoardState board = new BoardState();
                List<Move> moveList = new List<Move>();
                List<short> scoreList = new List<short>();
                while (true)
                {
                    startTicks[i] = Stopwatch.GetTimestamp();
                    board.Copy(startPos);
                    if (PlayRandom(board, randomMoves))
                    {
                        moveList.Clear();
                        scoreList.Clear();
                        byte wdl = Playout(board.Clone(), minNodes, maxNodes, hotMoves, temp, moveList, scoreList);

                        long now = Stopwatch.GetTimestamp();
                        float duration = (float)(now - startTicks[i]) / Stopwatch.Frequency;
                        float speed = moveList.Count / duration;

                        totalPositionCount += moveList.Count;
                        float totalSpeed = totalPositionCount * Stopwatch.Frequency / (float)(now - totalStartTicks);
                        Console.WriteLine($"  T#{i} +{moveList.Count} in {duration:F2}s ({(int)speed}p/s) Total: {totalPositionCount} Speed: {(int)totalSpeed}p/s");

                        lock (writer)
                        {
                            writer.Write(board, randomMoves, wdl, moveList, scoreList);
                        }
                    }
                }
            });
        }

        private static bool PlayRandom(BoardState board, int randomMoves)
        {
            Random random = new Random();
            Move[] move = new Move[225];
            MoveGen moveGen = new MoveGen(move, 0);
            for (int i = 0; i < randomMoves; i++)
            {
                moveGen.Collect(board);
                int iMove = random.Next(moveGen.Next);
                if (!board.Play(move[iMove]))
                    return false;

                moveGen.Next = 0;
            }
            //Console.WriteLine(Notation.GetFen(board));
            return true;
        }

        private static byte Playout(BoardState board, int minNodes, int maxNodes, int hotMoves, int temp, List<Move> moves, List<short> scores)
        {
            Transpositions.IncreaseAge();

            SearchOptions searchOptions = SearchOptions.Default;
            searchOptions.Temperature = temp;
            searchOptions.MaxNodes = maxNodes;
            Dictionary<ulong, int> hashes = new Dictionary<ulong, int>();
            List<ulong> reps = new();

            while (true)
            {
                //done playing hot moves?
                if (moves.Count >= hotMoves)
                    searchOptions.Temperature = 0;

                if (board.HalfmoveClock == 0)
                    reps.Clear();
                reps.Add(board.ZobristHash);

                Move bestMove = default;
                var search = new IterativeSearch(board, searchOptions, reps.ToArray());
                int score = 0;
                while (search.NodesVisited < minNodes && search.Depth < IterativeSearch.MAX_PLY)
                {
                    search.SearchDeeper();
                    if (search.Aborted || search.PrincipalVariation.Length == 0)
                        break;

                    bestMove = search.PrincipalVariation[0];
                    score = search.Score;
                }

                if (score == 0 && bestMove == default)
                {
                    Console.WriteLine($"Stalemate! {Notation.GetFen(board)}");
                    return 1; //2 = White, 1 = Draw, 0 = Black
                }

                if (!board.Play(bestMove))
                    break;

                if (Evaluation.IsCheckmate(score) && Evaluation.GetMateDistance(score) == 1)
                {
                    Console.WriteLine($"{board.SideToMove} wins! {Notation.GetFen(board)}");
                    if (board.SideToMove == Color.White)
                        return 2; //White
                    else
                        return 0; //Black
                }

                if (board.HalfmoveClock > 99)
                {
                    Console.WriteLine($"Draw by 50-move rule! {Notation.GetFen(board)}");
                    moves.RemoveRange(moves.Count - 50, 50);
                    return 1; //2 = White, 1 = Draw, 0 = Black
                }

                if (hashes.TryGetValue(board.ZobristHash, out int count) && count == 2)
                {
                    //this is the 3rd repetion
                    Console.WriteLine($"Draw by threefold repetition! {Notation.GetFen(board)}");
                    moves.RemoveRange(moves.Count - 1, 1);
                    return 1; //2 = White, 1 = Draw, 0 = Black
                }

                //Console.Write($"{Notation.GetMoveName(bestMove)} ");
                hashes[board.ZobristHash] = count + 1;
                moves.Add(bestMove);
                scores.Add((short)score);
            }
            throw new Exception("Unreachable!");
        }
    }
}
