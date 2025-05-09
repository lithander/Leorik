using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public readonly struct Move
    {
        public readonly Piece Flags;
        public readonly Piece Target;
        public readonly byte FromSquare;
        public readonly byte ToSquare;

        public Move(Piece flags, int fromIndex, int toIndex, Piece target)
        {
            Flags = flags;
            FromSquare = (byte)fromIndex;
            ToSquare = (byte)toIndex;
            Target = target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece NewPiece()
        {
            if (Flags < Piece.KnightPromotion)
                return Flags & Piece.PieceMask;
            else if (Flags >= Piece.CastleShort)
                return Piece.None;
            else
                return (Piece)((int)Flags >> 3) & ~Piece.ColorMask | (Flags & Piece.ColorMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece CapturedPiece()
        {
            return Flags >= Piece.CastleShort ? Piece.None : Target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece MovingPiece()
        {
            return Flags & Piece.PieceMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece MovingPieceType()
        {
            return Flags & Piece.TypeMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPromotion()
        {
            return Flags >= Piece.KnightPromotion && Flags < Piece.CastleShort;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCastling()
        {
            return Flags >= Piece.CastleShort;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnPassant()
        {
            return (Flags & ~Piece.ColorMask) == Piece.EnPassant;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MvvLvaScore()
        {
            //Most valuable Victim, Least valuable Attacker
            //EnPassent = -1
            //King capturing Pawn = 1 * 6 - 6 = 0
            //Pawn capturing Queen = 6 * 5 - 1 = 29  
            return 6 * Order(Target) - Order(Flags & Piece.PieceMask);
        }

        //Pawn = 1, Knight = 2, Bishop = 3; Rook = 4, Queen = 5, King = 6
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Order(Piece piece) => (int)piece >> 2;

        public override string ToString() => Notation.GetMoveName(this, Variant.Chess960);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Move move)
        {
            return (int)move.Flags
                 |      move.FromSquare << 8
                 |      move.ToSquare << 16
                 | (int)move.Target << 24;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Move(int moveBits)
        {
            return new Move(
                (Piece) moveBits,           //Flags
                (byte) (moveBits >> 8),     //From
                (byte) (moveBits >> 16),    //To
                (Piece)(moveBits >> 24));   //Target
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Move lhs, Move rhs) => lhs.Equals(rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Move lhs, Move rhs) => !lhs.Equals(rhs);              

        public bool Equals(Move other)
        {
            return (Flags == other.Flags) &&
                   (Target == other.Target) &&
                   (FromSquare == other.FromSquare) &&
                   (ToSquare == other.ToSquare);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Move move)
                return Equals(move);

            return false;
        }

        public override int GetHashCode() => (int)this;
    }
}
