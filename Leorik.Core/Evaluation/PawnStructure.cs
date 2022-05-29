using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct PawnStructure
    {
        public struct PawnHashEntry
        {
            public PawnStructure Eval;
            public int StoreCount;
            public int Age;
            public ulong BlackPawns;
            public ulong WhitePawns;

            public PawnHashEntry(PawnStructure pawns, BoardState board, int count) : this()
            {
                Age = 0;
                StoreCount = count;
                Eval = pawns;
                BlackPawns = board.Black & board.Pawns;
                WhitePawns = board.White & board.Pawns;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool GetEval(BoardState board, ref PawnStructure eval)
            {
                if ((board.Black & board.Pawns) != BlackPawns)
                    return false;
                if ((board.White & board.Pawns) != WhitePawns)
                    return false;

                //hit resets the age
                Age = 0;
                eval = Eval;
                return true;
            }
        }

        public static ulong TableHits = 0;
        public static ulong TableMisses = 0;

        public const int HASH_TABLE_SIZE = 4999; //prime!
        public static PawnHashEntry[] PawnHashTable = new PawnHashEntry[HASH_TABLE_SIZE+1];

        const short ISOLATED_PAWN = -8;
        const short CONNECTED_PAWN = 6;
        const short PROTECTED_PAWN = 14;
        const short PASSED_RANK = 16;
        const short PASSED_CENTER = -12;

        public short Base;
        public short Endgame;

        public static void Clear()
        {
            PawnHashTable = new PawnHashEntry[HASH_TABLE_SIZE+1];
            TableHits = 0;
            TableMisses = 0;
        }

        public PawnStructure(BoardState pos) : this()
        {
            AddIsolatedPawns(pos);
            AddPassedPawns(pos);
            AddProtectedPawns(pos);
            AddConnectedPawns(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board, ref Move move)
        {
            if ((move.MovingPiece() & Piece.TypeMask) == Piece.Pawn ||
                (move.CapturedPiece() & Piece.TypeMask) == Piece.Pawn)
                Update(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board)
        {
            ulong index = board.Pawns % HASH_TABLE_SIZE;
            TableHits++;

            ref PawnHashEntry e0 = ref PawnHashTable[index];
            ref PawnHashEntry e1 = ref PawnHashTable[index ^ 1];
            e0.Age++;
            e1.Age++;

            if (e0.GetEval(board, ref this))
                return;

            if (e1.GetEval(board, ref this))
                return;


            TableHits--;
            TableMisses++;
            this = new PawnStructure(board);
            if (e1.Age > e0.Age)
            {
                e1.Age = 0;
                e1.StoreCount++;
                e1.Eval = new PawnStructure(board);
                e1.BlackPawns = board.Black & board.Pawns;
                e1.WhitePawns = board.White & board.Pawns;
            }
            else 
            {
                e0.Age = 0;
                e0.StoreCount++;
                e0.Eval = new PawnStructure(board);
                e0.BlackPawns = board.Black & board.Pawns;
                e0.WhitePawns = board.White & board.Pawns;
            }
        }

        private void AddPassedPawns(BoardState pos)
        {
            for (ulong bits = Features.GetPassedPawns(pos, Color.Black); bits != 0; bits = Bitboard.ClearLSB(bits))
                Endgame -= ScorePassedPawn(Bitboard.LSB(bits));

            for (ulong bits = Features.GetPassedPawns(pos, Color.White); bits != 0; bits = Bitboard.ClearLSB(bits))
                Endgame += ScorePassedPawn(Bitboard.LSB(bits) ^ 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short ScorePassedPawn(int square)
        {
            int rank = 8 - (square >> 3);
            int file = square & 7;
            int center = Math.Min(file, 7 - file);

            return (short)(PASSED_RANK * rank + PASSED_CENTER * center);
        }

        private void AddIsolatedPawns(BoardState pos)
        {
            int white = Bitboard.PopCount(Features.GetIsolatedPawns(pos, Color.White));
            int black = Bitboard.PopCount(Features.GetIsolatedPawns(pos, Color.Black));
            Base += (short)(ISOLATED_PAWN * (white - black));
        }

        private void AddConnectedPawns(BoardState pos)
        {
            int white = Bitboard.PopCount(Features.GetConnectedPawns(pos, Color.White));
            int black = Bitboard.PopCount(Features.GetConnectedPawns(pos, Color.Black));
            Base += (short)(CONNECTED_PAWN * (white - black));
        }

        private void AddProtectedPawns(BoardState pos)
        {
            int white = Bitboard.PopCount(Features.GetProtectedPawns(pos, Color.White));
            int black = Bitboard.PopCount(Features.GetProtectedPawns(pos, Color.Black));
            Base += (short)(PROTECTED_PAWN * (white - black));
        }
    }
}