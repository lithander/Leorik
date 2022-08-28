using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class History
    {
        private const int Squares = 64;
        private const int Pieces = 12;

        private ulong TotalPositive = 0;
        private ulong TotalPlayed = 0;

        private readonly ulong[,] Positive = new ulong[Squares, Pieces];
        private readonly ulong[,] All = new ulong[Squares, Pieces];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece)
        {
            return ((byte)piece >> 1) - 2; //BlackPawn = 0...
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            ulong inc = Inc(depth);
            TotalPositive += inc;
            Positive[move.ToSquare, iPiece] += inc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Played(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            ulong inc = Inc(depth);
            TotalPlayed += inc;
            All[move.ToSquare, iPiece] += inc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            float a = Positive[move.ToSquare, iPiece];
            float b = All[move.ToSquare, iPiece];
            //local-ratio / average-ratio
            return TotalPlayed * a / (b * TotalPositive + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Inc(int depth)
        {
            return (ulong)(depth * depth);
        }

    }
}