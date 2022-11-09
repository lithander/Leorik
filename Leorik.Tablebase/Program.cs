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

            string tbPath = GetPath(args);
            Console.WriteLine($"Syzygy Tablebase Path: {tbPath}");
            Syzygy tb = new Syzygy(tbPath);

            Console.WriteLine();
            string fen = GetPosition(args);
            do
            {
                Probe(tb, fen);
                fen = Console.ReadLine();
            }
            while(!string.IsNullOrEmpty(fen));
        }

        private static void Probe(Syzygy tb, string fen)
        {
            BoardState pos = Notation.GetBoardState(fen);
            if (tb.ProbeWinDrawLoss(pos, out WinDrawLoss result))
                Console.WriteLine($"ProbeWinDrawLoss(...) = {(int)result} = {result}");
            else
                Console.WriteLine("ProbeWinDrawLoss(...) failed!");
        }

        private static string GetPosition(string[] args)
        {
            if (args.Length > 1)
                return args[1];

            Console.Write($"Enter FEN: ");
            return Console.ReadLine();

        }

        private static string GetPath(string[] args)
        {
            if (args.Length > 0)
                return args[0];

            Console.Write($"Provide the Syzygy Tablebase Path: ");
            return Console.ReadLine();
        }
    }
}
