using Leorik.Core;
using static Leorik.Tuning.Tuner;

namespace Leorik.Tuning
{
    static class KingSafetyTuner
    {
        const int N = 384; //6 Pieces * 64 Squares

        public static float[] GetLeorikKingSafetyCoefficients()
        {
            float[] result = new float[N];
            int index = 0;
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    result[index++] = Evaluation.KingSafetyTables[64 * piece + sq];
                }
            }
            return result;
        }

        public static float[] GetUntrainedKingSafetyCoefficients()
        {
            return new float[N];
        }

        public static float EvaluateKingSafety(BoardState board)
        {
            float[] coefficients = KingPhaseTuner.GetLeorikKingPhaseCoefficients();
            KingPhaseTuner.GetKingPhases(board, coefficients, out float wkPhase, out float bkPhase);
            return EvaluateWhite(board, wkPhase) - EvaluateBlack(board, bkPhase);
        }

        private static float EvaluateBlack(BoardState board, float blackKingPhase)
        {
            short blackKingSafetyScore = 0;
            for (ulong bits = board.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                int pieceIndex = PieceIndex(piece);
                int tableIndex = (pieceIndex << 6) | square;
                blackKingSafetyScore += Evaluation.KingSafetyTables[tableIndex];
            }

            return blackKingPhase * blackKingSafetyScore;
        }

        private static float EvaluateWhite(BoardState board, float whiteKingPhase)
        {
            short whiteKingSafetyScore = 0;
            for (ulong bits = board.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                int pieceIndex = PieceIndex(piece);
                int tableIndex = (pieceIndex << 6) | (square ^ 56);
                whiteKingSafetyScore += Evaluation.KingSafetyTables[tableIndex];
            }

            return whiteKingPhase * whiteKingSafetyScore;
        }

        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        internal static float[] GetFeaturesBlack(BoardState pos)
        {
            float[] result = new float[N];

            for (ulong bits = pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = pos.GetPiece(square);
                int pieceIndex = PieceIndex(piece);
                int tableIndex = (pieceIndex << 6) | square;
                result[tableIndex]++;
            }

            return result;
        }

        internal static float[] GetFeaturesWhite(BoardState pos)
        {
            float[] result = new float[N];

            for (ulong bits = pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = pos.GetPiece(square);
                int pieceIndex = PieceIndex(piece);
                int tableIndex = (pieceIndex << 6) | (square ^ 56);
                result[tableIndex]++;
            }

            return result;
        }

        internal static float EvaluateKingSafety(TuningData td, float[] cKingSafety)
        {
            float evalBlack = Evaluate(td.BlackFeatures, cKingSafety);
            float evalWhite = Evaluate(td.WhiteFeatures, cKingSafety);
            return td.WhiteKingPhase * evalWhite - td.BlackKingPhase * evalBlack;
        }

        internal static double MeanSquareError(List<TuningData> data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = entry.MaterialEval + EvaluateKingSafety(entry, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        internal static void Minimize(List<TuningData> data, float[] coefficients, float scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (TuningData entry in data)
            {
                float eval = entry.MaterialEval + EvaluateKingSafety(entry, coefficients);
                float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                //evalKS = WhiteKingPhase * evalWhite - td.BlackKingPhase * evalBlack;
                foreach (Feature f in entry.BlackFeatures)
                    accu[f.Index] -= error * entry.BlackKingPhase * f.Value;

                foreach (Feature f in entry.WhiteFeatures)
                    accu[f.Index] += error * entry.WhiteKingPhase * f.Value;

            }

            for (int i = 0; i < N; i++)
                coefficients[i] -= alpha * accu[i] / data.Count;
        }

        public static void MinimizeParallel(List<TuningData> data, float[] coefficients, float scalingCoefficient, float alpha)
        {
            //each thread maintains a local accu. After the loop is complete the accus are combined
            Parallel.ForEach(data,
                //initialize the local variable accu
                () => new float[N],
                //invoked by the loop on each iteration in parallel
                (entry, loop, accu) =>
                {
                    float eval = entry.MaterialEval + EvaluateKingSafety(entry, coefficients);
                    float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                    //evalKS = WhiteKingPhase * evalWhite - td.BlackKingPhase * evalBlack;
                    foreach (Feature f in entry.BlackFeatures)
                        accu[f.Index] -= error * entry.BlackKingPhase * f.Value;

                    foreach (Feature f in entry.WhiteFeatures)
                        accu[f.Index] += error * entry.WhiteKingPhase * f.Value;

                    return accu;
                },
                //executed when each partition has completed.
                (accu) =>
                {
                    lock (coefficients)
                    {
                        for (int i = 0; i < N; i++)
                            coefficients[i] -= alpha * accu[i] / data.Count;
                    }
                }
            );
        }
    }
}
