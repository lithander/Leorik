using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public static class BishopPair
    {
        const int BALANCE = 5;
        const int BISHOP_PAIR = 25;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(BoardState board, ref EvalTerm eval)
        {
            AddBlack(board, ref eval);
            AddWhite(board, ref eval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddWhite(BoardState board, ref EvalTerm eval)
        {
            ulong blackSquaredBishop = board.White & board.Bishops & Features.BlackSquares;
            ulong whiteSquaredBishop = board.White & board.Bishops & Features.WhiteSquares;

            if (blackSquaredBishop > 0 && whiteSquaredBishop > 0)
            {
                eval.Base += BISHOP_PAIR;
                return;
            }

            ulong pieces = board.Black & ~board.Pawns & ~board.Knights;
            int blackVsWhite = PopCount(Features.BlackSquares & pieces) - PopCount(Features.WhiteSquares & pieces);

            if (blackSquaredBishop > 0)
                eval.Base += (short)(BALANCE * blackVsWhite);
            else if (whiteSquaredBishop > 0)
                eval.Base -= (short)(BALANCE * blackVsWhite);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddBlack(BoardState board, ref EvalTerm eval)
        {
            ulong blackSquaredBishop = board.Black & board.Bishops & Features.BlackSquares;
            ulong whiteSquaredBishop = board.Black & board.Bishops & Features.WhiteSquares;

            if (blackSquaredBishop > 0 && whiteSquaredBishop > 0)
            {
                eval.Base -= BISHOP_PAIR;
                return;
            }

            ulong pieces = board.White & ~board.Pawns & ~board.Knights;
            int blackVsWhite = PopCount(Features.BlackSquares & pieces) - PopCount(Features.WhiteSquares & pieces);
            if (blackSquaredBishop > 0)
                eval.Base -= (short)(BALANCE * blackVsWhite);
            else if (whiteSquaredBishop > 0)
                eval.Base += (short)(BALANCE * blackVsWhite);
        }
    }
}
