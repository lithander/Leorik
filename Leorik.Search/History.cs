using Leorik.Core;
using System.Runtime.CompilerServices;


namespace Leorik.Search
{
    public class History
    {
        private const int MaxPly = 99;
        private const int Squares = 64;
        private const int Pieces = 14; //including colored 'none'
        public const int ContDepth = 3;

        private ulong TotalPositive = 0;
        private ulong TotalPlayed = 0;

        long NullMovePassesSum = 0;
        long NullMovePassesCount = 1;

        private readonly ulong[,] Positive = new ulong[Squares, Squares];
        private readonly ulong[,] All = new ulong[Squares, Squares];
        private readonly Move[] Moves = new Move[MaxPly];
        private readonly Move[] Killers = new Move[MaxPly];
        private readonly Move[,,] Continuation = new Move[ContDepth, Squares, Pieces];

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

        const int CORR_TABLE = 19997; //prime!
        private readonly CorrEntry[] Corrections = new CorrEntry[8 * CORR_TABLE];


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

            for (int i = 0; i < Math.Min(ply, ContDepth); i++)
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
        public Move GetContinuation(int ply, int i)
        {
            if (i >= ply)
                return default;

            Move prev = Moves[ply - i - 1];
            return Continuation[i, prev.ToSquare, PieceIndex(prev)];
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
        public int GetAdjustedStaticEval(BoardState board)
        {
            int stm = (board.SideToMove == Color.Black) ? 1 : 0;

            int eval = board.SideToMoveScore();
            int corr = GetCorrection(board, stm);

            return eval + corr;
        }

        private int GetCorrection(BoardState board, int stm)
        {
            //Pawns->Knights->Bishops->Rooks->Queens->Kings;
            int result = GetCorrection(stm,  board.Pawns);
            result += GetCorrection(stm + 2, board.Knights | board.Bishops);
            result += GetCorrection(stm + 4, board.Queens | board.Rooks);
            result += GetCorrection(stm + 6, board.Kings);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateCorrection(BoardState board, int depth, int delta)
        {
            long inc = depth * depth;
            long corr = inc * Math.Clamp(delta, -100, +100);
            int stm = (board.SideToMove == Color.Black) ? 1 : 0;

            AddCorrection(stm,     corr, inc, board.Pawns);
            AddCorrection(stm + 2, corr, inc, board.Knights | board.Bishops);
            AddCorrection(stm + 4, corr, inc, board.Queens | board.Rooks);
            AddCorrection(stm + 6, corr, inc, board.Kings);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCorrection(int offset, long corr, long inc, ulong bits)
        {
            int index = (int)(bits % CORR_TABLE) + offset * CORR_TABLE;
            Corrections[index].Add(corr, inc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCorrection(int offset, ulong bits)
        {
            int index = (int)(bits % CORR_TABLE) + offset * CORR_TABLE;
            return Corrections[index].Get();
        }
    }
}