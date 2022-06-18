using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    public struct Mobility
    {
        //MSE=0,236356898762
        //static short[] Pawn = new short[13] { -10, 0, -1, 0, 62, 0, 0, 0, 0, 0, 0, 0, 0 };
        //static short[] Bishop = new short[14] { -30, -19, -10, -5, 2, 6, 10, 12, 15, 14, 14, 13, 7, 13 };
        //static short[] Rook = new short[15] { -41, -33, -26, -19, -15, -8, -3, 2, 5, 9, 12, 14, 20, 18, 18 };
        //static short[] Queen = new short[28] { -26, -30, -26, -27, -23, -20, -20, -19, -16, -12, -11, -9, -2, 0, 3, 9, 11, 17, 22, 27, 29, 34, 39, 39, 36, 38, 9, 17 };
        //static short[] King = new short[9] { 14, 7, 5, 7, 5, -1, -4, -11, -23 };

        //Tuned on the BIG Dataset
        //Leorik's MSE('data/quiet-labeled.epd') with MSE_SCALING = 100: 0,23698339051219622
        static short[] Pawn = new short[13] { -10, 0, -1, 0, 62, 0, 0, 0, 0, 0, 0, 0, 0 };
        static short[] Bishop = new short[14] { -27, -20, -12, -8, -2, 2, 5, 6, 8, 9, 12, 5, 12, -8, };
        static short[] Rook = new short[15] { -41, -31, -29, -24, -23, -16, -12, -8, -5, -1, -1, 3, 11, 5, 4, };
        static short[] Queen = new short[28] { -25, -32, -29, -28, -25, -21, -19, -18, -16, -11, -8, -9, -7, -4, -3, 0, 0, 6, 7, 7, 12, 18, 12, 14, 10, 17, 4, 8, };
        static short[] King = new short[9] { 20, 12, 7, 5, 2, -4, -2, -12, -27, };

        public short Base;
        public short Endgame;

        public Mobility(BoardState board)
        {
            Base = 0;
            Endgame = 0;

            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            int count = Bitboard.PopCount(Bitboard.KingTargets[square] & ~occupied);
            Base -= King[count];

            square = Bitboard.LSB(board.Kings & board.White);
            count = Bitboard.PopCount(Bitboard.KingTargets[square] & ~occupied);
            Base += King[count];

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                count = Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & ~occupied);
                Base -= Bishop[count];
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                count = Bitboard.PopCount(Bitboard.GetBishopTargets(occupied, square) & ~occupied);
                Base += Bishop[count];
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                count = Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & ~occupied);
                Base -= Rook[count];
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                count = Bitboard.PopCount(Bitboard.GetRookTargets(occupied, square) & ~occupied);
                Base += Rook[count];
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                count = Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & ~occupied);
                Base -= Queen[count];
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                count = Bitboard.PopCount(Bitboard.GetQueenTargets(occupied, square) & ~occupied);
                Base += Queen[count];
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = Bitboard.PopCount(blackPawns) - Bitboard.PopCount(oneStep);
            //promotion square not blocked?
            int promo = Bitboard.PopCount(oneStep & 0x00000000000000FFUL);
            Base -= (short)(promo * Pawn[4] + Pawn[0] * blocked);

            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = Bitboard.PopCount(whitePawns) - Bitboard.PopCount(oneStep);
            //promotion square not blocked?
            promo = Bitboard.PopCount(oneStep & 0xFF00000000000000UL);
            Base += (short)(promo * Pawn[4] + Pawn[0] * blocked);
        }
    }
}
