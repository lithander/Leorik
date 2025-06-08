using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core
{
    public struct NeuralNetEval
    {
        public record struct Bucket(int Index, bool Mirrored);

        public Bucket BlackBucket;
        public Bucket WhiteBucket;

        public short[] Black;
        public short[] White;

        public short Score { get; private set; }

        public NeuralNetEval()
        {
            Black = new short[Network.Default.Layer1Size];
            White = new short[Network.Default.Layer1Size];
        }

        public NeuralNetEval(NeuralNetEval eval)
        {
            Black = new short[Network.Default.Layer1Size];
            White = new short[Network.Default.Layer1Size];
            Copy(eval);
        }

        public NeuralNetEval(BoardState board)
        {
            Black = new short[Network.Default.Layer1Size];
            White = new short[Network.Default.Layer1Size];
            Reset(board);
        }

        public void Copy(NeuralNetEval other)
        {
            Array.Copy(other.Black, Black, Network.Default.Layer1Size);
            Array.Copy(other.White, White, Network.Default.Layer1Size);
            WhiteBucket = other.WhiteBucket;
            BlackBucket = other.BlackBucket;
            Score = other.Score;
        }

        public void Reset(BoardState board)
        {
            ResetWhiteAccu(board);
            ResetBlackAcuu(board);
            UpdateEval(board);
        }

        public void Update(NeuralNetEval eval, Move move, BoardState newBoard)
        {
            Copy(eval);
            UpdateFeatures(ref move);

            if (WhiteKingBucket(newBoard) != WhiteBucket)
                ResetWhiteAccu(newBoard);
            else if (BlackKingBucket(newBoard) != BlackBucket)
                ResetBlackAcuu(newBoard);

            UpdateEval(newBoard);
        }

        private void UpdateEval(BoardState board)
        {
            int pieceCount = Bitboard.PopCount(board.White | board.Black);
            int outputBucket = Network.Default.GetMaterialBucket(pieceCount);
            Score = (short)Evaluate(board.SideToMove, outputBucket);
        }

        private void ResetWhiteAccu(BoardState board)
        {
            Array.Copy(Network.Default.FeatureBiases, White, Network.Default.Layer1Size);
            WhiteBucket = WhiteKingBucket(board);

            for (ulong bits = board.White | board.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                (_, int whiteIdx) = FeatureIndices(piece, square);
                AddWeights(White, Network.Default.FeatureWeights, whiteIdx);
            }
        }

        private void ResetBlackAcuu(BoardState board)
        {
            Array.Copy(Network.Default.FeatureBiases, Black, Network.Default.Layer1Size);
            BlackBucket = BlackKingBucket(board);

            for (ulong bits = board.White | board.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                (int blackIdx, _) = FeatureIndices(piece, square);
                AddWeights(Black, Network.Default.FeatureWeights, blackIdx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bucket WhiteKingBucket(BoardState board)
        {
            int square = Bitboard.LSB(board.White & board.Kings);
            return new Bucket(Network.Default.InputBucketMap[square], Bitboard.File(square) >= 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bucket BlackKingBucket(BoardState board)
        {
            int square = Bitboard.LSB(board.Black & board.Kings) ^ 56;
            return new Bucket(Network.Default.InputBucketMap[square], Bitboard.File(square) >= 4);
        }

        private void UpdateFeatures(ref Move move)
        {
            Deactivate(move.MovingPiece(), move.FromSquare);
            Deactivate(move.Target, move.ToSquare);
            Activate(move.NewPiece(), move.ToSquare);
        
            switch (move.Flags)
            {
                case Piece.EnPassant | Piece.BlackPawn:
                    Deactivate(Piece.WhitePawn, move.ToSquare + 8);
                    break;
                case Piece.EnPassant | Piece.WhitePawn:
                    Deactivate(Piece.BlackPawn, move.ToSquare - 8);
                    break;
                case Piece.CastleShort | Piece.Black:
                    Activate(Piece.BlackRook, 61);
                    Activate(Piece.BlackKing, 62);
                    break;
                case Piece.CastleLong | Piece.Black:
                    Activate(Piece.BlackRook, 59);
                    Activate(Piece.BlackKing, 58);
                    break;
                case Piece.CastleShort | Piece.White:
                    Activate(Piece.WhiteRook, 5);
                    Activate(Piece.WhiteKing, 6);
                    break;
                case Piece.CastleLong | Piece.White:
                    Activate(Piece.WhiteRook, 3);
                    Activate(Piece.WhiteKing, 2);
                    break;
            }
        }

        private void Deactivate(Piece piece, int square)
        {
            if (piece != Piece.None)
            {
                (int blackIdx, int whiteIdx) = FeatureIndices(piece, square);
                SubtractWeights(Black, Network.Default.FeatureWeights, blackIdx);
                SubtractWeights(White, Network.Default.FeatureWeights, whiteIdx);
            }
        }

        private void Activate(Piece piece, int square)
        {
            if (piece != Piece.None)
            {
                (int blackIdx, int whiteIdx) = FeatureIndices(piece, square);
                AddWeights(Black, Network.Default.FeatureWeights, blackIdx);
                AddWeights(White, Network.Default.FeatureWeights, whiteIdx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int blackIdx, int whiteIdx) FeatureIndices(Piece piece, int square)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;
            const int BucketStride = Network.InputSize;

            int type = ((int)(piece & Piece.TypeMask) >> 2) - 1;
            int white = ((int)(piece & Piece.ColorMask) >> 1);

            int blackSquare = square ^ (BlackBucket.Mirrored ? 63 : 56);
            int blackIdx = BlackBucket.Index * BucketStride + white * ColorStride + type * PieceStride + blackSquare;

            int whiteSquare = square ^ (WhiteBucket.Mirrored ? 7 : 0);
            int whiteIdx = WhiteBucket.Index * BucketStride + (white ^ 1) * ColorStride + type * PieceStride + whiteSquare;

            return (blackIdx * Network.Default.Layer1Size, whiteIdx * Network.Default.Layer1Size);
        }

        private void AddWeights(short[] accu, short[] featureWeights, int offset)
        {
            //for (int i = 0; i < accu.Length; i++)
            //    accu[i] += featureWeights[offset + i];

            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(featureWeights.AsSpan(offset, Network.Default.Layer1Size));
            for (int i = 0; i < accuVectors.Length; i++)
                accuVectors[i] += weightsVectors[i];
        }

        private void SubtractWeights(short[] accu, short[] featureWeights, int offset)
        {
            //for (int i = 0; i < accu.Length; i++)
            //    accu[i] -= featureWeights[offset + i];

            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(featureWeights.AsSpan(offset, Network.Default.Layer1Size));
            for (int i = 0; i < accuVectors.Length; i++)
                accuVectors[i] -= weightsVectors[i];
        }

        private int Evaluate(Color stm, int outputBucket)
        {
            int output = (stm == Color.Black)
                ? EvaluateHiddenLayer(Black, White, Network.Default.OutputWeights, outputBucket)
                : EvaluateHiddenLayer(White, Black, Network.Default.OutputWeights, outputBucket);

            //during SCReLU values end up multiplied with QA * QA * QB
            //but OutputBias is quantized by only QA * QB
            output /= Network.QA;
            output += Network.Default.OutputBiases[outputBucket];
            //Now scale and convert back to float!
            return (output * Network.Scale) / (Network.QA * Network.QB);
        }

        private int EvaluateHiddenLayer(short[] us, short[] them, short[] weights, int bucket)
        {
            int length = Network.Default.Layer1Size;
            int offset = bucket * 2 * length;
            int sum = ApplySCReLU(us, weights.AsSpan(offset, length))
                    + ApplySCReLU(them, weights.AsSpan(offset + length, length));
            return sum;
        }

        private int ApplySCReLU(short[] accu, Span<short> weights)
        {
            //int SquaredClippedReLU(int value) => Math.Clamp(value, 0, Network.QA) * Math.Clamp(value, 0, Network.QA);
            //int sum = 0;
            //for (int i = 0; i < Network.Default.Layer1Size; ++i)
            //    sum += SquaredClippedReLU(accu[i]) * weights[i];
            //return sum;

            Vector256<short> ceil = Vector256.Create<short>(Network.QA);
            Vector256<short> floor = Vector256.Create<short>(0);
            
            Span<Vector256<short>> accuVectors = MemoryMarshal.Cast<short, Vector256<short>>(accu);
            Span<Vector256<short>> weightsVectors = MemoryMarshal.Cast<short, Vector256<short>>(weights);
            
            Vector256<int> sum = Vector256<int>.Zero;
            for (int i = 0; i < accuVectors.Length; i++)
            {
                Vector256<short> a = Vector256.Max(Vector256.Min(accuVectors[i], ceil), floor); //ClippedReLU
                Vector256<short> w = weightsVectors[i];
            
                if (Avx2.IsSupported)
                {
                    //with a being [0..255] and w being [-127..127] (a * w) fits into short but (a * a) can overflow
                    //so instead of (a * a) * w we will compute (a * w) * a
                    sum += Avx2.MultiplyAddAdjacent(w * a, a); //_mm256_madd_epi16
                }
                else
                {
                    (Vector256<int> a0, Vector256<int> a1) = Vector256.Widen(a);
                    (Vector256<int> w0, Vector256<int> w1) = Vector256.Widen(w);
                    sum += a0 * a0 * w0;
                    sum += a1 * a1 * w1;
                }
            }
            return Vector256.Sum(sum);
        }
    }
}
