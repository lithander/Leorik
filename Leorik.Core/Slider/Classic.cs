using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core.Slider
{
    public static class Classic
    {
        private static readonly ulong[] Diagonal =
        {
            0x8040201008040201, 0x0080402010080402, 0x0000804020100804, 0x0000008040201008,
            0x0000000080402010, 0x0000000000804020, 0x0000000000008040, 0x0000000000000080,
            0x4020100804020100, 0x8040201008040201, 0x0080402010080402, 0x0000804020100804,
            0x0000008040201008, 0x0000000080402010, 0x0000000000804020, 0x0000000000008040,
            0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x0080402010080402,
            0x0000804020100804, 0x0000008040201008, 0x0000000080402010, 0x0000000000804020,
            0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201,
            0x0080402010080402, 0x0000804020100804, 0x0000008040201008, 0x0000000080402010,
            0x0804020100000000, 0x1008040201000000, 0x2010080402010000, 0x4020100804020100,
            0x8040201008040201, 0x0080402010080402, 0x0000804020100804, 0x0000008040201008,
            0x0402010000000000, 0x0804020100000000, 0x1008040201000000, 0x2010080402010000,
            0x4020100804020100, 0x8040201008040201, 0x0080402010080402, 0x0000804020100804,
            0x0201000000000000, 0x0402010000000000, 0x0804020100000000, 0x1008040201000000,
            0x2010080402010000, 0x4020100804020100, 0x8040201008040201, 0x0080402010080402,
            0x0100000000000000, 0x0201000000000000, 0x0402010000000000, 0x0804020100000000,
            0x1008040201000000, 0x2010080402010000, 0x4020100804020100, 0x8040201008040201
        };

        private static readonly ulong[] AntiDiagonal =
        {
            0x0000000000000001, 0x0000000000000102, 0x0000000000010204, 0x0000000001020408,
            0x0000000102040810, 0x0000010204081020, 0x0001020408102040, 0x0102040810204080,
            0x0000000000000102, 0x0000000000010204, 0x0000000001020408, 0x0000000102040810,
            0x0000010204081020, 0x0001020408102040, 0x0102040810204080, 0x0204081020408000,
            0x0000000000010204, 0x0000000001020408, 0x0000000102040810, 0x0000010204081020,
            0x0001020408102040, 0x0102040810204080, 0x0204081020408000, 0x0408102040800000,
            0x0000000001020408, 0x0000000102040810, 0x0000010204081020, 0x0001020408102040,
            0x0102040810204080, 0x0204081020408000, 0x0408102040800000, 0x0810204080000000,
            0x0000000102040810, 0x0000010204081020, 0x0001020408102040, 0x0102040810204080,
            0x0204081020408000, 0x0408102040800000, 0x0810204080000000, 0x1020408000000000,
            0x0000010204081020, 0x0001020408102040, 0x0102040810204080, 0x0204081020408000,
            0x0408102040800000, 0x0810204080000000, 0x1020408000000000, 0x2040800000000000,
            0x0001020408102040, 0x0102040810204080, 0x0204081020408000, 0x0408102040800000,
            0x0810204080000000, 0x1020408000000000, 0x2040800000000000, 0x4080000000000000,
            0x0102040810204080, 0x0204081020408000, 0x0408102040800000, 0x0810204080000000,
            0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000
        };

        const ulong HORIZONTAL = 0x00000000000000FF;
        const ulong VERTICAL = 0x0101010101010101;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            //diagonal line through square
            ulong bbDiagonal = Diagonal[square];
            //antidiagonal line through square
            ulong bbAntiDiagonal = AntiDiagonal[square];

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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong DiagonalSubset(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            ulong bbDiagonal = Diagonal[square];
            return Subset(bbDiagonal, bbBlocker & bbDiagonal, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong AntiDiagonalSubset(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            ulong bbAntiDiagonal = AntiDiagonal[square];
            return Subset(bbAntiDiagonal, bbBlocker & bbAntiDiagonal, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong HorizontalSubset(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            return Subset(bbHorizontal, bbBlocker & bbHorizontal, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong VerticalSubset(ulong occupation, int square)
        {
            Deconstruct(occupation, square, out ulong bbBlocker, out ulong bbBelow);
            ulong bbVertical = VERTICAL << (square & 7);
            return Subset(bbVertical, bbBlocker & bbVertical, bbBelow);
        }
    }
}
