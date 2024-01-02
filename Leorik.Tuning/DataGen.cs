﻿using Leorik.Core;
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
        PackedBoard _packedBoard = new();

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
            _packedBoard.Pack(position, (short)randomMoves, position.Eval.Score, wdl, moveCount);
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
            _stream.WriteLine(position.Eval.Score);
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

            PackedBoard packed = new PackedBoard();
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
        const int VERSION = 4;
        const int RANDOM_MOVES = 12;
        const int DEPTH = 12;
        const int NODE_COUNT = int.MaxValue;
        const string OUTPUT_PATH = "D:/Projekte/Chess/Leorik/TD2/";

        public static void RunPrompt()
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine($" Leorik DataGen v{VERSION}");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine();

            Query("Threads", 1, out int threads);
            Query("Number of random moves", RANDOM_MOVES, out int randomMoves);
            Query("Nodes", NODE_COUNT, out int nodes);
            Query("Depth", DEPTH, out int depth);
            Query("Temperature", 0, out int temp);

            Query("Path", OUTPUT_PATH, out string path);

            string suffix = nodes == int.MaxValue ? "" : $"_{nodes / 1000}K";
            suffix += $"_{depth}D_{randomMoves}R_{temp}T_v{VERSION}";
            string fileName = DateTime.Now.ToString("s").Replace(':', '.') + suffix;
            Query("File", fileName, out fileName);

            DoublePlayoutWriter writer = new DoublePlayoutWriter(path, fileName);
            RunDatagen(randomMoves, nodes, depth, temp, writer, threads);
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

        private static void RunDatagen(int randomMoves, int nodes, int depth, int temp, IPlayoutWriter writer, int threads)
        {
            long positionCount = 0;
            long startTicks = Stopwatch.GetTimestamp();
            Parallel.For(0, threads, i =>
            {
                BoardState startPos = Notation.GetStartingPosition();
                BoardState board = new BoardState();
                List<Move> moves = new List<Move>();
                List<short> scores = new List<short>();
                while (true)
                {
                    board.Copy(startPos);
                    if (PlayRandom(board, randomMoves))
                    {
                        moves.Clear();
                        scores.Clear();
                        byte wdl = Playout(board.Clone(), depth, nodes, temp, moves, scores);
                        lock (writer)
                        {
                            positionCount += moves.Count;
                            long delta = Stopwatch.GetTimestamp() - startTicks;
                            float positionsPerSecond = positionCount * Stopwatch.Frequency / (float)delta;
                            Console.WriteLine($"T#{i} {positionCount} {(int)positionsPerSecond}pos/s");

                            writer.Write(board, randomMoves, wdl, moves, scores);
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

        private static byte Playout(BoardState board, int maxDepth, int maxNodes, int temp, List<Move> moves, List<short> scores)
        {
            SearchOptions searchOptions = SearchOptions.Default;
            searchOptions.MaxNodes = maxNodes;
            searchOptions.Temperature = temp;
            Dictionary<ulong, int> hashes = new Dictionary<ulong, int>();
            List<ulong> reps = new();

            while (true)
            {
                if (board.HalfmoveClock == 0)
                    reps.Clear();
                reps.Add(board.ZobristHash);

                Move bestMove = default;
                var search = new IterativeSearch(board, searchOptions, reps.ToArray());
                int score = 0;
                while (search.Depth < maxDepth)
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