using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Leorik.Core
{
    public struct EvalTerm
    {
        public short Base;
        public short Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract((short mg, short eg) tuple)
        {
            Base -= tuple.mg;
            Endgame -= tuple.eg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add((short mg, short eg) tuple)
        {
            Base += tuple.mg;
            Endgame += tuple.eg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract((short mg, short eg) tuple, int count)
        {
            Base -= (short)(count * tuple.mg);
            Endgame -= (short)(count * tuple.eg);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add((short mg, short eg) tuple, int count)
        {
            Base += (short)(count * tuple.mg);
            Endgame += (short)(count * tuple.eg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMaterial(int pieceIndex, int squareIndex, Vector256<float> vars)
        {
            int entryIndex = Weights.MaterialTerms * ((pieceIndex << 6) | squareIndex);
            Vector256<float> weights = Vector256.Create(Weights.MaterialWeights, entryIndex + 1);
            Base += (short)(Vector256.Dot(vars, weights) + Weights.MaterialWeights[entryIndex]);
            weights = Vector256.Create(Weights.MaterialWeights, entryIndex + 10);
            Endgame += (short)(Vector256.Dot(vars, weights) + Weights.MaterialWeights[entryIndex + 9]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractMaterial(int pieceIndex, int squareIndex, Vector256<float> vars)
        {
            int entryIndex = Weights.MaterialTerms * ((pieceIndex << 6) | squareIndex);
            Vector256<float> weights = Vector256.Create(Weights.MaterialWeights, entryIndex + 1);
            Base -= (short)(Vector256.Dot(vars, weights) + Weights.MaterialWeights[entryIndex]);
            weights = Vector256.Create(Weights.MaterialWeights, entryIndex + 10);
            Endgame -= (short)(Vector256.Dot(vars, weights) + Weights.MaterialWeights[entryIndex + 9]);
        }
    }
}
