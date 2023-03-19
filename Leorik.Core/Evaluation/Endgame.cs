
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public static class Endgame
    {
        public static HashSet<string> Drawn = new()
        {
            "KNvK", "KvKN",
            "KBvK", "KvKB",
            "KNNvK", "KvKNN",

            "KNNvKN", "KNvKNN",
            "KNNvKB", "KBvKNN",
            
            "KRvKN", "KNvKR",
            "KRvKB", "KBvKR"
        };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDrawn(BoardState board)
        {
            if (PopCount(board.Black | board.White) > 5)
                return false;

            ulong black = board.Black & ~board.Kings;
            ulong white = board.White & ~board.Kings;
            return IsDrawn(board, black, white) || IsDrawn(board, white, black);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDrawn(BoardState board, ulong strong, ulong weak)
        {
            //(N|B|NN) vs 0
            ulong NOrB = board.Knights | board.Bishops;
            if (weak == 0) //weak == 0
            {
                if ((board.Knights & strong) == strong && PopCount(strong) == 2)
                    return true; //strong == NN
                if ((NOrB & strong) == strong && PopCount(strong) == 1)
                    return true; //strong == (N|B)
            }
            //(NN||R) vs (N|B)
            else if ((NOrB & weak) == weak && PopCount(weak) == 1) //weak == (N|B)
            {
                if ((board.Knights & strong) == strong && PopCount(strong) == 2)
                    return true; //strong == NN
                if ((board.Rooks & strong) == strong && PopCount(strong) == 1)
                    return true; //strong == R
            }
            return false;
        }
    }
}
