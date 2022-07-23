using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    //TODO: consider removing the color enum and using two dedicated functions instead
    public class Features
    {
        public const ulong WhiteSquares = 0x55AA55AA55AA55AAUL;
        public const ulong BlackSquares = 0xAA55AA55AA55AA55UL;

        public static readonly ulong[] NeighbourFiles =
        {
            0x0202020202020202UL, 0x0505050505050505UL,
            0x0A0A0A0A0A0A0A0AUL, 0x1414141414141414UL,
            0x2828282828282828UL, 0x5050505050505050UL,
            0xA0A0A0A0A0A0A0A0UL, 0x4040404040404040UL
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIsolatedPawn(ulong pawns, int square)
        {
            int file = square & 7;
            return (pawns & NeighbourFiles[file]) == 0;
        }

        private static ulong GetIsolatedPawns(ulong pawns)
        {
            ulong result = 0;
            for (ulong bits = pawns; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                if (IsIsolatedPawn(pawns, square))
                    result |= 1UL << square;
            }
            return result;
        }

        public static ulong GetIsolatedBlackPawns(BoardState board)
        {
            return GetIsolatedPawns(board.Black & board.Pawns);
        }

        public static ulong GetIsolatedWhitePawns(BoardState board)
        {
            return GetIsolatedPawns(board.White & board.Pawns);
        }

        public static ulong GetPassedBlackPawns(BoardState board)
        {
            ulong whiteFront = Up(FillUp(board.White & board.Pawns));
            return board.Black & board.Pawns & ~(whiteFront | Left(whiteFront) | Right(whiteFront));
        }

        public static ulong GetPassedWhitePawns(BoardState board)
        {
            ulong blackFront = Down(FillDown(board.Black & board.Pawns));
            return board.White & board.Pawns & ~(blackFront | Left(blackFront) | Right(blackFront));
        }

        public static ulong GetProtectedBlackPawns(BoardState board)
        {
            ulong blackPawns = board.Black & board.Pawns;
            return (LeftDown(blackPawns) | RightDown(blackPawns)) & blackPawns;
        }

        public static ulong GetProtectedWhitePawns(BoardState board)
        {
            ulong whitePawns = board.White & board.Pawns;
            return (LeftUp(whitePawns) | RightUp(whitePawns)) & whitePawns;
        }

        public static ulong GetConnectedBlackPawns(BoardState board)
        {
            ulong blackPawns = board.Black & board.Pawns;
            return (Left(blackPawns) | Right(blackPawns)) & blackPawns;
        }

        public static ulong GetConnectedWhitePawns(BoardState board)
        {
            ulong whitePawns = board.White & board.Pawns;
            return (Left(whitePawns) | Right(whitePawns)) & whitePawns;
        }

        public static ulong GetBackwardBlackPawns(BoardState board)
        {
            ulong blackPawns = board.Black & board.Pawns;
            ulong whitePawns = board.White & board.Pawns;
            ulong blackAttacks = LeftDown(blackPawns) | RightDown(blackPawns);
            ulong whiteAttacks = LeftUp(whitePawns) | RightUp(whitePawns);
            //black pawns behind all friendly adjacent pawns whose down-square is attacked by white pawns are backward
            return Up(Down(blackPawns) & whiteAttacks & ~FillDown(blackAttacks));
        }

        public static ulong GetBackwardWhitePawns(BoardState board)
        {
            ulong blackPawns = board.Black & board.Pawns;
            ulong whitePawns = board.White & board.Pawns;
            ulong blackAttacks = LeftDown(blackPawns) | RightDown(blackPawns);
            ulong whiteAttacks = LeftUp(whitePawns) | RightUp(whitePawns);
            //white pawns behind all friendly adjacent pawns whose up-square is attacked by black pawns are backward
            return Down(Up(whitePawns) & blackAttacks & ~FillUp(whiteAttacks));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong LeftDown(ulong bits) => (bits & 0xFEFEFEFEFEFEFEFEUL) >> 9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RightDown(ulong bits) => (bits & 0x7F7F7F7F7F7F7F7FUL) >> 7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong LeftUp(ulong bits) => (bits & 0xFEFEFEFEFEFEFEFEUL) << 7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RightUp(ulong bits) => (bits & 0x7F7F7F7F7F7F7F7FUL) << 9;
    }
}
