using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    static class BishopPair
    {
        const int BISHOP_PAIR = 25;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Update(BoardState board, ref Material eval)
        {
            //A weakspot is that this awards the bonus for two bishops on same-colored squares (but these are rare enough to ignore)
            if (Bitboard.PopCount(board.Black & board.Bishops) > 1)
                eval.Base -= BISHOP_PAIR;

            if (Bitboard.PopCount(board.White & board.Bishops) > 1)
                eval.Base += BISHOP_PAIR;
        }

        internal static void UpdateIncremental(BoardState board, ref Move move, ref Material eval)
        {
            if (move.Flags == (Piece.Black | Piece.BishopPromotion))
                if (Bitboard.PopCount(board.Black & board.Bishops) == 2)
                    eval.Base -= BISHOP_PAIR;

            if (move.Flags == (Piece.White | Piece.BishopPromotion))
                if(Bitboard.PopCount(board.White & board.Bishops) == 2)
                    eval.Base += BISHOP_PAIR;

            //if there's only one BLACK bishop left lose the Bishop-Pair bonus
            if (move.CapturedPiece() == Piece.BlackBishop && Bitboard.PopCount(board.Black & board.Bishops) == 1)
                eval.Base += BISHOP_PAIR;

            //if there's only one WHITE bishop left lose the Bishop-Pair bonus
            if (move.CapturedPiece() == Piece.WhiteBishop && Bitboard.PopCount(board.White & board.Bishops) == 1)
                eval.Base -= BISHOP_PAIR;
        }
    }
}
