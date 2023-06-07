using System.Numerics;
using System;
using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        public static readonly int PhaseSum = 5000;

        public short PhaseValue;
        public EvalTerm Pawns;
        public EvalTermFloat Material;
        public EvalTerm Mobility;

        public float Phase => NormalizePhase(PhaseValue);

        public short Score { get; private set; }

        public short RawScore => (short)(EvalBase() + NormalizePhase(PhaseValue) * EvalEndgame());

        public Evaluation(BoardState board) : this()
        {
            PawnStructure.Update(board, ref Pawns);
            MobilityEval.Update(board, ref Mobility);

            AddPieces(board);
            UpdateScore(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void QuickUpdate(BoardState board, ref Move move)
        {
            PawnStructure.Update(board, ref move, ref Pawns);
            TryUpdateMaterial(board, ref move);
            UpdateScore(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board, ref Move move)
        {
            Mobility = default;
            MobilityEval.Update(board, ref Mobility);
            PawnStructure.Update(board, ref Pawns);
            TryUpdateMaterial(board, ref move);
            UpdateScore(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryUpdateMaterial(BoardState board, ref Move move)
        {
            if (move.MovingPieceType() == Piece.King)
            {
                PhaseValue = 0;
                Material = default;
                AddPieces(board);
            }
            else
            {
                UpdateMaterial(ref move);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvalBase() => Pawns.Base + (int)Material.Base + Mobility.Base;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvalEndgame() => Pawns.Endgame + (int)Material.Endgame + Mobility.Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateScore(BoardState board)
        {
            float score = EvalBase() + NormalizePhase(PhaseValue) * EvalEndgame();
            Score = (short)(Endgame.IsDrawn(board) ? (int)score >> 4 : score);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPieces(BoardState board)
        {
            UpdateVariables(board);

            ulong occupied = board.Black | board.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                AddPiece(piece, square);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMaterial(ref Move move)
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

        private float[] _white;
        private float[] _black;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateVariables(BoardState board)
        {
            int WhiteKingSquare = Bitboard.LSB(board.White & board.Kings) ^ 56;
            int BlackKingSquare = Bitboard.LSB(board.Black & board.Kings);
            _white = GetVariables(WhiteKingSquare, BlackKingSquare);
            _black = GetVariables(BlackKingSquare, WhiteKingSquare);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            PhaseValue += Weights.PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
            {
                AddMaterial(pieceIndex, squareIndex ^ 56, _white);
            }
            else
            {
                SubtractMaterial(pieceIndex, squareIndex, _black);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            PhaseValue -= Weights.PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
            {
                SubtractMaterial(pieceIndex, squareIndex ^ 56, _white);
            }
            else
            {
                AddMaterial(pieceIndex, squareIndex, _black);
            }
        }

        private float[] GetVariables(int myKingSqr, int oppKingSqr)
        {
            float whiteKingFile = Bitboard.File(myKingSqr) / 3.5f - 1f;
            float whiteKingRank = Bitboard.Rank(myKingSqr) / 3.5f - 1f;
            float blackKingFile = Bitboard.File(oppKingSqr) / 3.5f - 1f;
            float blackKingRank = Bitboard.Rank(oppKingSqr) / 3.5f - 1f;

            return new float[]
            {
                whiteKingFile * whiteKingFile,
                whiteKingFile,
                whiteKingRank * whiteKingRank,
                whiteKingRank,
                blackKingFile * blackKingFile,
                blackKingFile,
                blackKingRank * blackKingRank,
                blackKingRank,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddMaterial(int pieceIndex, int squareIndex, float[] variables)
        {
            int entryIndex = Weights.MaterialTerms * ((pieceIndex << 6) | squareIndex);
            Vector<float> vars = new Vector<float>(variables);
            Vector<float> coefs = new Vector<float>(new Span<float>(Weights.MaterialWeights, entryIndex + 1, 8));
            Material.Base += Weights.MaterialWeights[entryIndex] + Vector.Dot(vars, coefs);
            coefs = new Vector<float>(new Span<float>(Weights.MaterialWeights, entryIndex + 10, 8));
            Material.Endgame += Weights.MaterialWeights[entryIndex + 9] + Vector.Dot(vars, coefs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubtractMaterial(int pieceIndex, int squareIndex, float[] variables)
        {
            int entryIndex = Weights.MaterialTerms * ((pieceIndex << 6) | squareIndex);
            Vector<float> vars = new Vector<float>(variables);
            Vector<float> coefs = new Vector<float>(new Span<float>(Weights.MaterialWeights, entryIndex + 1, 8));
            Material.Base -= Weights.MaterialWeights[entryIndex] + Vector.Dot(vars, coefs);
            coefs = new Vector<float>(new Span<float>(Weights.MaterialWeights, entryIndex + 10, 8));
            Material.Endgame -= Weights.MaterialWeights[entryIndex + 9] + Vector.Dot(vars, coefs);
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
        public static float NormalizePhase(float phaseValue) => (PhaseSum - phaseValue) / PhaseSum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;
    }
}
