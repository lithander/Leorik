using Leorik.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class History : IComparer<Move>
    {
        private const int Squares = 64;
        private const int Pieces = 12;
        private readonly int[,] Positive = new int[Squares, Pieces];
        private readonly int[,] Negative = new int[Squares, Pieces];
        
        public void Scale()
        {
            for (int square = 0; square < Squares; square++)
                for(int piece = 0; piece < Pieces; piece++)
                {
                    Positive[square, piece] /= 2;
                    Negative[square, piece] /= 2;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece)
        {
            return ((byte)piece >> 1) - 2; //BlackPawn = 0...
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            Positive[move.ToSquare, iPiece] += depth * depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bad(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            Negative[move.ToSquare, iPiece] += depth * depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            float a = Positive[move.ToSquare, iPiece];
            float b = Negative[move.ToSquare, iPiece];
            return a / (a + b + 1);//ratio of good increments normalized to the range of [0..1]
        }

        public int Compare(Move x, Move y)
        {
            float a = Value(ref x);
            float b = Value(ref y);
            return a.CompareTo(b);
        }
    }
}
