using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Perft
{
    class Program
    {
        private static Perft _perft;

        static Program()
        {
            Network.LoadDefaultNetwork();
            _perft = new Perft();
        }

        static void Main()
        {
            Console.WriteLine($"Leorik Perft {Bitboard.SliderMode}");
            //Console.WriteLine();
            //RunStandardPerft();
            Console.WriteLine();
            RunFischerPerft(5);
            Console.WriteLine();
            Console.WriteLine("Press any key to quit");//stop command prompt from closing automatically on windows
            Console.ReadKey();
        }

        private static void RunStandardPerft()
        {
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
                long result = _perft.Compute(position, depth);
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
        }


        private static void Parse(StreamReader file, out BoardState position, out int depth, out long refResult)
        {
            string[] data = file.ReadLine().Split(';');
            string fen = data[0];
            position = Notation.GetBoardState(fen);
            depth = int.Parse(data[1]);
            refResult = long.Parse(data[2]);
        }

        private static void RunFischerPerft(int depth, int skip = 0)
        {
            int line = 1;
            long totalNodes = 0;
            double totalDuration = 0;
            var file = File.OpenText("fischer.epd");
            while (!file.EndOfStream)
            {
                //The parser expects a fen-string followed by a number of depth and a perft combinations
                //Example: 4k3 / 8 / 8 / 8 / 8 / 8 / 8 / 4K2R w K - 0 1; D1 15; D2 66; 
                Parse(file, out string fen, out BoardState position, depth, out long refResult);
                string myFen = Notation.GetFen(position);
                Console.WriteLine(myFen);
                if (skip > 0)
                {
                    line++;
                    skip--;
                    continue;
                }

                long t0 = Stopwatch.GetTimestamp();
                long result = _perft.Compute(Notation.GetBoardState(myFen), depth);
                long t1 = Stopwatch.GetTimestamp();

                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                double ms = (1000 * dt);
                totalNodes += result;
                totalDuration += dt;

                if (result != refResult)
                    Console.WriteLine($"{line++} {fen} ERROR! perft({depth})={result}, expected {refResult} ({result - refResult:+#;-#})");
                else
                    Console.WriteLine($"{line++} OK! {(int)ms}ms, {(int)(result / ms)}K NPS");
            }
            file.Close();
            Console.WriteLine();
            Console.WriteLine($"Total: {totalNodes} Nodes, {(int)(1000 * totalDuration)}ms, {(int)(totalNodes / totalDuration / 1000)}K NPS");
        }

        private static void Parse(StreamReader file, out string fen, out BoardState position, int depth, out long refResult)
        {
            string[] data = file.ReadLine().Split(';');
            fen = data[0];
            position = Notation.GetBoardState(fen);
            string[] token = data[depth].Split(' ');
            (string depthToken, string perftToken) = (token[0], token[1]);
            if(int.Parse(depthToken.Substring(1)) != depth)
                throw new Exception($"Depth mismatch! {depthToken} != {depth}");
            refResult = long.Parse(perftToken);
        }
    }
}
