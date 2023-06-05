using System;
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

        const int IsolatedPawns = 0;
        const int PassedPawns = 64;
        const int ProtectedPawns = 128;
        const int ConnectedPawns = 192;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref Move move, ref EvalTerm eval)
        {
            if (move.MovingPieceType() == Piece.Pawn || move.CapturedPieceType() == Piece.Pawn)
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
            AddIsolatedPawns(pos, ref eval);
            AddPassedPawns(pos, ref eval);
            AddProtectedPawns(pos, ref eval);
            AddConnectedPawns(pos, ref eval);
            return eval;
        }

        private static void AddPassedPawns(BoardState pos, ref EvalTerm eval)
        {
            for (ulong bits = Features.GetPassedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                eval.Subtract(ref Weights.PawnWeights[PassedPawns | square]);
            }

            for (ulong bits = Features.GetPassedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                eval.Add(ref Weights.PawnWeights[PassedPawns | square]);
            }
        }

        private static void AddIsolatedPawns(BoardState pos, ref EvalTerm eval)
        {
            for (ulong bits = Features.GetIsolatedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                eval.Subtract(ref Weights.PawnWeights[IsolatedPawns | square]);
            }

            for (ulong bits = Features.GetIsolatedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                eval.Add(ref Weights.PawnWeights[IsolatedPawns | square]);
            }
        }

        private static void AddConnectedPawns(BoardState pos, ref EvalTerm eval)
        {
            for (ulong bits = Features.GetConnectedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                eval.Subtract(ref Weights.PawnWeights[ConnectedPawns | square]);
            }

            for (ulong bits = Features.GetConnectedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                eval.Add(ref Weights.PawnWeights[ConnectedPawns | square]);
            }
        }

        private static void AddProtectedPawns(BoardState pos, ref EvalTerm eval)
        {
            for (ulong bits = Features.GetProtectedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                eval.Subtract(ref Weights.PawnWeights[ProtectedPawns | square]);
            }

            for (ulong bits = Features.GetProtectedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                eval.Add(ref Weights.PawnWeights[ProtectedPawns | square]);
            }
        }
    }
}
