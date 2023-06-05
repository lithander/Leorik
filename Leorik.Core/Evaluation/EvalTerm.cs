using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    public struct EvalTermFloat
    {
        public float Base;
        public float Endgame;
    }

    public struct EvalTerm
    {
        public short Base;
        public short Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Eval(float phase) => Base + Endgame * phase;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(ref (short mg, short eg) tuple)
        {
            Base -= tuple.mg;// Weights.MidgameTables[tableIndex];
            Endgame -= tuple.eg;// Weights.EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ref (short mg, short eg) tuple)
        {
            Base += tuple.mg;// Weights.MidgameTables[tableIndex];
            Endgame += tuple.eg;// Weights.EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(ref (short mg, short eg) tuple, int count)
        {
            Base -= (short)(count * tuple.mg);
            Endgame -= (short)(count * tuple.eg);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ref (short mg, short eg) tuple, int count)
        {
            Base += (short)(count * tuple.mg);
            Endgame += (short)(count * tuple.eg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Change(int index, int count)
        {
            //(short mg, short eg) = Weights.Features[index];
            //Base += (short)(count * mg);
            //Endgame += (short)(count * eg);
        }
    }
}
