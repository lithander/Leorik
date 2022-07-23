using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public static class PawnStructure
    {
        struct PawnHashEntry
        {
            public EvalTerm Eval;
            public ulong BlackPawns;
            public ulong WhitePawns;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetEval(BoardState board, ref EvalTerm eval)
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

        const short BACKWARD_PAWN = -5;
        const short ISOLATED_PAWN = -7;
        const short CONNECTED_PAWN = 6;
        const short PROTECTED_PAWN = 13;
        const short PASSED_RANK = 16;
        const short PASSED_CENTER = -12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref Move move, ref EvalTerm eval)
        {
            if ((move.MovingPiece() & Piece.TypeMask) == Piece.Pawn ||
                (move.CapturedPiece() & Piece.TypeMask) == Piece.Pawn)
                Update(board, ref eval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref EvalTerm eval)
        {
            ulong index = board.Pawns % HASH_TABLE_SIZE;
            ref PawnHashEntry entry = ref PawnHashTable[index];
            if (entry.TryGetEval(board, ref eval))
                return;

            //compute pawnstructure from scratch and save it in the hash table
            entry.Eval = eval = Eval(board);
            entry.BlackPawns = board.Black & board.Pawns;
            entry.WhitePawns = board.White & board.Pawns;
        }

        public static EvalTerm Eval(BoardState pos)
        {
            EvalTerm eval = default;
            AddBackwardPawns(pos, ref eval);
            AddIsolatedPawns(pos, ref eval);
            AddPassedPawns(pos, ref eval);
            AddProtectedPawns(pos, ref eval);
            AddConnectedPawns(pos, ref eval);
            return eval;
        }

        private static void AddPassedPawns(BoardState pos, ref EvalTerm eval)
        {
            for (ulong bits = Features.GetPassedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.Endgame -= ScorePassedPawn(Bitboard.LSB(bits));

            for (ulong bits = Features.GetPassedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.Endgame += ScorePassedPawn(Bitboard.LSB(bits) ^ 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short ScorePassedPawn(int square)
        {
            int rank = 8 - (square >> 3);
            int file = square & 7;
            int center = Math.Min(file, 7 - file);
            return (short)(PASSED_RANK * rank + PASSED_CENTER * center);
        }

        private static void AddIsolatedPawns(BoardState pos, ref EvalTerm eval)
        {
            int white = Bitboard.PopCount(Features.GetIsolatedWhitePawns(pos));
            int black = Bitboard.PopCount(Features.GetIsolatedBlackPawns(pos));
            eval.Base += (short)(ISOLATED_PAWN * (white - black));
        }

        private static void AddConnectedPawns(BoardState pos, ref EvalTerm eval)
        {
            int white = Bitboard.PopCount(Features.GetConnectedWhitePawns(pos));
            int black = Bitboard.PopCount(Features.GetConnectedBlackPawns(pos));
            eval.Base += (short)(CONNECTED_PAWN * (white - black));
        }

        private static void AddProtectedPawns(BoardState pos, ref EvalTerm eval)
        {
            int white = Bitboard.PopCount(Features.GetProtectedWhitePawns(pos));
            int black = Bitboard.PopCount(Features.GetProtectedBlackPawns(pos));
            eval.Base += (short)(PROTECTED_PAWN * (white - black));
        }

        private static void AddBackwardPawns(BoardState pos, ref EvalTerm eval)
        {
            int white = Bitboard.PopCount(Features.GetBackwardWhitePawns(pos));
            int black = Bitboard.PopCount(Features.GetBackwardBlackPawns(pos));
            eval.Base += (short)(BACKWARD_PAWN * (white - black));
        }
    }
}
