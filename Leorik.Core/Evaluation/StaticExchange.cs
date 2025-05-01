using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public class StaticExchange
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

        ulong White;
        ulong Black;
        ulong Pawns;
        ulong Knights;
        ulong Bishops;
        ulong Rooks;
        ulong Queens;
        ulong Kings;
        ulong ToBit;

        Piece Flags;
        Piece Target;
        int FromSquare;
        int ToSquare;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Piece NewPiece()
        {
            return Flags < Piece.KnightPromotion || Flags >= Piece.CastleShort
                ? Flags & Piece.PieceMask
                : (Piece)((int)Flags >> 3) & ~Piece.ColorMask | (Flags & Piece.ColorMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsPromotion() => Flags >= Piece.KnightPromotion && Flags < Piece.CastleShort;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Piece CapturedPieceType() => Target & Piece.TypeMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Piece MovingPiece() => Flags & Piece.PieceMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBad(BoardState position, ref Move move)
        {
            if (Move.Order(move.CapturedPiece()) >= Move.Order(move.MovingPiece()))
                return false;

            int see = Evaluate(position, move, -1, 1);
            return position.SideToMove == Color.White ? see < 0 : see > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EvaluateSign(BoardState position, Move move)
        {
            StaticExchange see = new();
            return Math.Sign(see.Evaluate(position, move, -1, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Evaluate(BoardState position, Move move)
        {
            StaticExchange see = new();
            return see.Evaluate(position, move, -Evaluation.CheckmateScore, Evaluation.CheckmateScore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PieceValue(int sign, Piece piece) => sign * PieceValues[(int)piece >> 1];

        public int Evaluate(BoardState position, Move move, int alpha, int beta)
        {
            if (move.Flags >= Piece.CastleShort)
                return 0;

            Flags = move.Flags;
            Target = move.Target;
            FromSquare = move.FromSquare;
            ToSquare = move.ToSquare;
            ToBit = 1UL << ToSquare;
            White = position.White & ~ToBit;
            Black = position.Black & ~ToBit;
            Pawns = position.Pawns & ~ToBit;
            Knights = position.Knights & ~ToBit & KnightTargets[ToSquare];
            Bishops = position.Bishops & ~ToBit & DiagonalMask[ToSquare];
            Rooks = position.Rooks & ~ToBit & OrthogonalMask[ToSquare];
            Queens = position.Queens & ~ToBit & (OrthogonalMask[ToSquare] | DiagonalMask[ToSquare]);
            Kings = position.Kings & ~ToBit & KingTargets[ToSquare];


            int sign = (int)position.SideToMove;
            if (sign > 0)
                PlayWhite();
            else
                PlayBlack();

            int see = move.IsEnPassant() ? PieceValue(sign, move.MovingPiece()) : 0;

            while (true)
            {
                if (CapturedPieceType() == Piece.King)
                    return sign * beta;

                see -= PieceValue(sign, Target);

                if (IsPromotion())
                {
                    see -= PieceValue(sign, MovingPiece());
                    see += PieceValue(sign, NewPiece());
                }

                if (see < alpha)
                    return sign * alpha; //move would be too bad, stm uses stand-pat

                beta = Math.Min(beta, see); //new stand-pat option for opponent

                if (sign > 0)
                {
                    if (!PlayBlacksLeastValuableAttack())
                        return beta;
                    Black ^= (1UL << FromSquare);
                }
                else
                {
                    if (!PlayWhitesLeastValuableAttack())
                        return -beta;
                    White ^= (1UL << FromSquare);
                }

                //swap the side like a negamax without recursion
                sign *= -1;
                see *= -1;
                (alpha, beta) = (-beta, -alpha);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PlayBlacksLeastValuableAttack()
        {
            Target = NewPiece();

            //capture left
            ulong blackPawns = Black & Pawns;
            ulong left = (blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9;
            if ((left & ToBit & 0xFFFFFFFFFFFFFF00UL) > 0)
            {
                Flags = Piece.BlackPawn;
                FromSquare = ToSquare + 9;
                return true;
            }

            ulong right = (blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7;
            if ((right & ToBit & 0xFFFFFFFFFFFFFF00UL) > 0)
            {
                Flags = Piece.BlackPawn;
                FromSquare = ToSquare + 7;
                return true;
            }

            ulong pieces = Black & Knights;
            if (pieces > 0)
            {
                Flags = Piece.BlackKnight;
                FromSquare = LSB(pieces);
                return true;
            }

            pieces = Black & Bishops;
            if (pieces > 0)
            {
                pieces &= GetBishopTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.BlackBishop;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            pieces = Black & Rooks;
            if (pieces > 0)
            {
                pieces &= GetRookTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.BlackRook;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            if ((left & ToBit & 0x00000000000000FFUL) > 0)
            {
                Flags = Piece.BlackPawn | Piece.QueenPromotion;
                FromSquare = ToSquare + 9;
                return true;
            }

            if ((right & ToBit & 0x00000000000000FFUL) > 0)
            {
                Flags = Piece.BlackPawn | Piece.QueenPromotion;
                FromSquare = ToSquare + 7;
                return true;
            }

            pieces = Black & Queens;
            if (pieces > 0)
            {
                pieces &= GetQueenTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.BlackQueen;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            pieces = Black & Kings;
            if (pieces > 0)
            {
                Flags = Piece.BlackKing;
                FromSquare = LSB(pieces);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PlayWhitesLeastValuableAttack()
        {
            Target = NewPiece();

            ulong whitePawns = Pawns & White;
            ulong left = (whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7;
            if ((left & ToBit & 0x00FFFFFFFFFFFFFFUL) > 0)
            {
                Flags = Piece.WhitePawn;
                FromSquare = ToSquare - 7;
                return true;
            }

            ulong right = (whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9;
            if ((right & ToBit & 0x00FFFFFFFFFFFFFFUL) > 0)
            {
                Flags = Piece.WhitePawn;
                FromSquare = ToSquare - 9;
                return true;
            }

            ulong pieces = White & Knights;
            if (pieces > 0)
            {
                Flags = Piece.WhiteKnight;
                FromSquare = LSB(pieces);
                return true;
            }

            pieces = White & Bishops;
            if (pieces > 0)
            {
                pieces &= GetBishopTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.WhiteBishop;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            pieces = White & Rooks;
            if (pieces > 0)
            {
                pieces &= GetRookTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.WhiteRook;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            if ((left & ToBit & 0xFF00000000000000UL) > 0)
            {
                Flags = Piece.WhitePawn | Piece.QueenPromotion;
                FromSquare = ToSquare - 7;
                return true;
            }

            if ((right & ToBit & 0xFF00000000000000UL) > 0)
            {
                Flags = Piece.WhitePawn | Piece.QueenPromotion;
                FromSquare = ToSquare - 9;
                return true;
            }

            pieces = White & Queens;
            if (pieces > 0)
            {
                pieces &= GetQueenTargets(Black | White, ToSquare);
                if (pieces > 0)
                {
                    Flags = Piece.WhiteQueen;
                    FromSquare = LSB(pieces);
                    return true;
                }
            }

            pieces = White & Kings;
            if (pieces > 0)
            {
                Flags = Piece.WhiteKing;
                FromSquare = LSB(pieces);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayBlack()
        {
            ulong bbFrom = 1UL << FromSquare;
            Black ^= bbFrom;

            switch (Flags & ~Piece.ColorMask)
            {
                case Piece.CastleShort:
                    Kings ^= bbFrom | ToBit;
                    Rooks ^= 0xA000000000000000UL;
                    Black ^= 0xA000000000000000UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | ToBit;
                    Rooks ^= 0x0900000000000000UL;
                    Black ^= 0x0900000000000000UL;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayWhite()
        {
            ulong bbFrom = 1UL << FromSquare;
            White ^= bbFrom;

            switch (Flags & ~Piece.ColorMask)
            {
                case Piece.CastleShort:
                    Rooks ^= 0x00000000000000A0UL;
                    White ^= 0x00000000000000A0UL;
                    break;
                case Piece.CastleLong:
                    Rooks ^= 0x0000000000000009UL;
                    White ^= 0x0000000000000009UL;
                    break;
            }
        }
    }
}
