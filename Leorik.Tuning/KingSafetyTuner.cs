using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    static class KingSafetyTuner
    {
        internal static ulong GetKingPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                int square = Bitboard.LSB(board.Kings & board.Black);
                return board.Black & board.Pawns & Bitboard.KingTargets[square];
            }
            else //White
            {
                int square = Bitboard.LSB(board.White & board.Kings);
                return board.White & board.Pawns & Bitboard.KingTargets[square];
            }
        }

        internal static ulong GetPawnShield(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackKing = board.Black & board.Kings;
                ulong shield = Down(blackKing);
                shield |= Left(shield) | Right(shield);
                //shield |= Down(shield);
                return board.Black & board.Pawns & shield;
            }
            else //White
            {
                ulong whiteKing = board.White & board.Kings;
                ulong shield = Up(whiteKing);
                shield |= Left(shield) | Right(shield);
                //shield |= Up(shield);
                return board.White & board.Pawns & shield;
            }
        }

        internal static ulong GetKingZone(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                int square = Bitboard.LSB(board.Kings & board.Black);
                return (board.Kings & board.Black) | Bitboard.KingTargets[square];
            }
            else //White
            {
                int square = Bitboard.LSB(board.White & board.Kings);
                return (board.White & board.Kings) | Bitboard.KingTargets[square];
            }
        }

        internal static Feature[] GetPawnShieldFeatures(BoardState position, float phase)
        {            
            List<Feature> features = new List<Feature>();
            //Black PawnShield
            int index = Bitboard.PopCount(GetPawnShield(position, Color.Black));
            features.AddFeature(index, -1, phase, true);
            //White PawnShield
            index = Bitboard.PopCount(GetPawnShield(position, Color.White));
            features.AddFeature(index, 1, phase, true);

            //Black KingPawns
            index = 10 + Bitboard.PopCount(GetKingPawns(position, Color.Black));
            features.AddFeature(index, -1, phase, true);
            //White KingPawns
            index = 10 + Bitboard.PopCount(GetKingPawns(position, Color.White));
            features.AddFeature(index, 1, phase, true);
            return features.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FillUp(ulong bits)
        {
            bits |= (bits << 8);
            bits |= (bits << 16);
            bits |= (bits << 32);
            return bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FillDown(ulong bits)
        {
            bits |= (bits >> 8);
            bits |= (bits >> 16);
            bits |= (bits >> 32);
            return bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Up(ulong bits) => bits << 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Down(ulong bits) => bits >> 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Left(ulong bits) => (bits & 0xFEFEFEFEFEFEFEFEUL) >> 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Right(ulong bits) => (bits & 0x7F7F7F7F7F7F7F7FUL) << 1;

        internal static void Report(int offset, int step, float[] coefficients)
        {
            for (int i = 0; i < 20; i++)
            {
                int c = (int)Math.Round(coefficients[offset + i * step]);
                Console.Write(c);
                Console.Write(", ");
            }
            Console.WriteLine();
        }

        internal static Feature[] GetKingThreatsFeatures(BoardState position, float phase)
        {
            List<Feature> features = new List<Feature>();
            //Black PawnShield
            int index = Features.CountWhiteKingThreats(position);
            features.AddFeature(index, -1, phase, true);
            //White PawnShield
            index = Features.CountBlackKingThreats(position);
            features.AddFeature(index, 1, phase, true);

            return features.ToArray();
        }

        internal static void Print(BoardState board)
        {
            EvalTerm eval = default;
            KingSafety.Update(board, ref eval);
            //PrintWhiteKing(board);
            //PrintBlackKing(board);
        }

        private static void PrintWhiteKing(BoardState board)
        {
            ulong kingZone = GetKingZone(board, Color.White);
            PrintBitboard('K', kingZone);
            ulong occupied = 0;// board.Black | board.White;
            int square;
            int sum = 0;
            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                sum += PrintBitboard('n', Bitboard.KnightTargets[square] & kingZone);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                sum += PrintBitboard('b', Bitboard.GetBishopTargets(occupied, square) & kingZone);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                sum += PrintBitboard('r', Bitboard.GetRookTargets(occupied, square) & kingZone);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                sum += PrintBitboard('q', Bitboard.GetQueenTargets(occupied, square) & kingZone);
            }
            Console.WriteLine(sum);
            if (sum != Features.CountWhiteKingThreats(board))
                throw new Exception();
        }

        private static void PrintBlackKing(BoardState board)
        {
            ulong kingZone = GetKingZone(board, Color.Black);
            PrintBitboard('k', kingZone);
            ulong occupied = 0;// board.Black | board.White;
            int square;
            int sum = 0;
            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                sum += PrintBitboard('N', Bitboard.KnightTargets[square] & kingZone);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                sum += PrintBitboard('B', Bitboard.GetBishopTargets(occupied, square) & kingZone);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                sum += PrintBitboard('R', Bitboard.GetRookTargets(occupied, square) & kingZone);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                sum += PrintBitboard('Q', Bitboard.GetQueenTargets(occupied, square) & kingZone);
            }
            Console.WriteLine(sum);
            if (sum != Features.CountBlackKingThreats(board))
                throw new Exception();
        }

        static int PrintBitboard(char label, ulong bits)
        {
            if (bits == 0)
                return 0;
            int count = 0;
            Console.WriteLine();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int sq = (7 - i) * 8 + j;
                    bool bit = (bits & (1UL << sq)) != 0;
                    if(bit)
                        count++;
                    Console.Write(bit ? label+" " : "- ");
                }
                Console.WriteLine();
            }
            return count;
        }
    }
}
