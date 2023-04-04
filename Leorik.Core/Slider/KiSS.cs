using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core.Slider
{
    //This class implements Mike Sherwin's Kindergarten Super SISSY Bitboards where SISSY stands for Split Index Sub Set Yielding
    //...henceforth we shall abreviate it as KiSS <3
    public static class KiSS
    {
        static readonly ulong[] Subset = new ulong[4 * 64 * 64];

        const int FILE_A = 0;
        const int FILE_H = 7;
        const int RANK_1 = 0;
        const int RANK_8 = 7;
        const ulong FILE_A2_A7 = 0x0001010101010100;
        const ulong DIAGONAL_C2_H7 = 0x0080402010080400;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            ulong offset = (ulong)(square << 7);
            ulong dIndex = (occupation & Blocker.DiagonalMask[square]) * FILE_A2_A7 >> 57;
            ulong aIndex = (occupation & Blocker.AntiDiagonalMask[square]) * FILE_A2_A7 >> 57;
            return Subset[offset + dIndex] | Subset[offset + 64 + aIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            ulong offset = 8192 + (ulong)(square << 7);
            ulong hIndex = occupation >> (square & 0b111000 | 1) & 63;
            ulong vIndex = (occupation >> (square & 0b000111) & FILE_A2_A7) * DIAGONAL_C2_H7 >> 58;
            return Subset[offset + hIndex] | Subset[offset + 64 + vIndex];
        }

        //Table initialization

        static KiSS()
        {
            //Init Subsets
            for (int square = 0; square < 64; square++)
                for (int index = 0; index < 64; index++)
                {
                    int offset = square * 128 + index;
                    Subset[offset] = GetDiagonalSubset(square, index);
                    Subset[offset + 64] = GetAntiDiagonalSubset(square, index);
                    Subset[offset + 8192] = GetHorizontalSubset(square, index);
                    Subset[offset + 8192 + 64] = GetVerticalSubset(square, index);
                }
        }

        static ulong GetDiagonalSubset(int square, int index)
        {
            ulong blockers = GetBlockers(square, index);
            ulong result = 0;
            for (int sq = square; IsFree(blockers, sq) && File(sq) < FILE_H && Rank(sq) < RANK_8; sq += 9)
                result |= 1UL << sq + 9;
            for (int sq = square; IsFree(blockers, sq) && File(sq) > FILE_A && Rank(sq) > RANK_1; sq -= 9)
                result |= 1UL << sq - 9;
            return result;
        }

        static ulong GetAntiDiagonalSubset(int square, int index)
        {
            ulong blockers = GetBlockers(square, index);
            ulong result = 0;
            for (int sq = square; IsFree(blockers, sq) && File(sq) > FILE_A && Rank(sq) < RANK_8; sq += 7)
                result |= 1UL << sq + 7;
            for (int sq = square; IsFree(blockers, sq) && File(sq) < FILE_H && Rank(sq) > RANK_1; sq -= 7)
                result |= 1UL << sq - 7;
            return result;
        }

        static ulong GetHorizontalSubset(int square, int index)
        {
            ulong blockers = GetBlockers(square, index);
            ulong result = 0;
            for (int sq = square; IsFree(blockers, sq) && File(sq) < FILE_H; sq++)
                result |= 1UL << sq + 1;
            for (int sq = square; IsFree(blockers, sq) && File(sq) > FILE_A; sq--)
                result |= 1UL << sq - 1;
            return result;
        }

        static ulong GetVerticalSubset(int square, int index)
        {
            ulong blockers = GetVerticalBlockers(square, index);
            ulong result = 0;
            for (int sq = square; IsVerticalFree(blockers, sq) && Rank(sq) < RANK_8; sq += 8)
                result |= 1UL << sq + 8;
            for (int sq = square; IsVerticalFree(blockers, sq) && Rank(sq) > RANK_1; sq -= 8)
                result |= 1UL << sq - 8;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsFree(ulong mask, int square) => (mask & 1UL << File(square)) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong GetBlockers(int square, int index) => (ulong)(index << 1) & ~(1UL << File(square));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsVerticalFree(ulong mask, int square) => (mask & 1UL << square - File(square)) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong GetVerticalBlockers(int square, int index)
        {
            //Place the 6 'index' bits FEDCBA like this, leave standing square ZERO 
            //  - - - - - - - - 
            //  A - - - - - - - 
            //  B - - - - - - - 
            //  C - - - - - - - 
            //  D - - - - - - - 
            //  E - - - - - - - 
            //  F - - - - - - - 
            //  - - - - - - - - 
            ulong blockers = 0;
            for (int i = 0, shift = 48; i < 6; i++, shift -= 8)
                if ((index & 1 << i) > 0) //index bit is set?
                    if (shift != square - File(square)) //don't block standing square
                        blockers |= 1UL << shift;
            return blockers;
        }

        //~~~ DEBUG ~~~

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _BishopAttacks(ulong occupation, int square)
        {
            ulong dIndex = (occupation & Blocker.DiagonalMask[square]) * FILE_A2_A7 >> 57;
            ulong aIndex = (occupation & Blocker.AntiDiagonalMask[square]) * FILE_A2_A7 >> 57;
            return GetDiagonalSubset(square, (int)dIndex) | GetAntiDiagonalSubset(square, (int)aIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _RookAttacks(ulong occupation, int square)
        {
            ulong hIndex = occupation >> (square & 0b111000 | 1) & 63;
            ulong vIndex = (occupation >> (square & 0b000111) & FILE_A2_A7) * DIAGONAL_C2_H7 >> 58;
            return GetHorizontalSubset(square, (int)hIndex) | GetVerticalSubset(square, (int)vIndex);
        }
    }
}
