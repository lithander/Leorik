using Leorik.Core;
using Leorik.Search;
using System.Diagnostics;

namespace Leorik.Tuning
{
    interface IPlayoutWriter
    {
        void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves);
    }

    class BinaryPlayoutWriter : IPlayoutWriter, IDisposable
    {
        BinaryWriter _stream;
        PackedBoard _packedBoard = new();

        public BinaryPlayoutWriter(string filePath)
        {
            _stream = new BinaryWriter(new FileStream(filePath, FileMode.Create));
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves)
        {
            byte moveCount = (byte)moves.Count;
            _packedBoard.Pack(position, (short)randomMoves, position.Eval.Score, wdl, moveCount);
            _packedBoard.Write(_stream);
            for (int i = 0; i < moveCount; i++)
                _stream.Write((int)moves[i]);
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

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves)
        {
            byte moveCount = (byte)moves.Count;
            _stream.WriteLine(Notation.GetFen(position, randomMoves));
            _stream.WriteLine(position.Eval.Score);
            _stream.WriteLine(wdl);
            _stream.WriteLine(moveCount);
            for (int i = 0; i < moveCount; i++)
            {
                if (i > 0)
                    _stream.Write(' ');
                _stream.Write((int)moves[i]);
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

        public void Write(BoardState position, int randomMoves, byte wdl, List<Move> moves)
        {
            _writerA.Write(position, randomMoves, wdl, moves);
            _writerB.Write(position, randomMoves, wdl, moves);
        }
    }

    internal class DataGen
    {
        const int RANDOM_MOVES = 12;
        const int NODE_COUNT = 100_000;
        const string OUTPUT_PATH = "D:/Projekte/Chess/Leorik/TD2/";

        public static void RunPrompt()
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine(" Leorik DataGen v2 ");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine();

            Console.WriteLine("Threads:");
            if (!int.TryParse(Console.ReadLine(), out int threads))
                threads = 1;

            Console.WriteLine("Number of random moves:");
            if (!int.TryParse(Console.ReadLine(), out int randomMoves))
                randomMoves = RANDOM_MOVES;

            Console.WriteLine("Nodes:");
            if (!int.TryParse(Console.ReadLine(), out int nodes))
                nodes = NODE_COUNT;

            Console.WriteLine("Path:");
            string path = Console.ReadLine();
            if (string.IsNullOrEmpty(path))
                path = OUTPUT_PATH;

            Console.WriteLine("File:");
            string suffix = $"_{nodes / 1000}K_{randomMoves}RM";
            string fileName = Console.ReadLine();
            if (string.IsNullOrEmpty(fileName))
                fileName = DateTime.Now.ToString("s").Replace(':', '.') + suffix;

            DoublePlayoutWriter writer = new DoublePlayoutWriter(path, fileName);
            RunDatagen(randomMoves, nodes, writer, threads);
        }

        private static void RunDatagen(int randomMoves, int nodes, IPlayoutWriter writer, int threads)
        {
            long positionCount = 0;
            long startTicks = Stopwatch.GetTimestamp();
            Parallel.For(0, threads, i =>
            {
                BoardState startPos = Notation.GetStartingPosition();
                BoardState board = new BoardState();
                List<Move> moves = new List<Move>();
                while (true)
                {
                    board.Copy(startPos);
                    if (PlayRandom(board, randomMoves))
                    {
                        moves.Clear();
                        byte wdl = Playout(board.Clone(), 50, nodes, moves);
                        lock (writer)
                        {
                            positionCount += moves.Count;
                            long delta = Stopwatch.GetTimestamp() - startTicks;
                            float positionsPerSecond = positionCount * Stopwatch.Frequency / (float)delta;
                            Console.WriteLine($"T#{i} {positionCount} {(int)positionsPerSecond}pos/s");

                            writer.Write(board, randomMoves, wdl, moves);
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
            for(int i = 0; i < randomMoves; i++) 
            {
                moveGen.Collect(board);
                int iMove = random.Next(moveGen.Next);
                if(!board.Play(move[iMove]))
                    return false;

                moveGen.Next = 0;
            }
            //Console.WriteLine(Notation.GetFen(board));
            return true;
        }

        private static byte Playout(BoardState board, int maxDepth, int maxNodes, List<Move> moves)
        {
            SearchOptions searchOptions = SearchOptions.Default;
            searchOptions.MaxNodes = maxNodes;
            Dictionary<ulong, int> hashes = new Dictionary<ulong, int>();
            List<ulong> reps = new();

            while (true)
            {
                if (board.HalfmoveClock == 0)
                    reps.Clear();
                reps.Add(board.ZobristHash);

                Move bestMove = default;
                var search = new IterativeSearch(board, searchOptions, reps.ToArray());
                while (search.Depth < maxDepth)
                {
                    search.SearchDeeper();
                    if (search.Aborted || search.PrincipalVariation.Length == 0)
                        break;

                    bestMove = search.PrincipalVariation[0];
                }

                if (search.Score == 0 && bestMove == default)
                {
                    Console.WriteLine($"Stalemate! {Notation.GetFen(board)}");
                    return 1; //2 = White, 1 = Draw, 0 = Black
                }

                if (!board.Play(bestMove))
                    break;

                if (Evaluation.IsCheckmate(search.Score) && Evaluation.GetMateDistance(search.Score) == 1)
                {
                    Console.WriteLine($"{board.SideToMove} lost! {Notation.GetFen(board)}");
                    if (board.SideToMove == Color.White)
                        return 0; //Black
                    else
                        return 2; //White
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
            }
            throw new Exception("Unreachable!");
        }
    }
}
