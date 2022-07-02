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
    }
}
