using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace Leorik.Core
{
    public struct NeuralNetEval
    {
        const int Layer1Size = 768;
        const int InputSize = 768;

        public short[] Black;
        public short[] White;

        public short Score { get; private set; }

        public NeuralNetEval()
        {
            Black = new short[Layer1Size];
            White = new short[Layer1Size];
        }

        public NeuralNetEval(NeuralNetEval eval)
        {
            Black = new short[Layer1Size];
            White = new short[Layer1Size];
            Copy(eval);
        }

        public NeuralNetEval(BoardState board)
        {
            Black = new short[Layer1Size];
            White = new short[Layer1Size];
            Update(board);
        }

        public void Copy(NeuralNetEval other)
        {
            Array.Copy(other.Black, Black, Layer1Size);
            Array.Copy(other.White, White, Layer1Size);
        }

        public void Update(BoardState board)
        {
            Array.Copy(Network.Default.FeatureBiases, Black, Layer1Size);
            Array.Copy(Network.Default.FeatureBiases, White, Layer1Size);

            ActivateAll(board);
            int stm = (int)board.SideToMove;
            int eval = Evaluate(board.SideToMove);
            Score = (short)(stm * eval);
        }

        public void Update(BoardState board, ref Move move)
        {
            UpdateFeatures(ref move);
            int stm = (int)board.SideToMove;
            int eval = Evaluate(board.SideToMove);
            Score = (short)(stm * eval);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateFeatures(ref Move move)
        {
            if ((move.MovingPiece() & Piece.ColorMask) == Piece.White)
            {
                Deactivate(move.MovingPiece(), move.FromSquare);
                Activate(move.NewPiece(), move.ToSquare);

                if (move.CapturedPiece() != Piece.None)
                    Deactivate(move.CapturedPiece(), move.ToSquare);
            }
            else
            {
                Deactivate(move.MovingPiece(), move.FromSquare);
                Activate(move.NewPiece(), move.ToSquare);

                if (move.CapturedPiece() != Piece.None)
                    Deactivate(move.CapturedPiece(), move.ToSquare);
            }

            switch (move.Flags)
            {
                case Piece.EnPassant | Piece.BlackPawn:
                    Deactivate(Piece.WhitePawn, move.ToSquare + 8);
                    break;
                case Piece.EnPassant | Piece.WhitePawn:
                    Deactivate(Piece.BlackPawn, move.ToSquare - 8);
                    break;
                case Piece.CastleShort | Piece.Black:
                    Deactivate(Piece.BlackRook, 63);
                    Activate(Piece.BlackRook, 61);
                    break;
                case Piece.CastleLong | Piece.Black:
                    Deactivate(Piece.BlackRook, 56);
                    Activate(Piece.BlackRook, 59);
                    break;
                case Piece.CastleShort | Piece.White:
                    Deactivate(Piece.WhiteRook, 7);
                    Activate(Piece.WhiteRook, 5);
                    break;
                case Piece.CastleLong | Piece.White:
                    Deactivate(Piece.WhiteRook, 0);
                    Activate(Piece.WhiteRook, 3);
                    break;
            }
        }

        private void ActivateAll(BoardState board)
        {
            for (ulong bits = board.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                Activate(piece, square);
            }

            for (ulong bits = board.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                Activate(piece, square);
            }
        }

        private void Deactivate(Piece piece, int square)
        {
            (int blackIdx, int whiteIdx) = FeatureIndices(piece, square);
            SubtractWeights(Black, Network.Default.FeatureWeights, blackIdx * Layer1Size);
            SubtractWeights(White, Network.Default.FeatureWeights, whiteIdx * Layer1Size);
        }

        private void Activate(Piece piece, int square)
        {
            (int blackIdx, int whiteIdx) = FeatureIndices(piece, square);
            AddWeights(Black, Network.Default.FeatureWeights, blackIdx * Layer1Size);
            AddWeights(White, Network.Default.FeatureWeights, whiteIdx * Layer1Size);
        }

        private (int blackIdx, int whiteIdx) FeatureIndices(Piece piece, int square)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int type = ((int)(piece & Piece.TypeMask) >> 2) - 1;
            int white = ((int)(piece & Piece.ColorMask) >> 1);

            int blackIdx = white * ColorStride + type * PieceStride + square ^ 0x38;
            int whiteIdx = (white ^ 1) * ColorStride + type * PieceStride + square;

            return (blackIdx, whiteIdx);
        }

        private void AddWeights(short[] accu, short[] featureWeights, int offset)
        {
            //assert(offset + Size <= delta.size());
            //for (int i = 0; i < accu.Length; i++)
            //    accu[i] += featureWeights[offset + i];

            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(featureWeights.AsSpan(offset, InputSize));
            for (int i = 0; i < accuVectors.Length; i++)
                accuVectors[i] += weightsVectors[i];
        }

        private void SubtractWeights(short[] accu, short[] featureWeights, int offset)
        {
            //assert(offset + Size <= delta.size());
            //for (int i = 0; i < accu.Length; i++)
            //    accu[i] -= featureWeights[offset + i];

            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(featureWeights.AsSpan(offset, InputSize));
            for (int i = 0; i < accuVectors.Length; i++)
                accuVectors[i] -= weightsVectors[i];
        }

        private int Evaluate(Color stm)
        {
            const int Scale = 400;
            const int Q = 255 * 64;

            int output = (stm == Color.Black)
                ? Forward(Black, White, Network.Default.OutputWeights)
                : Forward(White, Black, Network.Default.OutputWeights);
            return (output + Network.Default.OutputBias) * Scale / Q;
        }

        private int Forward(short[] us, short[] them, short[] weights)
        {
            int sum = ForwardCReLU(us, weights.AsSpan())
                    + ForwardCReLU(them, weights.AsSpan(Layer1Size));

            const int NormalizationK = 1;
            return sum / NormalizationK;
        }

        private int ForwardCReLU(short[] accu, Span<short> weights)
        {
            //for (int i = 0; i < Layer1Size; ++i)
            //    sum += ClippedReLU(accu[i]) * weights[i];

            Vector256<short> ceil = Vector256.Create<short>(255);
            Vector256<short> floor = Vector256.Create<short>(0);

            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(weights);

            Vector256<int> sum = Vector256<int>.Zero;
            for (int i = 0; i < accuVectors.Length; i++)
            {
                var a = Avx2.Max(Avx2.Min(accuVectors[i], ceil), floor);
                var w = weightsVectors[i];
                //result += Vector256.Dot(a, w);
                (Vector256<int> a0, var a1) = Vector256.Widen(a);
                (Vector256<int> w0, var w1) = Vector256.Widen(w);
                sum += a0 * w0;
                sum += a1 * w1;
            }
            return Vector256.Sum(sum);
        }
    }
}
