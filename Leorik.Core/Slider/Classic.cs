using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

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
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;
            //diagonal line through square
            ulong bbDiagonal = Diagonal[square];
            //antidiagonal line through square
            ulong bbAntiDiagonal = AntiDiagonal[square];

            return (Subset(bbDiagonal, bbBlocker & bbDiagonal, bbBelow)
                  | Subset(bbAntiDiagonal, bbBlocker & bbAntiDiagonal, bbBelow)) & ~bbPiece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;
            //horizontal line through square
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            //vertical line through square
            ulong bbVertical = VERTICAL << (square & 7);

            return (Subset(bbHorizontal, bbBlocker & bbHorizontal, bbBelow)
                  | Subset(bbVertical, bbBlocker & bbVertical, bbBelow)) & ~bbPiece;
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
        internal static ulong DiagonalSubset(ulong occupation, int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            ulong bbDiagonal = Diagonal[square];
            return Subset(bbDiagonal, bbBlocker & bbDiagonal, bbPiece - 1) & ~bbPiece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong AntiDiagonalSubset(ulong occupation, int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            ulong bbAntiDiagonal = AntiDiagonal[square];
            return Subset(bbAntiDiagonal, bbBlocker & bbAntiDiagonal, bbPiece - 1) & ~bbPiece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong HorizontalSubset(ulong occupation, int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            return Subset(bbHorizontal, bbBlocker & bbHorizontal, bbPiece - 1) & ~bbPiece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong VerticalSubset(ulong occupation, int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            ulong bbVertical = VERTICAL << (square & 7);
            return Subset(bbVertical, bbBlocker & bbVertical, bbPiece - 1) & ~bbPiece;
        }
    }
}
