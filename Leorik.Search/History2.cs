using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class History2
    {
        private const int MoveHashes = 128; //Power-of-Two required!
        private const int Squares = 64;
        private const int Pieces = 12;

        private ulong TotalPositive = 0;
        private ulong TotalPlayed = 0;

        private readonly ulong[,] Positive = new ulong[Squares, Pieces];
        private readonly ulong[,] All = new ulong[Squares, Pieces];

        private readonly ulong[,,] CounterMove = new ulong[Squares, Pieces, MoveHashes];
        private readonly ulong[,,] FollowUp = new ulong[Squares, Pieces, MoveHashes];

        public void Scale()
        {
            //TotalPositive = 0;
            //TotalPlayed = 0;
            //
            //for (int square = 0; square < Squares; square++)
            //    for (int piece = 0; piece < Pieces; piece++)
            //    {
            //        Positive[square, piece] = 0;
            //        TotalPositive += Positive[square, piece];
            //
            //        All[square, piece] = 0;
            //        TotalPlayed += All[square, piece];
            //
            //        for (int cm = 0; cm < MoveHashes; cm++)
            //            CounterMove[square, piece, cm] = 0;
            //    }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece)
        {
            return ((byte)piece >> 1) - 2; //BlackPawn = 0...
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HashIndex(ulong moveHash)
        {
            return (int)(moveHash & (MoveHashes - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int depth, ulong counterMoveHash, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            ulong inc = Inc(depth);
            TotalPositive += inc;
            Positive[move.ToSquare, iPiece] += inc;
            if(counterMoveHash != 0)
                CounterMove[move.ToSquare, iPiece, HashIndex(counterMoveHash)] += inc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int depth, ulong counterMoveHash, ulong followUpHash, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            ulong inc = Inc(depth);
            TotalPositive += inc;
            Positive[move.ToSquare, iPiece] += inc;
            if (counterMoveHash != 0)
                CounterMove[move.ToSquare, iPiece, HashIndex(counterMoveHash)] += inc;
            if(followUpHash != 0)
                FollowUp[move.ToSquare, iPiece, HashIndex(followUpHash)] += inc;
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
        public float Value(ulong counterMoveHash, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            float b = All[move.ToSquare, iPiece];
            float a = Positive[move.ToSquare, iPiece];
            if (a == 0)
                return 0;

            if (counterMoveHash != 0)
                a += CounterMove[move.ToSquare, iPiece, HashIndex(counterMoveHash)];
            //float avg = TotalPositive / TotalPlayed;
            //float local = (a + c) / b;
            //return local / avg;
            return a * TotalPlayed / (b * TotalPositive + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ulong counterMoveHash, ulong followUpHash, ref Move move)
        {
            int iPiece = PieceIndex(move.MovingPiece());
            float b = All[move.ToSquare, iPiece];
            float a = Positive[move.ToSquare, iPiece];
            if (a == 0)
                return 0;
        
            if (counterMoveHash != 0)
                a += CounterMove[move.ToSquare, iPiece, HashIndex(counterMoveHash)];
            if (followUpHash != 0)
                a += FollowUp[move.ToSquare, iPiece, HashIndex(followUpHash)];
            //float avg = TotalPositive / TotalPlayed;
            //float local = (a + c) / b;
            //return local / avg;
            return a * TotalPlayed / (b * TotalPositive + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Inc(int depth)
        {
            return (ulong)(depth * depth);
        }

    }
}
