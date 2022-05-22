﻿using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public class Features
    {
        public static readonly ulong[] NeighbourFiles =
        {
            0x0202020202020202UL, 0x0505050505050505UL,
            0x0A0A0A0A0A0A0A0AUL, 0x1414141414141414UL,
            0x2828282828282828UL, 0x5050505050505050UL,
            0xA0A0A0A0A0A0A0A0UL, 0x4040404040404040UL
        };

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

        public static ulong GetDoubledPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackRear = Up(FillUp(board.Black & board.Pawns));
                return board.Black & board.Pawns & blackRear;
            }
            else //White
            {
                ulong whiteRear = Down(FillDown(board.White & board.Pawns));
                return board.White & board.Pawns & whiteRear;
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
                return (Left(blackPawns) ^ Right(blackPawns)) & blackPawns;
            }
            else //White
            {
                ulong whitePawns = board.White & board.Pawns;
                return (Left(whitePawns) ^ Right(whitePawns)) & whitePawns;
            }
        }

        const ulong QUEEN_SIDE = 0x0000000000000007UL;
        const ulong KING_SIDE = 0x00000000000000E0UL;

        const ulong BLACK_QUEEN_SIDE_SHIELD = 0x0007030100000000UL;
        const ulong BLACK_KING_SIDE_SHIELD = 0x00C0C0C000000000UL;
        //. . . . . . . .
        //B B B . . . B B
        //B B . . . . B B
        //B . . . . . B B
        //W . . . . . W W
        //W W . . . . W W
        //W W W . . . W W
        //. . . . . . . .
        const ulong WHITE_QUEEN_SIDE_SHIELD = 0x0000000001030700UL;
        const ulong WHITE_KING_SIDE_SHIELD = 0x00000000C0C0C000UL;

        public static ulong GetPawnShields(BoardState board)
        {
            ulong result = 0;
            ulong blackKing = board.Black & board.Kings;
            ulong blackPawns = board.Black & board.Pawns;
            if ((blackKing & (KING_SIDE << 56)) > 0)
                result |= blackPawns & BLACK_KING_SIDE_SHIELD;
            if ((blackKing & (QUEEN_SIDE << 56)) > 0)
                result |= blackPawns & BLACK_QUEEN_SIDE_SHIELD;

            ulong whiteKing = board.White & board.Kings;
            ulong whitePawns = board.White & board.Pawns;
            if ((whiteKing & KING_SIDE) > 0)
                result |= whitePawns & WHITE_KING_SIDE_SHIELD;
            if ((whiteKing & QUEEN_SIDE) > 0)
                result |= whitePawns & WHITE_QUEEN_SIDE_SHIELD;

            return result;
        }

        private static ulong FillUp(ulong bits)
        {
            bits |= (bits << 8);
            bits |= (bits << 16);
            bits |= (bits << 32);
            return bits;
        }

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