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

        internal static ulong GetKingZone(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                int square = Bitboard.LSB(board.Kings & board.Black);
                return (board.Kings & board.Black) | Bitboard.KingTargets[square];
            }
            else //White
            {
                int square = Bitboard.LSB(board.White & board.Kings);
                return (board.White & board.Kings) | Bitboard.KingTargets[square];
            }
        }

        public static int CountWhiteKingThreats(BoardState board)
        {
            ulong kingZone = GetKingZone(board, Color.White);
            ulong occupied = 0;// board.Black | board.White;
            int square;
            int sum = 0;
            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                sum += Bitboard.PopCount(Bitboard.KnightTargets[square] & kingZone);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                sum += Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & kingZone);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                sum += Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & kingZone);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                sum += Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & kingZone);
            }
            return sum;
        }

        public static int CountBlackKingThreats(BoardState board)
        {
            ulong kingZone = GetKingZone(board, Color.Black);
            ulong occupied = 0;// board.Black | board.White;
            int square;
            int sum = 0;
            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                sum += Bitboard.PopCount(Bitboard.KnightTargets[square] & kingZone);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                sum += Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & kingZone);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                sum += Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & kingZone);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                sum += Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & kingZone);
            }
            return sum;
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
