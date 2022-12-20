using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    public struct EvalTerm
    {
        public short Base;
        public short Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Eval(float phase) => Base + Endgame * phase;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFeature(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Add(ref Weights.Features[tableIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractFeature(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Subtract(ref Weights.Features[tableIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractFeature(int index)
        {
            Subtract(ref Weights.Features[index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFeature(int index)
        {
            Add(ref Weights.Features[index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractMobility(int index)
        {
            Subtract(ref Weights.Mobility[index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMobility(int index)
        {
            Add(ref Weights.Mobility[index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Subtract(ref (short mg, short eg) tuple)
        {
            Base -= tuple.mg;// Weights.MidgameTables[tableIndex];
            Endgame -= tuple.eg;// Weights.EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(ref (short mg, short eg) tuple)
        {
            Base += tuple.mg;// Weights.MidgameTables[tableIndex];
            Endgame += tuple.eg;// Weights.EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractMobility(int index, int count)
        {
            (short mg, short eg) = Weights.Mobility[index];
            Base -= (short)(count * mg);
            Endgame -= (short)(count * eg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMobility(int index, int count)
        {
            (short mg, short eg) = Weights.Mobility[index];
            Base += (short)(count * mg);
            Endgame += (short)(count * eg);
        }
    }
}
