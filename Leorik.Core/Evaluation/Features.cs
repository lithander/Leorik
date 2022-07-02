using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    //TODO: consider removing the color enum and using two dedicated functions instead
    public class Features
    {
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

        public static ulong GetIsolatedPawns(BoardState board, Color color)
        {
            ulong mask = color == Color.Black ? board.Black : board.White;
            ulong pawns = mask & board.Pawns;
            ulong result = 0;
            for (ulong bits = pawns; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                if (IsIsolatedPawn(pawns, square))
                    result |= 1UL << square;
            }
            return result;
        }

        public static ulong GetPassedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong whiteFront = Up(FillUp(board.White & board.Pawns));
                return board.Black & board.Pawns & ~(whiteFront | Left(whiteFront) | Right(whiteFront));
            }
            else //White
            {
                ulong blackFront = Down(FillDown(board.Black & board.Pawns));
                return board.White & board.Pawns & ~(blackFront | Left(blackFront) | Right(blackFront));
            }
        }

        public static ulong GetProtectedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackPawns = board.Black & board.Pawns;
                //capture left
                ulong captureLeft = (blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9;
                ulong captureRight = (blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7;
                return (captureLeft | captureRight) & blackPawns;
            }
            else //White
            {
                ulong whitePawns = board.White & board.Pawns;
                ulong captureRight = (whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9;
                ulong captureLeft = (whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7;
                return (captureRight | captureLeft) & whitePawns;
            }
        }

        public static ulong GetConnectedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackPawns = board.Black & board.Pawns;
                return (Left(blackPawns) | Right(blackPawns)) & blackPawns;
            }
            else //White
            {
                ulong whitePawns = board.White & board.Pawns;
                return (Left(whitePawns) | Right(whitePawns)) & whitePawns;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}
