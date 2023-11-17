using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Leorik.Core
{
    public static class PawnStructure
    {
        struct PawnHashEntry
        {
            public EvalTerm Eval;
            public ulong Black;
            public ulong Pawns;
            public ulong Kings;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetEval(BoardState board, ref EvalTerm eval)
            {
                if (board.Pawns != Pawns || board.Kings != Kings)
                    return false;

                ulong boardBlack = board.Black & (board.Pawns | board.Kings);
                if (boardBlack != Black)
                    return false;

                eval = Eval;
                return true;
            }
        }

        const int HASH_TABLE_SIZE = 16661; //4999 is prime! (16661 also prime)
        static PawnHashEntry[] PawnHashTable = new PawnHashEntry[HASH_TABLE_SIZE];

        const int IsolatedPawns = 0;
        const int PassedPawns = 64;
        const int ProtectedPawns = 128;
        const int ConnectedPawns = 192;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void Update(BoardState board, ref Move move, ref EvalTerm eval)
        //{
        //    if (move.MovingPieceType() == Piece.Pawn || move.CapturedPieceType() == Piece.Pawn)
        //        Update(board, ref eval);
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(BoardState board, ref EvalTerm eval, Vector128<float> white, Vector128<float> black)
        {
            ulong index = (board.Pawns | board.Kings) % HASH_TABLE_SIZE;
            ref PawnHashEntry entry = ref PawnHashTable[index];
            if (entry.TryGetEval(board, ref eval))
                return;

            //compute pawnstructure from scratch
            Eval(board, ref eval, white, black);

            //and save it in the hash table
            entry.Eval = eval;
            entry.Black = board.Black & (board.Pawns | board.Kings);
            entry.Kings = board.Kings;
            entry.Pawns = board.Pawns;
        }

        public static void Eval(BoardState pos, ref EvalTerm eval, Vector128<float> white, Vector128<float> black)
        {
            //WHITE
            Vector256<float> blackWhite = Vector256.Create(black, white);

            for (ulong bits = Features.GetPassedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.SubtractPawns(PassedPawns | Bitboard.LSB(bits), blackWhite);
            
            for (ulong bits = Features.GetIsolatedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.SubtractPawns(IsolatedPawns | Bitboard.LSB(bits), blackWhite);
            
            for (ulong bits = Features.GetConnectedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.SubtractPawns(ConnectedPawns | Bitboard.LSB(bits), blackWhite);
            
            for (ulong bits = Features.GetProtectedBlackPawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.SubtractPawns(ProtectedPawns | Bitboard.LSB(bits), blackWhite);

            //BLACK
            Vector256<float> whiteBlack = Vector256.Create(white, black);

            for (ulong bits = Features.GetPassedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.AddPawns(PassedPawns | Bitboard.LSB(bits) ^ 56, whiteBlack);

            for (ulong bits = Features.GetIsolatedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.AddPawns(IsolatedPawns | Bitboard.LSB(bits) ^ 56, whiteBlack);

            for (ulong bits = Features.GetConnectedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.AddPawns(ConnectedPawns | Bitboard.LSB(bits) ^ 56, whiteBlack);

            for (ulong bits = Features.GetProtectedWhitePawns(pos); bits != 0; bits = Bitboard.ClearLSB(bits))
                eval.AddPawns(ProtectedPawns | Bitboard.LSB(bits) ^ 56, whiteBlack);
        }
    }
}
