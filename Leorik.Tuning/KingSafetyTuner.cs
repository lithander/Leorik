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
            for (int i = 0; i < 10; i++)
            {
                int c = (int)Math.Round(coefficients[offset + i * step]);
                Console.Write(c);
                Console.Write(", ");
            }
            Console.WriteLine();
        }
    }
}
