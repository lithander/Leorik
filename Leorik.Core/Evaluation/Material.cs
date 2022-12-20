using System.Runtime.CompilerServices;

//MSE_SCALING = 100
//ITERATIONS = 100
//MATERIAL_ALPHA = 1000
//PHASE_ALPHA = 10
//MATERIAL_BATCH = 100
//PHASE_BATCH = 10
//
//Loading DATA from 'data/quiet-labeled.epd'
//N: 159 B: 304 R: 357 Q: 861
//MSE(cFeatures) with MSE_SCALING = 100 on the dataset: 0,2364836161887792

namespace Leorik.Core
{
    public struct Material
    {
        public short Base;
        public short Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Base += Weights.MidgameTables[tableIndex];
            Endgame += Weights.EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Base -= Weights.MidgameTables[tableIndex];
            Endgame -= Weights.EndgameTables[tableIndex];
        }
    }
}
