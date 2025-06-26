using Leorik.Core;
using System.Runtime.CompilerServices;


namespace Leorik.Search
{
    public class History
    {
        private const int MaxPly = 99;
        private const int Squares = 64;
        private const int Pieces = 14; //including colored 'none'

        private ulong[] TotalPositive = new ulong[Pieces];
        private ulong[] TotalPlayed = new ulong[Pieces];

        long NullMovePassesSum = 0;
        long NullMovePassesCount = 1;

        private readonly ulong[,,] Positive = new ulong[Pieces, Squares, Pieces];
        private readonly ulong[,,] All = new ulong[Pieces, Squares, Pieces];
        private readonly Move[] Moves = new Move[MaxPly];
        private readonly Move[] Killers = new Move[MaxPly];
        private readonly Move[,] Counter = new Move[Squares, Pieces];
        private readonly Move[,] FollowUp = new Move[Squares, Pieces];

        const int CORR_HASH_TABLE_SIZE = 19997; //prime!

        struct CorrEntry
        {
            public long Numerator;
            public long Denominator;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(long corr, long inc)
            {
                Numerator += corr;
                Denominator += inc;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Get() => (int)(Numerator / (Denominator + 100));
        }
        private readonly CorrEntry[] PawnCorrection = new CorrEntry[2 * CORR_HASH_TABLE_SIZE];
        private readonly CorrEntry[] MinorPieceCorrection = new CorrEntry[2 * CORR_HASH_TABLE_SIZE];
        private readonly CorrEntry[] MajorPieceCorrection = new CorrEntry[2 * CORR_HASH_TABLE_SIZE];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PieceIndex(Move move)
        {
            //WhiteCastling=0, BlackCastling = 1, BlackPawn = 2, WhitePawn = 3 ... BlackKing = 12, WhiteKing = 13
            return move.IsCastling() ? (int)(move.Flags & Piece.ColorMask) >> 1 : (byte)move.MovingPiece() >> 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Good(int ply, int depth, ref Move move)
        {
            int iTarget = (int)move.CapturedPiece() >> 1;
            ulong inc = (ulong)(depth * depth);

            TotalPositive[iTarget] += inc;
            Positive[iTarget, move.ToSquare, PieceIndex(move)] += inc;

            //no killer, followup, counter tracking for captures
            if (move.CapturedPiece() != Piece.None)
                return;

            Killers[ply] = move;

            if (ply < 2)
                return;

            Move prev = Moves[ply - 1];
            Counter[prev.ToSquare, PieceIndex(prev)] = move;

            prev = Moves[ply - 2];
            FollowUp[prev.ToSquare, PieceIndex(prev)] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Played(int ply, int depth, ref Move move)
        {
            int iTarget = (int)move.CapturedPiece() >> 1;
            ulong inc = (ulong)(depth * depth);

            TotalPlayed[iTarget] += inc;
            All[iTarget, move.ToSquare, PieceIndex(move)] += inc;
            Moves[ply] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            int iTarget = (int)move.CapturedPiece() >> 1;
            int iMoving = PieceIndex(move);
            float a = Positive[iTarget, move.ToSquare, iMoving];
            float b = All[iTarget, move.ToSquare, iMoving];
            //local-ratio / average-ratio
            return TotalPlayed[iTarget] * a / (b * TotalPositive[iTarget] + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong AllCount(ref Move move)
        {
            int iTarget = (int)move.CapturedPiece() >> 1;
            int iMoving = PieceIndex(move);
            return All[iTarget, move.ToSquare, iMoving];
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
            return Counter[prev.ToSquare, PieceIndex(prev)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetFollowUp(int ply)
        {
            if (ply < 2)
                return default;

            Move prev = Moves[ply - 2];
            return FollowUp[prev.ToSquare, PieceIndex(prev)];
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
        private int CorrectionIndex(ulong bits) => (int)(bits % CORR_HASH_TABLE_SIZE) * 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAdjustedStaticEval(BoardState board)
        {
            int stm = (board.SideToMove == Color.Black) ? 1 : 0;

            int result = board.SideToMoveScore();

            int index = CorrectionIndex(board.Pawns) + stm;
            result += PawnCorrection[index].Get();

            index = CorrectionIndex(board.Knights | board.Bishops) + stm;
            result += MinorPieceCorrection[index].Get();

            index = CorrectionIndex(board.Queens | board.Rooks) + stm;
            result += MajorPieceCorrection[index].Get();

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateCorrection(BoardState board, int depth, int delta)
        {
            long inc = depth * depth;
            long corr = inc * Math.Clamp(delta, -100, +100);
            int stm = (board.SideToMove == Color.Black) ? 1 : 0;

            int index = CorrectionIndex(board.Pawns) + stm;
            PawnCorrection[index].Add(corr, inc);

            index = CorrectionIndex(board.Knights | board.Bishops) + stm;
            MinorPieceCorrection[index].Add(corr, inc);

            index = CorrectionIndex(board.Queens | board.Rooks) + stm;
            MajorPieceCorrection[index].Add(corr, inc);
        }
    }
}