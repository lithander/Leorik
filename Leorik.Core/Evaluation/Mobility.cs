using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;


namespace Leorik.Core
{
    public static class Mobility
    {
        const int PAWN_STUCK = -9;
        const int PAWN_PROMOTION = 74;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Bishop(int moves) => Math.Min(7, moves) * 5;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Rook(int moves) => Math.Min(11, moves) * 5;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static int Queen(int moves) => Math.Min(25, moves) * 3;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static int King(int moves) => moves * -5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Update(BoardState board, ref EvalTerm eval)
        {
            eval.Base += Eval(board);
        }

        public static short Eval(BoardState board)
        {
            int result = 0;

            ulong occupied = board.Black | board.White;

            //Kings
            int square = LSB(board.Kings & board.Black);
            int moves = PopCount(KingTargets[square] & ~occupied);
            result -= King(moves);

            square = LSB(board.Kings & board.White);
            moves = PopCount(KingTargets[square] & ~occupied);
            result += King(moves);

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square) & ~occupied);
                result -= Bishop(moves);
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square) & ~occupied);
                result += Bishop(moves);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                result -= Rook(moves);
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                result += Rook(moves);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                result -= Queen(moves);
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                result += Queen(moves);
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = PopCount(blackPawns) - PopCount(oneStep);
            //promotion square not blocked?
            int promo = PopCount(oneStep & 0x00000000000000FFUL);
            result -= promo * PAWN_PROMOTION + PAWN_STUCK * blocked;

            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = PopCount(whitePawns) - PopCount(oneStep);
            //promotion square not blocked?
            promo = PopCount(oneStep & 0xFF00000000000000UL);
            result += promo * PAWN_PROMOTION + PAWN_STUCK * blocked;

            return (short)result;
        }
    }
}
