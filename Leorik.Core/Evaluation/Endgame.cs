
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

        internal static short ScaleRef(BoardState board, float score)
        {
            if (Drawn.Contains(Notation.GetEndgameClass(board)))
                return (short)((int)score >> 3);
            else
                return (short)score;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short _Scale(BoardState board, float score)
        {
            short s0 = Scale(board, score);
            short s1 = ScaleRef(board, score);
            if (s0 != s1)
                throw new Exception();
            return s0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short Scale(BoardState board, float score)
        {
            int cnt = PopCount(board.Black | board.White);
            if (cnt > 5)
                return (short)score;

            ulong black = board.Black & ~board.Kings;
            ulong white = board.White & ~board.Kings;
            if(IsDrawn(board, black, white) || IsDrawn(board, white, black))
            {
                Debug.Assert(Drawn.Contains(Notation.GetEndgameClass(board)));
                return (short)((int)score >> 3);
            }

            Debug.Assert(!Drawn.Contains(Notation.GetEndgameClass(board)));
            return (short)score;
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
