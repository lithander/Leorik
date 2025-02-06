using Leorik.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Tuning
{
    static class BitboardUtils
    {
        public static void Repl()
        {
            while (true)
            {
                Console.Write("Fen: ");
                string fen = Console.ReadLine();
                if (fen == "")
                    break;
                //fen = "8/8/7p/1P2Pp1P/2Pp1PP1/8/8/8 w - - 0 1";
                void Print(ulong bits, string label)
                {
                    Console.WriteLine(label);
                    PrintBitboard(bits);
                    Console.WriteLine();
                }
                BoardState board = Notation.GetBoardState(fen);
                NeuralNetEval nneval = new NeuralNetEval(board);
                Console.WriteLine($"NNUE: {nneval.Score} for {board.SideToMove} ({board.Score()}");
            }
        }

        public static void PrintBitboard(ulong bits)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int sq = (7 - i) * 8 + j;
                    bool bit = (bits & (1UL << sq)) != 0;
                    Console.Write(bit ? "O " : "- ");
                }
                Console.WriteLine();
            }
        }

        public static void PrintPosition(BoardState board)
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
                    SetColor(piece, rank, file);
                    Console.Write(Notation.GetChar(piece));
                    Console.Write(' ');
                }
                Console.ResetColor();
                Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine(" '----------------'");
        }


        static void SetColor(Piece piece, int rank, int file)
        {
            if ((rank + file) % 2 == 1)
                Console.BackgroundColor = ConsoleColor.DarkGray;
            else
                Console.BackgroundColor = ConsoleColor.Black;

            if ((piece & Piece.ColorMask) == Piece.White)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void PrintData(Func<int, int> func)
        {
            Console.WriteLine("    A   B   C   D   E   F   G   H  ");
            Console.WriteLine(" .--------------------------------.");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($" |"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    Console.Write($"{func(square),3} ");
                }
                Console.WriteLine($"| {rank + 1}"); //ranks aren't zero-indexed
                if (rank >= 1)
                    Console.WriteLine(" |--------------------------------|");
            }
            Console.WriteLine(" '--------------------------------'");
        }

        private static void BlockPrint(List<string> bbStrings, int stride)
        {
            int next = 0;
            while (next < bbStrings.Count)
            {
                for (int i = 0; i < stride; i++)
                {
                    Console.Write(bbStrings[next++]);
                    if (next == bbStrings.Count)
                        break;
                    Console.Write(", ");
                }
                Console.WriteLine();
            }
        }

        //*********************
        //*** BB-GENERATORS ***
        //*********************

        public static ulong GenerateWhiteSquares()
        {
            ulong result = 0;
            for (int rank = 0; rank < 8; rank++)
                for (int file = 0; file < 8; file++)
                {
                    if (rank % 2 == file % 2)
                        continue;

                    int square = rank * 8 + file;
                    result |= (1UL << square);
                }
            return result;
        }

        public static void GenerateKingZone()
        {
            List<string> zoneStrings = new List<string>();
            for (int i = 0; i < 64; i++)
            {
                int rank = i / 8;
                int file = i & 7;
                if (rank == 0) rank++;
                if (rank == 7) rank--;
                if (file == 0) file++;
                if (file == 7) file--;
                int square = rank * 8 + file;
                //Debug.Assert(square == i, "");
                //ulong king = 1UL << i;
                ulong zone = Bitboard.KingTargets[square] | 1UL << square;
                zoneStrings.Add(Notation.GetHex(zone));
                Console.WriteLine(Notation.GetHex(zone));
            }
            Console.ReadLine();
            BlockPrint(zoneStrings, 4);
        }

    }
}
