using Leorik.Core;
using System.Runtime.CompilerServices;

//Score of Leorik-3.1.10 vs Leorik-3.1.9: 2948 - 2839 - 5653  [0.505] 11440
//...      Leorik-3.1.10 playing White: 2666 - 248 - 2806  [0.711] 5720
//...      Leorik-3.1.10 playing Black: 282 - 2591 - 2847  [0.298] 5720
//...      White vs Black: 5257 - 530 - 5653  [0.707] 11440
//Elo difference: 3.3 +/- 4.5, LOS: 92.4 %, DrawRatio: 49.4 %

namespace Leorik.Search
{
    public class History
    {
        private const int MaxPly = 99;
        private const int Squares = 64;
        private const int Pieces = 14; //including colored 'none'
        private const int ContDepth = 2;

        private ulong TotalPositive = 0;
        private ulong TotalPlayed = 0;

        long NullMovePassesSum = 0;
        long NullMovePassesCount = 1;

        private readonly ulong[,] Positive = new ulong[Squares, Squares];
        private readonly ulong[,] All = new ulong[Squares, Squares];
        private readonly Move[] Moves = new Move[MaxPly];
        private readonly Move[] Killers = new Move[MaxPly];
        private readonly Move[,,] Continuation = new Move[ContDepth, Squares, Pieces];

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
            //no killer, followup, counter tracking for captures
            if (move.CapturedPiece() != Piece.None)
                return;

            ulong inc = (ulong)(depth * depth);
            TotalPositive += inc;
            Positive[move.ToSquare, move.FromSquare] += inc;
            Killers[ply] = move;

            for(int i = 0; i < Math.Min(ply, ContDepth); i++)
            {
                Move prev = Moves[ply - i - 1];
                Continuation[i, prev.ToSquare, PieceIndex(prev)] = move;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Played(int ply, int depth, ref Move move)
        {
            Moves[ply] = move;

            ulong inc = (ulong)(depth * depth);
            TotalPlayed += inc;

            if (move.CapturedPiece() != Piece.None)
                return;

            All[move.ToSquare, move.FromSquare] += inc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Value(ref Move move)
        {
            float a = Positive[move.ToSquare, move.FromSquare];
            float b = All[move.ToSquare, move.FromSquare];
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
            return Continuation[0, prev.ToSquare, PieceIndex(prev)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Move GetFollowUp(int ply)
        {
            if (ply < 2)
                return default;

            Move prev = Moves[ply - 2];
            return Continuation[1, prev.ToSquare, PieceIndex(prev)];
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