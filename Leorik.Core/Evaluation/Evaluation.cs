using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        //MSE_SCALING = 100;
        //ITERATIONS = 100;
        //MATERIAL_ALPHA = 500;
        //PHASE_ALPHA = 100;
        //MATERIAL_BATCH = 100;
        //PHASE_BATCH = 5;
        //~~~Phase~~~
        //N: 143 B: 278 R: 394 Q: 869
        //MSE(cFeatures) with MSE_SCALING = 100 on the dataset: 0,23595433788678255

        public static readonly int PhaseSum = 5000;
        public static readonly short[] PhaseValues = new short[6] { 0, 143, 278, 394, 869, 0 };

        public short PhaseValue;
        public short Positional;
        public short BishopPairs;
        public EvalTerm Pawns;
        public Material Material;

        public short Score { get; private set; }

        public Evaluation(BoardState board) : this()
        {
            Positional = Mobility.Eval(board);
            BishopPairs = BishopPair.Eval(board);
            PawnStructure.Update(board, ref Pawns);
            AddPieces(board);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void QuickUpdate(BoardState board, ref Move move)
        {
            PawnStructure.Update(board, ref Pawns);
            BishopPairs = BishopPair.Eval(board);
            UpdateMaterial(board, ref move);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board, ref Move move)
        {
            Positional = Mobility.Eval(board);
            BishopPairs = BishopPair.Eval(board);
            PawnStructure.Update(board, ref move, ref Pawns);
            UpdateMaterial(board, ref move);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateScore()
        {
            //TODO: use operator overloading to make this readable
            int mg = Pawns.Base + Material.Base + Positional + BishopPairs;
            int eg = Pawns.Endgame + Material.Endgame;
            Score = (short)(mg + Phase(PhaseValue) * eg);
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
            PhaseValue += PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                Material.AddScore(pieceIndex, squareIndex ^ 56);
            else
                Material.SubtractScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            PhaseValue -= PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                Material.SubtractScore(pieceIndex, squareIndex ^ 56);
            else
                Material.AddScore(pieceIndex, squareIndex);
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
