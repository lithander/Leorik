using Leorik.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace Leorik.Search
{
    public class History
    {
        private const int MaxPly = 99;
        private const int Squares = 64;
        private const int Pieces = 14; //including colored 'none'

        private ulong TotalPositive = 0;
        private ulong TotalPlayed = 0;

        long NullMovePassesSum = 0;
        long NullMovePassesCount = 1;

        private readonly ulong[,] Positive = new ulong[Squares, Pieces];
        private readonly ulong[,] All = new ulong[Squares, Pieces];
        private readonly Move[] Moves = new Move[MaxPly];
        private readonly Move[] Killers = new Move[MaxPly];
        private readonly Move[,] Counter = new Move[Squares, Pieces];
        private readonly Move[,] FollowUp = new Move[Squares, Pieces];

        const int CORR_HASH_TABLE_SIZE = 4999; //prime!
        private readonly long[] Correction = new long[2 * CORR_HASH_TABLE_SIZE];
        private readonly long[] UpdateCount = new long[2 * CORR_HASH_TABLE_SIZE];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece) => (byte)piece >> 1; //BlackPawn = 0...

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int ply, int depth, ref Move move)
        {
            ulong inc = (ulong)(depth * depth);
            TotalPositive += inc;

            //no killer, followup, counter tracking for captures
            if (move.CapturedPiece() != Piece.None)
                return;

            int iMoving = PieceIndex(move.MovingPiece());
            Positive[move.ToSquare, iMoving] += inc;
            Killers[ply] = move;

            if (ply < 2)
                return;

            Move prev = Moves[ply - 2];
            FollowUp[prev.ToSquare, PieceIndex(prev.MovingPiece())] = move;

            prev = Moves[ply - 1];
            Counter[prev.ToSquare, PieceIndex(prev.MovingPiece())] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Played(int ply, int depth, ref Move move)
        {
            Moves[ply] = move;

            ulong inc = (ulong)(depth * depth);
            TotalPlayed += inc;

            if (move.CapturedPiece() != Piece.None)
                return;

            int iMoving = PieceIndex(move.MovingPiece());
            All[move.ToSquare, iMoving] += inc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iMoving = PieceIndex(move.MovingPiece());
            float a = Positive[move.ToSquare, iMoving];
            float b = All[move.ToSquare, iMoving];
            //local-ratio / average-ratio
            return TotalPlayed * a / (b * TotalPositive + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetKiller(int ply)
        {
            return Killers[ply];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetCounter(int ply)
        {
            Move prev = Moves[ply - 1];
            return Counter[prev.ToSquare, PieceIndex(prev.MovingPiece())];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetFollowUp(int ply)
        {
            if (ply < 2)
                return default;

            Move prev = Moves[ply - 2];
            return FollowUp[prev.ToSquare, PieceIndex(prev.MovingPiece())];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NullMovePass(int eval, int beta)
        {
            NullMovePassesCount++;
            NullMovePassesSum += eval - beta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExpectedFailHigh(int eval, int beta)
        {
            int avgNullMovePass = (int)(NullMovePassesSum / NullMovePassesCount);
            return eval > beta + avgNullMovePass;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCorrection(Color stm, ulong pawns)
        {
            int index = (int)(pawns % CORR_HASH_TABLE_SIZE) + ((stm == Color.Black) ? CORR_HASH_TABLE_SIZE : 0);
            long a = Correction[index];
            long b = UpdateCount[index];
            return (int)(a / (b + 100));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateCorrection(Color stm, int depth, ulong pawns, int delta)
        {
            int index = (int)(pawns % CORR_HASH_TABLE_SIZE) + ((stm == Color.Black) ? CORR_HASH_TABLE_SIZE : 0);
            long inc = depth * depth;
            Correction[index] += inc * Math.Clamp(delta, -100, +100);
            UpdateCount[index] += inc;
        }
    }
}