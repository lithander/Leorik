using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public static class KingRelativeEval
    {
        internal static void Update(BoardState board, ref EvalTerm eval)
        {
            int blackKingSquare = LSB(board.Black & board.Kings);
            int whiteKingSquare = LSB(board.White & board.Kings) ^ 56;

            Change(ref eval, 0, blackKingSquare, whiteKingSquare, PopCount(board.Black & board.Pawns), PopCount(board.White & board.Pawns));
            Change(ref eval, 1, blackKingSquare, whiteKingSquare, PopCount(board.Black & board.Knights), PopCount(board.White & board.Knights));
            Change(ref eval, 2, blackKingSquare, whiteKingSquare, PopCount(board.Black & board.Bishops), PopCount(board.White & board.Bishops));
            Change(ref eval, 3, blackKingSquare, whiteKingSquare, PopCount(board.Black & board.Rooks), PopCount(board.White & board.Rooks));
            Change(ref eval, 4, blackKingSquare, whiteKingSquare, PopCount(board.Black & board.Queens), PopCount(board.White & board.Queens));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Change(ref EvalTerm eval, int table, int blackKingSquare, int whiteKingSquare, int blackPieceCount, int whitePieceCount)
        {
            eval.Change((table + 10) * 64 + blackKingSquare, -blackPieceCount);
            eval.Change((table + 10) * 64 + whiteKingSquare, whitePieceCount);
            eval.Change((table + 15) * 64 + whiteKingSquare, -blackPieceCount);
            eval.Change((table + 15) * 64 + blackKingSquare, whitePieceCount);
        }

        internal static void Update(BoardState board, ref Move move, ref EvalTerm kingRelative)
        {
            if(move.MovingPieceType() == Piece.King)
            {
                kingRelative = default;
                Update(board, ref kingRelative);
            }
            else
            {
                if(move.MovingPiece() != move.NewPiece())
                {
                    ChangePiece(move.MovingPiece(), board, -1, ref kingRelative);
                    ChangePiece(move.NewPiece(), board, +1, ref kingRelative);
                }

                if (move.CapturedPiece() != Piece.None)
                    ChangePiece(move.CapturedPiece(), board, -1, ref kingRelative);

                if (move.Flags == (Piece.EnPassant | Piece.BlackPawn))
                    ChangePiece(Piece.WhitePawn, board, -1, ref kingRelative);
                else if(move.Flags == (Piece.EnPassant | Piece.WhitePawn))
                    ChangePiece(Piece.BlackPawn, board, -1, ref kingRelative);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ChangePiece(Piece piece, BoardState board, int count, ref EvalTerm kingRelative)
        {
            int table = PieceIndex(piece);
            if ((piece & Piece.ColorMask) == Piece.White)
            {
                int kingSquare = LSB(board.White & board.Kings) ^ 56;
                kingRelative.Change(table * 64 + kingSquare, count);
                kingSquare = LSB(board.Black & board.Kings);
                kingRelative.Change((5 + table) * 64 + kingSquare, count);
            }
            else
            {
                int kingSquare = LSB(board.Black & board.Kings);
                kingRelative.Change(table * 64 + kingSquare, -count);
                kingSquare = LSB(board.White & board.Kings) ^ 56;
                kingRelative.Change((5 + table) * 64 + kingSquare, -count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) + 9;
    }
}
