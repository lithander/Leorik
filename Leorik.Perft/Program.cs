using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Perft
{
    class Program
    {
        private const int MAX_PLY = 10;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private static BoardState[] Positions;
        private static Move[] Moves;

        static Program()
        {
            Network.LoadDefaultNetwork();
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];
        }

        static void Main()
        {
            Console.WriteLine($"Leorik Perft {Bitboard.SliderMode}");
            Console.WriteLine();
            int line = 1;
            long totalNodes = 0;
            double totalDuration = 0;
            var file = File.OpenText("qbb.txt");
            while (!file.EndOfStream)
            {
                //The parser expects a fen-string followed by a depth and a perft results at that depth
                //Example: 4k3 / 8 / 8 / 8 / 8 / 8 / 8 / 4K2R w K - 0 1; D1 15; D2 66; 6; 764643
                Parse(file, out BoardState position, out int depth, out long refResult);

                long t0 = Stopwatch.GetTimestamp();
                long result = Perft(position, depth);
                long t1 = Stopwatch.GetTimestamp();

                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                double ms = (1000 * dt);
                totalNodes += result;
                totalDuration += dt;

                if (result != refResult)
                    Console.WriteLine($"{line++} ERROR! perft({depth})={result}, expected {refResult} ({result - refResult:+#;-#})");
                else
                    Console.WriteLine($"{line++} OK! {(int)ms}ms, {(int)(result / ms)}K NPS");
            }
            file.Close();
            Console.WriteLine();
            Console.WriteLine($"Total: {totalNodes} Nodes, {(int)(1000 * totalDuration)}ms, {(int)(totalNodes / totalDuration / 1000)}K NPS");
            Console.WriteLine();
            Console.WriteLine("Press any key to quit");//stop command prompt from closing automatically on windows
            Console.ReadKey();
        }

        private static void Parse(StreamReader file, out BoardState position, out int depth, out long refResult)
        {
            string[] data = file.ReadLine().Split(';');
            string fen = data[0];
            position = Notation.GetBoardState(fen);
            depth = int.Parse(data[1]);
            refResult = long.Parse(data[2]);
        }

        private static long Perft(BoardState pos, int depth)
        {
            Positions[0].Copy(pos);
            return Perft(0, depth, new PerftMoveGen(Moves, 0));
        }

        //TODO:
        //- encode wether a move is legal or not in move.Target, play without testing legality if flag is set
        //- use a special move-gen when in check //https://chess.stackexchange.com/questions/35199/how-can-a-check-evasion-move-generation-algorithm-be-done-efficiently
        //- when not in check set the legal flag where appropriate //https://talkchess.com/forum3/viewtopic.php?f=7&t=80952&start=20#p937332 & https://talkchess.com/forum3/viewtopic.php?f=7&t=81265
        //- if movegen needs to evaluate "inCheck()" info anyway and search does also eval it frequently
        //  -> make it a field of the Position set immediately after playing the move

        private static long Perft(int depth, int remaining, PerftMoveGen moves)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int i = moves.Next;
            moves.Collect(current);
            long sum = 0;
            for (; i < moves.Next; i++)
            {
                if (next.PlayWithoutHashAndEval(current, ref Moves[i]))
                {
                    if (remaining > 1)
                        sum += Perft(depth + 1, remaining - 1, moves);
                    else
                        sum++;
                }
            }
            return sum;
        }
    }
}
