using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    public class See
    {
        public static readonly int[] PieceValues = new int[14]
        {
             0,     //Black = 0,
             0,     //White = 1,
            
            -100,   //BlackPawn = 2,
            +100,   //WhitePawn = 3,

            -300,   //BlackKnight = 4,
            +300,   //WhiteKnight = 5,
            
            -300,   //BlackBishop = 6,
            +300,   //WhiteBishop = 7,

            -500,   //BlackRook = 8,
            +500,   //WhiteRook = 9,
            
            -900,   //BlackQueen = 10,
            +900,   //WhiteQueen = 11,

            -9999,   //BlackKing = 12,
            +9999    //WhiteKing = 13,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PieceValue(Piece piece) => PieceValues[(int)piece >> 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EvaluateSign(BoardState position, Move move)
        {
            return Math.Sign(Evaluate(position, move));
        }

        public static int Evaluate(BoardState position, Move move)
        {
            position = position.Clone();
            int square = move.ToSquare;
            int alpha = -Evaluation.CheckmateScore;
            int beta = Evaluation.CheckmateScore;
            int see = move.IsEnPassant() ? PieceValue(move.MovingPiece()) : 0;

            while (true)
            {
                if ((move.CapturedPiece() & Piece.TypeMask) == Piece.King)
                    return position.SideToMove == Color.White ? beta : alpha;

                see -= PieceValue(move.CapturedPiece());

                if (move.IsPromotion())
                {
                    see -= PieceValue(move.MovingPiece());
                    see += PieceValue(move.NewPiece());
                }

                position.Play(move);

                if (position.SideToMove == Color.Black)
                    beta = Math.Min(beta, see); //Black can chose stand-put and keep this
                else
                    alpha = Math.Max(alpha, see); //White can chose stand-put and keep this

                if (!GetLeastValuableAttack(position, square, ref move))
                    return position.SideToMove == Color.White ? alpha : beta;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetLeastValuableAttack(BoardState board, int targetSquare, ref Move move)
        {
            if (board.SideToMove == Color.White)
                return GetWhitesLeastValuableAttack(board, targetSquare, ref move);
            else
                return GetBlacksLeastValuableAttack(board, targetSquare, ref move);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetBlacksLeastValuableAttack(BoardState board, int toSquare, ref Move move)
        {
            ulong target = 1UL << toSquare;
            Piece targetPiece = board.GetPiece(toSquare);

            //capture left
            ulong blackPawns = board.Black & board.Pawns;
            ulong left = (blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9;
            if((left & target) > 0)
            {
                if ((target & 0x00000000000000FFUL) > 0)
                    move = new Move(Piece.BlackPawn | Piece.QueenPromotion, toSquare + 9, toSquare, targetPiece);
                else
                    move = new Move(Piece.BlackPawn, toSquare + 9, toSquare, targetPiece);

                return true;
            }

            ulong right = (blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7;
            if ((right & target) > 0)
            {
                if ((target & 0x00000000000000FFUL) > 0)
                    move = new Move(Piece.BlackPawn | Piece.QueenPromotion, toSquare + 7, toSquare, targetPiece);
                else
                    move = new Move(Piece.BlackPawn, toSquare + 7, toSquare, targetPiece);

                return true;
            }

            ulong pieces = board.Black & board.Knights;
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[toSquare]) > 0)
            {
                int from = Bitboard.LSB(pieces & Bitboard.KnightTargets[toSquare]);
                move = new Move(Piece.BlackKnight, from, toSquare, targetPiece);
                return true;
            }

            pieces = board.Black & board.Bishops;
            if (pieces > 0 && (pieces & Bitboard.DiagonalMask[toSquare]) > 0)
            {
                pieces &= Bitboard.GetBishopTargets(board.Black | board.White, toSquare);
                if(pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.BlackBishop, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.Black & board.Rooks;
            if (pieces > 0 && (pieces & Bitboard.OrthogonalMask[toSquare]) > 0)
            {
                pieces &= Bitboard.GetRookTargets(board.Black | board.White, toSquare);
                if (pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.BlackRook, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.Black & board.Queens;
            if (pieces > 0 && (pieces & (Bitboard.DiagonalMask[toSquare] | Bitboard.OrthogonalMask[toSquare])) > 0)
            {
                pieces &= Bitboard.GetQueenTargets(board.Black | board.White, toSquare);
                if (pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.BlackQueen, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.Black & board.Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[toSquare]) > 0)
            {
                int from = Bitboard.LSB(pieces & Bitboard.KingTargets[toSquare]);
                move = new Move(Piece.BlackKing, from, toSquare, targetPiece);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetWhitesLeastValuableAttack(BoardState board, int toSquare, ref Move move)
        {
            ulong target = 1UL << toSquare;
            Piece targetPiece = board.GetPiece(toSquare);

            //capture left
            ulong whitePawns = board.Pawns & board.White;
            ulong left = (whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7;
            if ((left & target) > 0)
            {
                if ((target & 0xFF00000000000000UL) > 0)
                    move = new Move(Piece.WhitePawn | Piece.QueenPromotion, toSquare - 7, toSquare, targetPiece);
                else
                    move = new Move(Piece.WhitePawn, toSquare - 7, toSquare, targetPiece);

                return true;
            }

            ulong right = (whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9;
            if ((right & target) > 0)
            {
                if ((target & 0xFF00000000000000UL) > 0)
                    move = new Move(Piece.WhitePawn | Piece.QueenPromotion, toSquare - 9, toSquare, targetPiece);
                else
                    move = new Move(Piece.WhitePawn, toSquare - 9, toSquare, targetPiece);

                return true;
            }

            ulong pieces = board.White & board.Knights;
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[toSquare]) > 0)
            {
                int from = Bitboard.LSB(pieces & Bitboard.KnightTargets[toSquare]);
                move = new Move(Piece.WhiteKnight, from, toSquare, targetPiece);
                return true;
            }

            pieces = board.White & board.Bishops;
            if (pieces > 0 && (pieces & Bitboard.DiagonalMask[toSquare]) > 0)
            {
                pieces &= Bitboard.GetBishopTargets(board.Black | board.White, toSquare);
                if (pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.WhiteBishop, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.White & board.Rooks;
            if (pieces > 0 && (pieces & Bitboard.OrthogonalMask[toSquare]) > 0)
            {
                pieces &= Bitboard.GetRookTargets(board.Black | board.White, toSquare);
                if (pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.WhiteRook, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.White & board.Queens;
            if (pieces > 0 && (pieces & (Bitboard.DiagonalMask[toSquare] | Bitboard.OrthogonalMask[toSquare])) > 0)
            {
                pieces &= Bitboard.GetQueenTargets(board.Black | board.White, toSquare);
                if (pieces > 0)
                {
                    int from = Bitboard.LSB(pieces);
                    move = new Move(Piece.WhiteQueen, from, toSquare, targetPiece);
                    return true;
                }
            }

            pieces = board.White & board.Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[toSquare]) > 0)
            {
                int from = Bitboard.LSB(pieces & Bitboard.KingTargets[toSquare]);
                move = new Move(Piece.WhiteKing, from, toSquare, targetPiece);
                return true;
            }

            return false;
        }
    }
}
