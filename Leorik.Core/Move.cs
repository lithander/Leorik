using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public readonly struct Move
    {
        public readonly static Move BlackCastlingShort = new(Piece.BlackKing | Piece.CastleShort, 60, 62, Piece.None);//e8g8
        public readonly static Move BlackCastlingLong = new(Piece.BlackKing | Piece.CastleLong, 60, 58, Piece.None);//e8c8
        public readonly static Move WhiteCastlingShort = new(Piece.WhiteKing | Piece.CastleShort, 4, 6, Piece.None);//e1g1
        public readonly static Move WhiteCastlingLong = new(Piece.WhiteKing | Piece.CastleLong, 4, 2, Piece.None);//e1c1

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
            return Flags < Piece.KnightPromotion || Flags >= Piece.CastleShort
                ? Flags & Piece.PieceMask
                : (Piece)((int)Flags >> 3) & ~Piece.ColorMask | (Flags & Piece.ColorMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece CapturedPiece()
        {
            return Target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece MovingPiece()
        {
            return Flags & Piece.PieceMask;
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

        public override string ToString()
        {
            //result represents the move in the long algebraic notation (without piece names)
            string result = Notation.GetSquareName(FromSquare);
            result += Notation.GetSquareName(ToSquare);
            //the presence of a 5th character should mean promotion
            if (NewPiece() != MovingPiece())
                result += Notation.GetChar(NewPiece());

            return result;
        }

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
