using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public static class KingSafety
    {
        //- - - - - - - -
        //- - - - - - - -
        //- - - - - - - -
        //- - - O O O - -
        //- - - - K - - -
        //- - - - - - - -
        //- - - - - - - -
        //- - - - - - - -
        //
        //KingSafety - MG
        //-19, -1, 8, 12, 0, 0, 0, 0, 0, 0,
        //KingSafety - EG
        //, 0, 0, 0, 0, 0, 0,

        //MSE(cFeatures) with MSE_SCALING = 100 on the dataset: 0,23613431332171486 (Ref: 0,23643727955169666)

        static short[] PawnShieldBase = new short[4] { -19, -1, 8, 12 };
        static short[] PawnShieldEndgame = new short[4] { 40, 15, -7, -48 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref EvalTerm eval)
        {
            //int count = PopCount(Features.GetPawnShield(board, Color.White));
            //eval.Base += PawnShieldBase[count];
            //eval.Endgame += PawnShieldEndgame[count];
            //
            //count = PopCount(Features.GetPawnShield(board, Color.Black));
            //eval.Base -= PawnShieldBase[count];
            //eval.Endgame -= PawnShieldEndgame[count];
        }
    }
}