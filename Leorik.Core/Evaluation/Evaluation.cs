using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        public static readonly int PhaseSum = 5000;
        public static readonly short[] PhaseValues = new short[6] { 0, 125, 341, 380, 808, 0 };

        private short _phaseValue;
        private PawnStructure _pawns;
        private Material _material;
        private short _mobility;

        public short Score { get; private set; }

        public Evaluation(BoardState board) : this()
        {
            _pawns.Update(board);
            _mobility = Mobility.Eval(board);
            AddPieces(board);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateScore()
        {
            int mg = _pawns.Base + _material.Base + _mobility;
            int eg = _pawns.Endgame + _material.Endgame;
            Score = (short)(mg + Phase(_phaseValue) * eg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPieces(BoardState board)
        {
            ulong occupied = board.Black | board.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                AddPiece(piece, square);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateMobility(BoardState board)
        {
            _mobility = Mobility.Eval(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdatePawns(BoardState board)
        {
            _pawns.Update(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateMaterial(BoardState board, ref Move move)
        {

            RemovePiece(move.MovingPiece(), move.FromSquare);
            AddPiece(move.NewPiece(), move.ToSquare);

            if (move.CapturedPiece() != Piece.None)
                RemovePiece(move.CapturedPiece(), move.ToSquare);

            switch (move.Flags)
            {
                case Piece.EnPassant | Piece.BlackPawn:
                    RemovePiece(Piece.WhitePawn, move.ToSquare + 8);
                    break;
                case Piece.EnPassant | Piece.WhitePawn:
                    RemovePiece(Piece.BlackPawn, move.ToSquare - 8);
                    break;
                case Piece.CastleShort | Piece.Black:
                    RemovePiece(Piece.BlackRook, 63);
                    AddPiece(Piece.BlackRook, 61);
                    break;
                case Piece.CastleLong | Piece.Black:
                    RemovePiece(Piece.BlackRook, 56);
                    AddPiece(Piece.BlackRook, 59);
                    break;
                case Piece.CastleShort | Piece.White:
                    RemovePiece(Piece.WhiteRook, 7);
                    AddPiece(Piece.WhiteRook, 5);
                    break;
                case Piece.CastleLong | Piece.White:
                    RemovePiece(Piece.WhiteRook, 0);
                    AddPiece(Piece.WhiteRook, 3);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue += PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                _material.AddScore(pieceIndex, squareIndex ^ 56);
            else
                _material.SubtractScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue -= PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                _material.SubtractScore(pieceIndex, squareIndex ^ 56);
            else
                _material.AddScore(pieceIndex, squareIndex);
        }

        public const int CheckmateBase = 9000;
        public const int CheckmateScore = 9999;
                

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMateDistance(int score)
        {
            int plies = CheckmateScore - Math.Abs(score);
            int moves = (plies + 1) / 2;
            return moves;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmate(int score) => Math.Abs(score) > CheckmateBase;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Checkmate(Color color, int ply) => (int)color * (ply - CheckmateScore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Checkmate(int ply) => (ply - CheckmateScore);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Phase(short phaseValue)
        {
            return Math.Clamp((float)(PhaseSum - phaseValue) / PhaseSum, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;
    }
}
