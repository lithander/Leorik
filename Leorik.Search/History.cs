using Leorik.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace Leorik.Search
{
    public class History
    {
        //(new) 1 Killer vs (old) 2 Killers
        //Score of Leorik-2.5-B vs Leorik-2.5-A: 2281 - 2268 - 5451  [0.501] 10000
        //Elo difference: 0.5 +/- 4.6, LOS: 57.6 %, DrawRatio: 54.5 %

        //1 Killer + 1 Continuation (Counter OR FollowUp) vs (old) 2 KIllers
        //Score of Leorik-2.5-C vs Leorik-2.5-A: 2727 - 2203 - 5070  [0.526] 10000
        //Elo difference: 18.2 +/- 4.8, LOS: 100.0 %, DrawRatio: 50.7 %

        //1 Killer + 1 Continuation (Counter OR FollowUp) vs (new) 1 Killers
        //Score of Leorik-2.5-C vs Leorik-2.5-B: 2377 - 2101 - 4218  [0.516] 8696
        //Elo difference: 11.0 +/- 5.2, LOS: 100.0 %, DrawRatio: 48.5 %

        private const int MaxPly = 99;
        private const int Squares = 64;
        private const int Pieces = 12;

        private ulong[] TotalPositive = new ulong[Pieces + 2];
        private ulong[] TotalPlayed = new ulong[Pieces + 2];

        private readonly ulong[,,] Positive = new ulong[Pieces + 2, Squares, Pieces];
        private readonly ulong[,,] All = new ulong[Pieces + 2, Squares, Pieces];
        private readonly Move[] Moves = new Move[MaxPly];
        private readonly Move[] Killers = new Move[MaxPly];
        private readonly Move[,] Counter = new Move[Squares, Pieces + 2];
        private readonly Move[,] FollowUp = new Move[Squares, Pieces + 2];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Piece piece)
        {
            return ((byte)piece >> 1) - 2; //BlackPawn = 0...
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int ply, int depth, ref Move move)
        {
            int iMoving = PieceIndex(move.MovingPiece());
            int iTarget = PieceIndex(move.CapturedPiece());

            ulong inc = Inc(depth);
            TotalPositive[iTarget + 2] += inc;
            Positive[iTarget + 2, move.ToSquare, iMoving] += inc;

            //no killer, followup, counter tracking for captures
            if (iTarget >= 0)
                return;

            Killers[ply] = move;

            if (ply < 2)
                return;

            Move prev = Moves[ply - 2];
            FollowUp[prev.ToSquare, PieceIndex(prev.MovingPiece()) + 2] = move;

            if (ply < 1)
                return;

            prev = Moves[ply - 1];
            Counter[prev.ToSquare, PieceIndex(prev.MovingPiece()) + 2] = move;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Played(int ply, int depth, ref Move move)
        {
            int iMoving = PieceIndex(move.MovingPiece());
            int iTarget = PieceIndex(move.CapturedPiece());

            ulong inc = Inc(depth);
            TotalPlayed[iTarget + 2] += inc;
            All[iTarget + 2, move.ToSquare, iMoving] += inc;
            Moves[ply] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iMoving = PieceIndex(move.MovingPiece());
            int iTarget = PieceIndex(move.CapturedPiece());
            float a = Positive[iTarget + 2, move.ToSquare, iMoving];
            float b = All[iTarget + 2, move.ToSquare, iMoving];
            //local-ratio / average-ratio
            return TotalPlayed[iTarget + 2] * a / (b * TotalPositive[iTarget +2] + 1);
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
            return Counter[prev.ToSquare, PieceIndex(prev.MovingPiece()) + 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetFollowUp(int ply)
        {
            if (ply < 2) return default;
            Move prev = Moves[ply - 2];
            return FollowUp[prev.ToSquare, PieceIndex(prev.MovingPiece()) + 2];
        }
    }
}