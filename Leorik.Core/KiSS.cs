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
        //static ulong[] HorizontalSubset = new ulong[64 * 64];
        static ulong[] BishopSubset = new ulong[2 * 64 * 64];
        //static ulong[] AntiDiagonalSubset = new ulong[64 * 64];

        const int FILE_A = 0;
        const int FILE_H = 7;
        const int RANK_1 = 0;
        const int RANK_8 = 7;

        static KiSS()
        {
            for (int i = 0; i < 64; i++)
            {
                InitDiagonalMasks(i);
                InitBishopSubsets(i);
                InitRookSubsets(i);
            }

            //for (int i = 0; i < 64; i++)
            //{
            //    Console.WriteLine("HorizontalMask");
            //    PrintBitboard(HorizontalMask[i]);
            //    Console.WriteLine("OrthogonalMask");
            //    PrintBitboard(Bitboard.OrthogonalMask[i]);
            //}
        }

        private static void InitDiagonalMasks(int square)
        {
            int ts, dx, dy;
            int x = square % 8;
            int y = square / 8;

            // Initialize Kindergarten Super SISSY Bitboards
            // diagonals
            for (ts = square + 9, dx = x + 1, dy = y + 1; dx < FILE_H && dy < RANK_8; DiagonalMask[square] |= 1UL << ts, ts += 9, dx++, dy++) ;
            for (ts = square - 9, dx = x - 1, dy = y - 1; dx > FILE_A && dy > RANK_1; DiagonalMask[square] |= 1UL << ts, ts -= 9, dx--, dy--) ;

            // anti diagonals
            for (ts = square + 7, dx = x - 1, dy = y + 1; dx > FILE_A && dy < RANK_8; AntiDiagonalMask[square] |= 1UL << ts, ts += 7, dx--, dy++) ;
            for (ts = square - 7, dx = x + 1, dy = y - 1; dx < FILE_H && dy > RANK_1; AntiDiagonalMask[square] |= 1UL << ts, ts -= 7, dx++, dy--) ;
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

        static void InitBishopSubsets(int square)
        {
            ulong offset = (ulong)(square << 7);
            int ts;
            ulong occ, index;

            // diagonal indexes
            for (index = 0; index < 64; index++)
            {
                BishopSubset[offset | index] = 0;
                occ = index << 1;
                if ((square & 7) != FILE_H && (square >> 3) != RANK_8)
                {
                    for (ts = square + 9; ; ts += 9)
                    {
                        BishopSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_H || (ts >> 3) == RANK_8) break;
                    }
                }
                if ((square & 7) != FILE_A && (square >> 3) != RANK_1)
                {
                    for (ts = square - 9; ; ts -= 9)
                    {
                        BishopSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_A || (ts >> 3) == RANK_1) break;
                    }
                }
            }

            // anti diagonal indexes
            offset += 64;
            for (index = 0; index < 64; index++)
            {
                BishopSubset[offset | index] = 0;
                occ = index << 1;
                if ((square & 7) != FILE_A && (square >> 3) != RANK_8)
                {
                    for (ts = square + 7; ; ts += 7)
                    {
                        BishopSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_A || (ts >> 3) == RANK_8) break;
                    }
                }
                if ((square & 7) != FILE_H && (square >> 3) != RANK_1)
                {
                    for (ts = square - 7; ; ts -= 7)
                    {
                        BishopSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_H || (ts >> 3) == RANK_1) break;
                    }
                }
            }
        }

        static void InitRookSubsets(int square)
        {
            ulong offset = (ulong)(square << 7);
            int ts;
            ulong occ, index;

            // vertical indexes
            for (index = 0; index < 64; index++)
            {
                RookSubset[offset | index] = 0;
                ulong blockers = 0;
                for (int i = 0; i <= 5; i++)
                {
                    if ((index & (1UL << i)) > 0) 
                    {
                        blockers |= (1UL << (((5 - i) << 3) + 8));
                    }
		        }
		        if ((square >> 3) != RANK_8)
                {
                    for (ts = square + 8; ; ts += 8)
                    {
                        RookSubset[offset | index] |= (1UL << ts);
                        if ((blockers & (1UL << (ts - (ts & 7)))) > 0)
                            break;
                        if ((ts >> 3) == RANK_8) break;
                    }
		        }
		        if ((square >> 3) != RANK_1)
                {
                    for (ts = square - 8; ; ts -= 8)
                    {
                        RookSubset[offset | index] |= (1UL << ts);
                        if ((blockers & (1UL << (ts - (ts & 7)))) > 0) break;
                        if ((ts >> 3) == RANK_1) break;
                    }
		        }
	        }

            // horizontal indexes
            offset += 64;
            for (index = 0; index < 64; index++)
            {
                RookSubset[offset | index] = 0;
                occ = index << 1;
                if ((square & 7) != FILE_H)
                {
                    for (ts = square + 1; ; ts += 1)
                    {
                        RookSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_H) break;
			        }
		        }
		        if ((square & 7) != FILE_A)
                {
                    for (ts = square - 1; ; ts -= 1)
                    {
                        RookSubset[offset | index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILE_A) break;
                    }
		        }
	        }
        }

        const ulong FILE_B2_B7 = 0x0002020202020200;
        const ulong FILE_A2_A7 = 0x0001010101010100;
        const ulong DIAGONAL_C2_H7 = 0x0080402010080400;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static ulong BishopAttacks(ulong occupation, int square)
        //{
        //    return DiagonalSubset[square, ((occupation & DiagonalMask[square]) * FILE_B2_B7) >> 58] |
        //           AntiDiagonalSubset[square, ((occupation & AntiDiagonalMask[square]) * FILE_B2_B7) >> 58];
        //}
        //
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static ulong RookAttacks(ulong occupation, int square)
        //{
        //    return HorizontalSubset[square, (occupation >> shift_horizontal_table[square]) & 63] |
        //           VerticalSubset[square, (((occupation >> (square & 7)) & FILE_A2_A7) * DIAGONAL_C2_H7) >> 58];
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _BishopAttacks(ulong occupation, int square)
        {
            ulong dIndex = ((occupation & DiagonalMask[square]) * FILE_B2_B7) >> 58;
            ulong aIndex = ((occupation & AntiDiagonalMask[square]) * FILE_B2_B7) >> 58;
            ulong offset = (ulong)(square << 7);
            return BishopSubset[offset | dIndex] | BishopSubset[64 | offset | aIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong _RookAttacks(ulong occupation, int square)
        {
            ulong hIndex = (occupation >> ((square & 0b11111000) | 1)) & 63;
            ulong vIndex = (((occupation >> (square & 0b00000111)) & FILE_A2_A7) * DIAGONAL_C2_H7) >> 58;
            ulong offset = (ulong)(square << 7);
            return RookSubset[64 | offset | hIndex] | RookSubset[offset | vIndex];
        }
    }
}
