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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece) => (byte)piece >> 1; //BlackPawn = 0...

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int ply, int depth, ref Move move)
        {
            ulong inc = Inc(depth);
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

            ulong inc = Inc(depth);
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
        public ulong Inc(int depth)
        {
            return (ulong)(depth * depth);
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
        public void NullMovePassed(int eval, int beta)
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

        int _rootScore = 0;
        int _window = 20;
        int _alpha = 0;
        int _beta = 0;

        public void InitBounds(out int alpha, out int beta)
        {
            //Console.WriteLine($"New _window: {_window}");
            alpha = _alpha = _rootScore - _window;
            beta = _beta = _rootScore + _window;
        }

        public bool UpdateBounds(int score, out int alpha, out int beta)
        {
            _rootScore = score;
            if (score > _alpha && score < _beta)
            {
                float a = score - _alpha;
                float b = _beta - score;
                float ratio = ((a * a) + (b * b)) / ((a + b) * (a + b));
                //Console.WriteLine($"_window: {_window} ratio: {ratio}");
                _window = (int)Math.Max(20, ratio * ratio * _window);
                alpha = _alpha;
                beta = _beta;
                return true;
            }
            else
            {
                //Console.Write('.');
                _window *= 2;
                alpha = _alpha = score - _window;
                beta = _beta = score + _window;
            }
            return false;
        }
    }
}