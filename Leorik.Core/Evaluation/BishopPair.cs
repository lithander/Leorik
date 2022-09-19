using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public class BishopPair
    {
        const int PAIR_BONUS = 25;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Eval(BoardState board)
        {
            int blackBishopPair = Bitboard.PopCount(board.Black & board.Bishops) >> 1;
            int whiteBishopPair = Bitboard.PopCount(board.White & board.Bishops) >> 1;
            int bonus = PAIR_BONUS * (whiteBishopPair - blackBishopPair);
            return (short)bonus;
        }
    }
}
