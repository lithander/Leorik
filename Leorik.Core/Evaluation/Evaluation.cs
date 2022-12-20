using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        public static readonly int PhaseSum = 5000;

        public short PhaseValue;
        public EvalTerm Pawns;
        public EvalTerm Material;
        public EvalTerm Positional;

        public float Phase => NormalizePhase(PhaseValue);

        public short Score { get; private set; }

        public Evaluation(BoardState board) : this()
        {
            PawnStructure.Update(board, ref Pawns);
            Mobility.Update(board, ref Positional);
            //KingSafety.Update(board, ref _positional);
            AddPieces(board);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void QuickUpdate(BoardState board, ref Move move)
        {
            PawnStructure.Update(board, ref Pawns);
            UpdateMaterial(board, ref move);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board, ref Move move)
        {
            Positional = default;
            Mobility.Update(board, ref Positional);
            //KingSafety.Update(board, ref _positional);
            PawnStructure.Update(board, ref move, ref Pawns);
            UpdateMaterial(board, ref move);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateScore()
        {
            //TODO: use operator overloading to make this readable
            int mg = Pawns.Base + Material.Base + Positional.Base;
            int eg = Pawns.Endgame + Material.Endgame + Positional.Endgame;
            Score = (short)(mg + NormalizePhase(PhaseValue) * eg);
            //Console.WriteLine($"Phase:{Phase(_phaseValue)} Score:{Score}");
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
        private void UpdateMaterial(BoardState board, ref Move move)
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
            PhaseValue += Weights.PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                Material.AddFeature(pieceIndex, squareIndex ^ 56);
            else
                Material.SubtractFeature(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            PhaseValue -= Weights.PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                Material.SubtractFeature(pieceIndex, squareIndex ^ 56);
            else
                Material.AddFeature(pieceIndex, squareIndex);
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
        public static float NormalizePhase(float phaseValue)
        {
            return Math.Max((PhaseSum - phaseValue) / PhaseSum, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;
    }
}
