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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EvalTerm Sub(ref EvalTerm eval, int index)
        {
            eval.Base -= Weights.MidgameMobility[index];
            eval.Endgame -= Weights.EndgameMobility[index];
            return eval;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Add(ref EvalTerm eval, int index)
        {
            eval.Base += Weights.MidgameMobility[index];
            eval.Endgame += Weights.EndgameMobility[index];
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Update(BoardState board, ref EvalTerm eval)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = LSB(board.Kings & board.Black);
            int moves = PopCount(KingTargets[square] & ~occupied);
            Sub(ref eval, King + moves);

            square = LSB(board.Kings & board.White);
            moves = PopCount(KingTargets[square] & ~occupied);
            Add(ref eval, King + moves);

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
                Sub(ref eval, Bishop + moves);
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square) & ~occupied);
                Add(ref eval, Bishop + moves);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                Sub(ref eval, Rook + moves);
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square) & ~occupied);
                Add(ref eval, Rook + moves);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                Sub(ref eval, Queen + moves);
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square) & ~occupied);
                Add(ref eval, Queen + moves);
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = PopCount(blackPawns) - PopCount(oneStep);
            eval.Base -= (short)(blocked * Weights.MidgameMobility[Pawn + 0]);
            eval.Endgame -= (short)(blocked * Weights.EndgameMobility[Pawn + 0]);
            //promotion square not blocked?
            int promo = PopCount(oneStep & 0x00000000000000FFUL);
            eval.Base -= (short)(promo * Weights.MidgameMobility[Pawn + 4]);
            eval.Endgame -= (short)(promo * Weights.EndgameMobility[Pawn + 4]);
            
            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = PopCount(whitePawns) - PopCount(oneStep);
            eval.Base += (short)(blocked * Weights.MidgameMobility[Pawn + 0]);
            eval.Endgame += (short)(blocked * Weights.EndgameMobility[Pawn + 0]);
            //promotion square not blocked?
            promo = PopCount(oneStep & 0xFF00000000000000UL);
            eval.Base += (short)(promo * Weights.MidgameMobility[Pawn + 4]);
            eval.Endgame += (short)(promo * Weights.EndgameMobility[Pawn + 4]);
        }
    }
}
