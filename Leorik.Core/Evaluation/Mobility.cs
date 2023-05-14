using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;


namespace Leorik.Core
{
    public static class Mobility
    {
        const int Pawn = 0;
        const int Knight = 13;
        const int Bishop = 22;
        const int Rook = 36;
        const int Queen = 51;
        const int King = 79;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Update(BoardState board, ref EvalTerm eval)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = LSB(board.Kings & board.Black);
            int moves = PopCount(KingTargets[square] & ~occupied);
            eval.SubtractMobility(King + moves);

            square = LSB(board.Kings & board.White);
            moves = PopCount(KingTargets[square] & ~occupied);
            eval.AddMobility(King + moves);

            //Knights
            //for (ulong knights = board.Knights & board.Black; knights != 0; knights = ClearLSB(knights))
            //{
            //    square = LSB(knights);
            //    moves = PopCount(KnightTargets[square] & ~occupied);
            //    Sub(ref eval, Knight + moves);
            //}
            //for (ulong knights = board.Knights & board.White; knights != 0; knights = ClearLSB(knights))
            //{
            //    square = LSB(knights);
            //    moves = PopCount(KnightTargets[square] & ~occupied);
            //    Add(ref eval, Knight + moves);
            //}

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square) & ~occupied);
                eval.SubtractMobility(Bishop + moves);
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square) & ~occupied);
                eval.AddMobility(Bishop + moves);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                eval.SubtractMobility(Rook + moves);
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                eval.AddMobility(Rook + moves);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                eval.SubtractMobility(Queen + moves);
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                eval.AddMobility(Queen + moves);
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = PopCount(blackPawns) - PopCount(oneStep);
            eval.SubtractMobility(Pawn, blocked);
            //promotion square not blocked?
            int promo = PopCount(oneStep & 0x00000000000000FFUL);
            eval.SubtractMobility(Pawn + 3, promo);
            
            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = PopCount(whitePawns) - PopCount(oneStep);
            eval.AddMobility(Pawn, blocked);
            //promotion square not blocked?
            promo = PopCount(oneStep & 0xFF00000000000000UL);
            eval.AddMobility(Pawn + 3, promo);
        }
    }
}
