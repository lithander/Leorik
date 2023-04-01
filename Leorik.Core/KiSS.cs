using System;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core
{
    //This class implements Mike Sherwin's Kindergarten Super SISSY Bitboards where SISSY stands for Split Index Sub Set Yielding
    //...henceforth we shall abreviate it as KiSS <3
    public static class KiSS
    {
        static ulong[] DiagonalMask = new ulong[64];
        static ulong[] AntiDiagonalMask = new ulong[64];

        static ulong[] RookSubset = new ulong[2 * 64 * 64];
        static ulong[] BishopSubset = new ulong[2 * 64 * 64];

        const int FILE_A = 0;
        const int FILE_H = 7;
        const int RANK_1 = 0;
        const int RANK_8 = 7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Rank(int square) => square >> 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int File(int square) => square & 7;

        static bool IsBlocked(ulong mask, int square) => (mask & (1UL << File(square))) > 0;

        static KiSS()
        {
            InitDiagonalMasks();
            InitSubsets();
        }

        private static void InitSubsets()
        {
            for (int square = 0; square < 64; square++)
            {
                int offset = square * 128;
                for (int index = 0; index < 64; index++)
                {
                    BishopSubset[offset + index] = GetDiagonalSubset(square, index);
                    BishopSubset[offset + index + 64] = GetAntiDiagonalSubset(square, index);
                    RookSubset[offset + index] = GetHorizontalSubset(square, index);
                    RookSubset[offset + index + 64] = GetVerticalSubset(square, index);
                }
            }
        }

        private static void InitDiagonalMasks()
        {
            for (int square = 0; square < 64; square++)
            {
                int x = File(square);
                int y = Rank(square);

                //set diagonal bits above square
                for (int bit = square + 9, dx = x + 1, dy = y + 1; dx < FILE_H && dy < RANK_8; bit += 9, dx++, dy++)
                    DiagonalMask[square] |= 1UL << bit;

                //set diagonal bits below square
                for (int bit = square - 9, dx = x - 1, dy = y - 1; dx > FILE_A && dy > RANK_1; bit -= 9, dx--, dy--)
                    DiagonalMask[square] |= 1UL << bit;

                //set anti-diagonal bits above square
                for (int bit = square + 7, dx = x - 1, dy = y + 1; dx > FILE_A && dy < RANK_8; bit += 7, dx--, dy++)
                    AntiDiagonalMask[square] |= 1UL << bit;

                //set anti-diagonals bits below square
                for (int bit = square - 7, dx = x + 1, dy = y - 1; dx < FILE_H && dy > RANK_1; bit -= 7, dx++, dy--)
                    AntiDiagonalMask[square] |= 1UL << bit;
            }
        }

        static ulong GetDiagonalSubset(int square, int index)
        {
            ulong result = 0;
            ulong blockers = (ulong)(index << 1);

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) < FILE_H && Rank(sq) < RANK_8; sq += 9)
                result |= 1UL << (sq + 9);

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) > FILE_A && Rank(sq) > RANK_1; sq -= 9)
                result |= 1UL << (sq - 9);

            return result;
        }

        static ulong GetAntiDiagonalSubset(int square, int index)
        {
            ulong result = 0;
            ulong blockers = (ulong)(index << 1);

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) > FILE_A && Rank(sq) < RANK_8; sq += 7)
                result |= 1UL << (sq + 7);

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) < FILE_H && Rank(sq) > RANK_1; sq -= 7)
                result |= 1UL << (sq - 7);

            return result;
        }

        static ulong GetHorizontalSubset(int square, int index)
        {
            ulong result = 0;
            ulong blockers = (ulong)(index << 1);
            //unblock the starting square
            blockers &= ~(1UL << File(square));

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) < FILE_H; sq++)
                result |= 1UL << (sq + 1);

            for (int sq = square; !IsBlocked(blockers, sq) && File(sq) > FILE_A; sq--)
                result |= 1UL << (sq - 1);

            return result;
        }

        static ulong GetVerticalSubset(int square, int index)
        {
            // vertical indexes
            ulong result = 0;
            ulong blockers = 0;
            for (int i = 0; i <= 5; i++)
            {
                if ((index & (1 << i)) > 0)
                {
                    int shift = ((5 - i) << 3) + 8;
                    blockers |= 1UL << shift;
                }
            }
            for (int sq = square; Rank(sq) < RANK_8; sq += 8)
            {
                int up = sq + 8;
                result |= 1UL << up;
                if ((blockers & (1UL << (up - File(up)))) > 0) 
                    break;
            }
            for (int sq = square; Rank(sq) > RANK_1; sq -= 8)
            {
                int down = sq - 8;
                result |= 1UL << down;
                if ((blockers & (1UL << (down - File(down)))) > 0) 
                    break;
            }
            return result;
        }

        const ulong FILE_B2_B7 = 0x0002020202020200;
        const ulong FILE_A2_A7 = 0x0001010101010100;
        const ulong DIAGONAL_C2_H7 = 0x0080402010080400;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            ulong offset = (ulong)(square << 7);
            ulong dIndex = ((occupation & DiagonalMask[square]) * FILE_B2_B7) >> 58;
            ulong aIndex = ((occupation & AntiDiagonalMask[square]) * FILE_B2_B7) >> 58;
            return BishopSubset[offset + dIndex] | BishopSubset[offset + 64 + aIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _BishopAttacks(ulong occupation, int square)
        {
            ulong dIndex = ((occupation & DiagonalMask[square]) * FILE_B2_B7) >> 58;
            ulong aIndex = ((occupation & AntiDiagonalMask[square]) * FILE_B2_B7) >> 58;
            return GetDiagonalSubset(square, (int)dIndex) | GetAntiDiagonalSubset(square, (int)aIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            ulong offset = (ulong)(square << 7);
            ulong hIndex = (occupation >> ((square & 0b111000) | 1)) & 63;
            ulong vIndex = (((occupation >> (square & 0b000111)) & FILE_A2_A7) * DIAGONAL_C2_H7) >> 58;
            return RookSubset[offset + hIndex] | RookSubset[offset + 64 + vIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _RookAttacks(ulong occupation, int square)
        {
            ulong hIndex = (occupation >> ((square & 0b111000) | 1)) & 63;
            ulong vIndex = (((occupation >> (square & 0b000111)) & FILE_A2_A7) * DIAGONAL_C2_H7) >> 58;
            return GetHorizontalSubset(square, (int)hIndex) | GetVerticalSubset(square, (int)vIndex);
        }
    }
}
