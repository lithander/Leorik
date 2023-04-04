using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core.Slider
{
    public static class ClassicZero
    {

        const ulong DIAGONAL = 0x8040201008040201UL;
        const ulong ANTIDIAGONAL = 0x0102040810204080UL;
        const ulong HORIZONTAL = 0x00000000000000FF;
        const ulong VERTICAL = 0x0101010101010101;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //sign of 'ranks' decides between left shift or right shift. Then convert signed ranks to a positiver number of bits to shift by. Each rank has 8 bits e.g. 1 << 3 == 8
        private static ulong VerticalShift(in ulong bb, in int ranks) => ranks > 0 ? bb >> (ranks << 3) : bb << -(ranks << 3);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            //compute rank and file of square
            int rank = square >> 3;
            int file = square & 7;
            //diagonal line through square
            ulong bbDiagonal = VerticalShift(DIAGONAL, file - rank);
            //antidiagonal line through square
            ulong bbAntiDiagonal = VerticalShift(ANTIDIAGONAL, 7 - file - rank);

            return Subset(bbDiagonal, bbBlocker & bbDiagonal, bbBelow)
                 | Subset(bbAntiDiagonal, bbBlocker & bbAntiDiagonal, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            //horizontal line through square
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            //vertical line through square
            ulong bbVertical = VERTICAL << (square & 7);

            return Subset(bbHorizontal, bbBlocker & bbHorizontal, bbBelow)
                 | Subset(bbVertical, bbBlocker & bbVertical, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Deconstruct(ulong occupation, int square, out ulong bbBlocker, out ulong bbBelow)
        {
            ulong bbPiece = 1UL << square;
            bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            bbBelow = bbPiece - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Subset(ulong bbLine, ulong bbBlocker, ulong bbBelow)
        {
            //MaskLow sets all low bits up to and including the lowest blocker above orgin, the rest are zeroed out.
            //MaskHigh sets all low bits up to and including the highest blocker below origin, the rest are zerored out.
            //The bits of the line that are different between the two masks are the valid targets (including the first blockers on each side)
            return (MaskLow(bbBlocker & ~bbBelow) ^ MaskHigh(bbBlocker & bbBelow)) & bbLine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //identify the highest set bit and shift a mask so the bits below are set and the rest are zeroed
        private static ulong MaskHigh(ulong bb) => 0x7FFFFFFFFFFFFFFFUL >> BitOperations.LeadingZeroCount(bb | 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //identify the lowest set bit and set all bits below while zeroing the rest
        private static ulong MaskLow(ulong bb) => bb ^ (bb - 1);
    }
}
