using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public static class LegalMoveBound
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasMoves(BoardState board, int threshold)
        {
            if (board.SideToMove == Color.White)
                return CountWhite(board, int.MaxValue) >= threshold;
            else
                return CountBlack(board, int.MaxValue) >= threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountMoves(BoardState board)
        {
            if (board.SideToMove == Color.White)
                return CountWhite(board, int.MaxValue);
            else
                return CountBlack(board, int.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountBlack(BoardState board, int max)
        {
            int result = 0;
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            //can't move on squares occupied by side to move
            result += Bitboard.PopCount(Bitboard.KingTargets[square] & ~board.Black);
            if (result > max) 
                return max;

            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                result += Bitboard.PopCount(Bitboard.KnightTargets[square] & ~board.Black);
                if (result > max)
                    return max;
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                result += Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & ~board.Black);
                if (result > max)
                    return max;
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                result += Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & ~board.Black);
                if (result > max)
                    return max;
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                result += Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & ~board.Black);
                if (result > max)
                    return max;
            }

            //Pawns & Castling
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //move one square down
            result += Bitboard.PopCount(oneStep & 0xFFFFFFFFFFFFFF00UL);
            //move to first rank and promote
            result += 4 * Bitboard.PopCount(oneStep & 0x00000000000000FFUL);
            //move two squares down
            ulong twoStep = (oneStep >> 8) & ~occupied;
            result += Bitboard.PopCount(twoStep & 0x000000FF00000000UL);

            if (result > max)
                return max;

            //capture left
            ulong captureLeft = ((blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9) & board.White;
            result += Bitboard.PopCount(captureLeft & 0xFFFFFFFFFFFFFF00UL);
            //capture left to first rank and promote
            result += 4 * Bitboard.PopCount(captureLeft & 0x00000000000000FFUL);

            //capture right
            ulong captureRight = ((blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7) & board.White;
            result += Bitboard.PopCount(captureRight & 0xFFFFFFFFFFFFFF00UL);
            //capture right to first rank and promote
            result += 4 * Bitboard.PopCount(captureRight & 0x00000000000000FFUL);

            //is en-passent possible?
            captureLeft = ((blackPawns & 0x00000000FE000000UL) >> 9) & board.EnPassant;
            if (captureLeft != 0)
                result++;

            captureRight = ((blackPawns & 0x000000007F000000UL) >> 7) & board.EnPassant;
            if (captureRight != 0)
                result++;

            //Castling
            if (board.CanBlackCastleLong() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(59) /*&& !board.IsAttackedByWhite(58)*/)
                result++;

            if (board.CanBlackCastleShort() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(61) /*&& !board.IsAttackedByWhite(62)*/)
                result++;

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountWhite(BoardState board, int max)
        {
            int result = 0;
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.White);
            //can't move on squares occupied by side to move
            result += Bitboard.PopCount(Bitboard.KingTargets[square] & ~board.White);
            if (result > max) 
                return max;

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                result += Bitboard.PopCount(Bitboard.KnightTargets[square] & ~board.White);
                if (result > max)
                    return max;
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                result += Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & ~board.White);
                if (result > max)
                    return max;
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                result += Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & ~board.White);
                if (result > max)
                    return max;
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                result += Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & ~board.White);
                if (result > max)
                    return max;
            }

            //Pawns                
            ulong whitePawns = board.Pawns & board.White;
            ulong oneStep = (whitePawns << 8) & ~occupied;
            //move one square up
            result += Bitboard.PopCount(oneStep & 0x00FFFFFFFFFFFFFFUL);
            //move to last rank and promote
            result += 4 * Bitboard.PopCount(oneStep & 0xFF00000000000000UL);
            //move two squares up
            ulong twoStep = (oneStep << 8) & ~occupied;
            result += Bitboard.PopCount(twoStep & 0x00000000FF000000UL);

            if (result > max)
                return max;

            //capture left
            ulong captureLeft = ((whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7) & board.Black;
            result += Bitboard.PopCount(captureLeft & 0x00FFFFFFFFFFFFFFUL);
            //capture left to last rank and promote
            result += 4 * Bitboard.PopCount(captureLeft & 0xFF00000000000000UL);

            //capture right
            ulong captureRight = ((whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9) & board.Black;
            result += Bitboard.PopCount(captureRight & 0x00FFFFFFFFFFFFFFUL);
            //capture right to last rank and promote
            result += 4 * Bitboard.PopCount(captureRight & 0xFF00000000000000UL);

            //is en-passent possible?
            captureLeft = ((whitePawns & 0x000000FE00000000UL) << 7) & board.EnPassant;
            if (captureLeft != 0)
                result++;

            captureRight = ((whitePawns & 0x000007F00000000UL) << 9) & board.EnPassant;
            if (captureRight != 0)
                result++;

            //Castling
            if (board.CanWhiteCastleLong() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(3) /*&& !board.IsAttackedByBlack(2)*/)
                result++;

            if (board.CanWhiteCastleShort() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(5) /*&& !board.IsAttackedByBlack(6)*/)
                result++;

            return result;
        }
    }
}
