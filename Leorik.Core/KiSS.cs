using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core
{
    //This class implements Mike Sherwin's Kindergarten Super SISSY Bitboards where SISSY stands for Split Index Sub Set Yielding
    //...henceforth we shall abreviate it as KiSS <3
    public static class KiSS
    {
        static ulong[] hMask = new ulong[64];
        static ulong[] dMask = new ulong[64];
        static ulong[] aMask = new ulong[64];
        static ulong[,] vSubset = new ulong[64,64];
        static ulong[,] hSubset = new ulong[64, 64];
        static ulong[,] dSubset = new ulong[64, 64];
        static ulong[,] aSubset = new ulong[64, 64];
        static int[] shift_horizontal_table = new int[64]; // new lookup table for shifts in calculation of hIndex

        const ulong Size = (64 * 64 * 4 + 64 * 4) * sizeof(ulong) + 64 * sizeof(uint);

        const int FILEa = 0;
        const int FILEh = 7;
        const int RANK1 = 0;
        const int RANK8 = 7;

        static KiSS()
        {
            for (int i = 0; i < 64; i++)
            {
                InitSquare(i);
            }

            for (int i = 0; i < 64; i++)
            {
                shift_horizontal_table[i] = (i & 56) + 1;
            }
        }

        static void InitSquare(int sq)
        {
            int ts, dx, dy;
            ulong occ, index;
            int x = sq % 8;
            int y = sq / 8;

            // Initialize Kindergarten Super SISSY Bitboards
            // diagonals
            for (ts = sq + 9, dx = x + 1, dy = y + 1; dx < FILEh && dy < RANK8; dMask[sq] |= 1UL << ts, ts += 9, dx++, dy++) ;
            for (ts = sq - 9, dx = x - 1, dy = y - 1; dx > FILEa && dy > RANK1; dMask[sq] |= 1UL << ts, ts -= 9, dx--, dy--) ;

            // anti diagonals
            for (ts = sq + 7, dx = x - 1, dy = y + 1; dx > FILEa && dy < RANK8; aMask[sq] |= 1UL << ts, ts += 7, dx--, dy++) ;
            for (ts = sq - 7, dx = x + 1, dy = y - 1; dx < FILEh && dy > RANK1; aMask[sq] |= 1UL << ts, ts -= 7, dx++, dy--) ;

            // diagonal indexes
            for (index = 0; index < 64; index++)
            {
                dSubset[sq, index] = 0;
                occ = index << 1;
                if ((sq & 7) != FILEh && (sq >> 3) != RANK8)
                {
                    for (ts = sq + 9; ; ts += 9)
                    {
                        dSubset[sq, index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILEh || (ts >> 3) == RANK8) break;
                    }
                }
		        if ((sq & 7) != FILEa && (sq >> 3) != RANK1)
		        {
			        for (ts = sq - 9; ; ts -= 9)
			        {
				        dSubset[sq, index] |= (1UL << ts);
				        if ((occ & (1UL << (ts & 7))) > 0) break;
				        if ((ts & 7) == FILEa || (ts >> 3) == RANK1) break;
			        }
		        }
        	}

	        // anti diagonal indexes
	        for (index = 0; index < 64; index++)
            {
                aSubset[sq, index] = 0;
                occ = index << 1;
                if ((sq & 7) != FILEa && (sq >> 3) != RANK8)
                {
                    for (ts = sq + 7; ; ts += 7)
                    {
                        aSubset[sq, index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILEa || (ts >> 3) == RANK8) break;
			        }
		        }
		        if ((sq & 7) != FILEh && (sq >> 3) != RANK1)
                {
                    for (ts = sq - 7; ; ts -= 7)
                    {
                        aSubset[sq, index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILEh || (ts >> 3) == RANK1) break;
                    }
		        }
	        }

	        // horizontal lines
	        for (ts = sq + 1, dx = x + 1; dx < FILEh; hMask[sq] |= 1UL << ts, ts += 1, dx++) ;
            for (ts = sq - 1, dx = x - 1; dx > FILEa; hMask[sq] |= 1UL << ts, ts -= 1, dx--) ;

            // vertical indexes
            for (index = 0; index < 64; index++)
            {
                vSubset[sq, index] = 0;
                ulong blockers = 0;
                for (int i = 0; i <= 5; i++)
                {
                    if ((index & (1UL << i)) > 0) 
                    {
                        blockers |= (1UL << (((5 - i) << 3) + 8));
                    }
		        }
		        if ((sq >> 3) != RANK8)
                {
                    for (ts = sq + 8; ; ts += 8)
                    {
                        vSubset[sq, index] |= (1UL << ts);
                        if ((blockers & (1UL << (ts - (ts & 7)))) > 0)
                            break;
                        if ((ts >> 3) == RANK8) break;
                    }
		        }
		        if ((sq >> 3) != RANK1)
                {
                    for (ts = sq - 8; ; ts -= 8)
                    {
                        vSubset[sq, index] |= (1UL << ts);
                        if ((blockers & (1UL << (ts - (ts & 7)))) > 0) break;
                        if ((ts >> 3) == RANK1) break;
                    }
		        }
	        }

	        // horizontal indexes
        	for (index = 0; index < 64; index++)
            {
                hSubset[sq, index] = 0;
                occ = index << 1;
                if ((sq & 7) != FILEh)
                {
                    for (ts = sq + 1; ; ts += 1)
                    {
                        hSubset[sq, index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILEh) break;
			        }
		        }
		        if ((sq & 7) != FILEa)
                {
                    for (ts = sq - 1; ; ts -= 1)
                    {
                        hSubset[sq, index] |= (1UL << ts);
                        if ((occ & (1UL << (ts & 7))) > 0) break;
                        if ((ts & 7) == FILEa) break;
                    }
		        }
	        }
        }

        const ulong file_b2_b7 = 0x0002020202020200;
        const ulong file_a2_a7 = 0x0001010101010100;
        const ulong diag_c2h7 = 0x0080402010080400;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            return dSubset[square, ((occupation & dMask[square]) * file_b2_b7) >> 58] |
                   aSubset[square, ((occupation & aMask[square]) * file_b2_b7) >> 58];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            return hSubset[square, (occupation >> shift_horizontal_table[square]) & 63] |
                   vSubset[square, (((occupation >> (square & 7)) & file_a2_a7) * diag_c2h7) >> 58];
        }
    }
}
