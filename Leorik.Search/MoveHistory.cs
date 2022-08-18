using Leorik.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Search
{
    public class MoveHistory
    {
        private const int MoveHashes = 256; //Power-of-Two required!
        private const int Squares = 64;
        private const int Pieces = 12;
        private readonly int[] Positive = new int[Squares * Pieces * MoveHashes];

        public void Scale()
        {
            for (int i = 0; i < Squares * Pieces * MoveHashes; i++)
                Positive[i] >>= 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Index(ulong moveHash, ref Move move)
        {
            int a = (int)(moveHash & (MoveHashes - 1));
            int b = ((byte)move.MovingPiece() >> 1) - 2; //BlackPawn = 0...
            int c = move.ToSquare;
            int index = a * (Squares * Pieces) + b * Squares + c;
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(ulong moveHash, int depth, ref Move move)
        {
            //Console.WriteLine($"GOOD! Prev: {moveHash}:{moveHash & (MoveHashes - 1)} => {move.MovingPiece()}-{move} @ {depth}");
            Positive[Index(moveHash, ref move)] += depth * depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Value(ulong moveHash, ref Move move)
        {
            int value = Positive[Index(moveHash, ref move)];
            //if(value > 0)
            //    Console.WriteLine($"HIT! Prev: {moveHash}:{moveHash & (MoveHashes - 1)} => {move.MovingPiece()}-{move} = {value}");
            return value;
        }
    }
}
