using Leorik.Core;
using System;

namespace Leorik.Tablebase
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine(" Leorik Syzygy v1  ");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine();

            string tbPath = args.Length > 0 ? args[0] : ReadPath();
            Console.WriteLine($"Syzygy Tablebase Path: {tbPath}");
            Syzygy tb = new Syzygy(tbPath);

            Console.WriteLine();
            string fen = args.Length > 1 ? args[1] : ReadFen();
            while (!string.IsNullOrEmpty(fen))
            {
                Probe(tb, fen);
                fen = ReadFen();
            }
        }

        private static void Probe(Syzygy tb, string fen)
        {
            BoardState pos = Notation.GetBoardState(fen);
            if (tb.ProbeWinDrawLoss(pos, out WinDrawLoss result))
                Console.WriteLine($"ProbeWinDrawLoss({fen}) = {(int)result} = {result}");
            else
                Console.WriteLine($"ProbeWinDrawLoss({fen}) failed!");
        }

        private static string ReadFen()
        {
            Console.Write($"Enter FEN: ");
            return Console.ReadLine();
        }

        private static string ReadPath()
        {
            Console.Write($"Provide the Syzygy Tablebase Path: ");
            return Console.ReadLine();
        }
    }
}
