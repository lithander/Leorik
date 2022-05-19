using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct PawnStructure
    {
        struct PawnHashEntry
        {
            public PawnStructure Eval;
            public ulong BlackPawns;
            public ulong WhitePawns;

            public PawnHashEntry(PawnStructure pawns, BoardState board) : this()
            {
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

                eval = Eval;
                return true;
            }
        }

        const int HASH_TABLE_SIZE = 4999; //prime!
        static PawnHashEntry[] PawnHashTable = new PawnHashEntry[HASH_TABLE_SIZE];

        const short ISOLATED_PAWN = -14;
        const short PASSED_RANK = 15;
        const short PASSED_CENTER = -12;

        public short Base;
        public short Endgame;

        public PawnStructure(BoardState pos) : this()
        {
            AddIsolatedPawns(pos);
            AddPassedPawns(pos);
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
            if (!PawnHashTable[index].GetEval(board, ref this))
            {
                this = new PawnStructure(board);
                PawnHashTable[index] = new PawnHashEntry(this, board);
            }
        }

        private void AddPassedPawns(BoardState pos)
        {
            ulong passed = Features.GetPassedPawns(pos);

            for (ulong bits = passed & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
                Endgame -= ScorePassedPawn(Bitboard.LSB(bits));

            for (ulong bits = passed & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
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
            ulong isolated = Features.GetIsolatedPawns(pos);

            for (ulong bits = isolated & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
                Base -= ISOLATED_PAWN;

            for (ulong bits = isolated & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
                Base += ISOLATED_PAWN;
        }
    }
}
