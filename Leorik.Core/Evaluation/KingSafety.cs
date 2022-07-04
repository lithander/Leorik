using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public static class KingSafety
    {
        //KingSafety - MG
        //-56, -50, -48, -51, -47, -36, -27, 1, 8, 37, 81, 42, 51, 91, 4, 0, 0, 0, 0, 0,
        //KingSafety - EG
        //
        //
        //Phase
        //N:  77 B: 292 R: 368 Q:1026
        //MSE(cFeatures) with MSE_SCALING = 100 on the dataset: 0,23599683749768358

        static short[] KingThreatsBase = new short[20] { -56, -50, -48, -51, -47, -36, -27, 1, 8, 37, 81, 42, 51, 91, 4, 0, 0, 0, 0, 0, };
        static short[] KingThreatsEndgame = new short[20] { 45, 39, 23, 37, 34, 15, 14, -23, -29, -66, -94, -34, 22, 15, 0, 0, 0, 0, 0, 0, };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref EvalTerm eval)
        {
            //White
            int count = Math.Min(19, Features.CountBlackKingThreats(board));
            eval.Base += KingThreatsBase[count];
            eval.Endgame += KingThreatsEndgame[count];
            //Black
            count = Math.Min(19, Features.CountWhiteKingThreats(board));
            eval.Base -= KingThreatsBase[count];
            eval.Endgame -= KingThreatsEndgame[count];
        }
    }
}