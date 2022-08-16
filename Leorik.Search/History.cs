using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class History : IComparer<Move>
    {
        private const int Plies = 100;
        private const int Squares = 64;
        private const int Pieces = 12;

        private long TotalPositive = 0;
        private long TotalNegative = 0;

        private int TotalEntries = 0;
        private int[] EntriesPerDepth = new int[Plies];

        private readonly long[,] Positive = new long[Squares, Pieces];
        private readonly long[,] Negative = new long[Squares, Pieces];

        public void Scale(int scaleFactor = 2)
        {
            TotalEntries = 0;
            for (int i = 0; i < Plies; i++)
            {
                EntriesPerDepth[i] /= scaleFactor;
                TotalEntries += EntriesPerDepth[i];
            }

            TotalPositive = 0;
            TotalNegative = 0;
            for (int square = 0; square < Squares; square++)
                for (int piece = 0; piece < Pieces; piece++)
                {
                    Positive[square, piece] /= scaleFactor;
                    TotalPositive += Positive[square, piece];

                    Negative[square, piece] /= scaleFactor;
                    TotalNegative += Negative[square, piece];
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece)
        {
            return ((byte)piece >> 1) - 2; //BlackPawn = 0...
        }

        private int Inc(int depth)
        {
            return TotalEntries / (EntriesPerDepth[depth] + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            EntriesPerDepth[depth]++;
            TotalEntries++;

            int d = Inc(depth);
            TotalPositive += d;
            Positive[move.ToSquare, iPiece] += d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bad(int depth, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            EntriesPerDepth[depth]++;
            TotalEntries++;

            int d = Inc(depth);
            TotalNegative += d;
            Negative[move.ToSquare, iPiece] += d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            float a = Positive[move.ToSquare, iPiece];
            float b = Negative[move.ToSquare, iPiece];
            return a / (a + b + 1);//ratio of good increments normalized to the range of [0..1]
        }

        public void Log(int depth)
        {
            for (int i = 0; i < depth; i++)
                Console.WriteLine($"Depth: {i} Entries {EntriesPerDepth[i]} Inc: {Inc(i)}");
            Console.WriteLine();
        }

        public float Avg()
        {
            return TotalPositive / (float)(TotalPositive + TotalNegative + 1);
        }

        public int Compare(Move x, Move y)
        {
            float a = Value(ref x);
            float b = Value(ref y);
            return a.CompareTo(b);
        }
    }
}
