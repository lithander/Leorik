using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;


namespace Leorik.Core
{
    public static class MobilityEval
    {
        const int Pawn = 0;
        //const int Knight = 13;
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
            eval.Subtract(Weights.Mobility[King + moves]);

            square = LSB(board.Kings & board.White);
            moves = PopCount(KingTargets[square] & ~occupied);
            eval.Add(Weights.Mobility[King + moves]);

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
                moves = PopCount(GetBishopTargets(occupied, square));
                eval.Subtract(Weights.Mobility[Bishop + moves]);
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square));
                eval.Add(Weights.Mobility[Bishop + moves]);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square));
                eval.Subtract(Weights.Mobility[Rook + moves]);
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square));
                eval.Add(Weights.Mobility[Rook + moves]);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square));
                eval.Subtract(Weights.Mobility[Queen + moves]);
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square));
                eval.Add(Weights.Mobility[Queen + moves]);
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = PopCount(blackPawns) - PopCount(oneStep);
            eval.Subtract(Weights.Mobility[Pawn], blocked);
            //promotion square not blocked?
            int promo = PopCount(oneStep & 0x00000000000000FFUL);
            eval.Subtract(Weights.Mobility[Pawn + 3], promo);

            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = PopCount(whitePawns) - PopCount(oneStep);
            eval.Add(Weights.Mobility[Pawn], blocked);
            //promotion square not blocked?
            promo = PopCount(oneStep & 0xFF00000000000000UL);
            eval.Add(Weights.Mobility[Pawn + 3], promo);
        }
    }
}
