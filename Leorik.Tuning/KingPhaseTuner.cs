using Leorik.Core;
using static Leorik.Tuning.Tuner;

namespace Leorik.Tuning
{
    internal class KingPhaseTuner
    {
        const int N = 64;

        public static float[] Weights = new float[N];

        public static float[] GetLeorikKingPhaseCoefficients()
        {
            float[] coefficients = new float[N];
            for (int i = 0; i < N; i++)
                coefficients[i] = (float)Evaluation.KingPhaseTable[i];
            return coefficients;
        }

        public static float[] GetUntrainedKingPhaseCoefficients()
        {
            float[] coefficients = new float[N];
            coefficients[62] = 1; //Kingside Castling
            coefficients[57] = -1; //Queenside Castling
            return coefficients;
        }

        public static float[] InitKingPlacementWeights(List<TuningData> data)
        {
            Weights = new float[N];
            foreach (TuningData entry in data)
            {
                Weights[entry.BlackKingSquare]++;
                Weights[entry.WhiteKingSquare]++;
            }
            for (int i = 0; i < N; i++)
                Weights[i] /= data.Count;
            return Weights;
        }

        public static void GetKingPhases(BoardState pos, float[] cKingPhase, out float wkPhase, out float bkPhase)
        {
            int blackKingSquare = Bitboard.LSB(pos.Kings & pos.Black);
            bkPhase = cKingPhase[blackKingSquare];
        
            int whiteKingSquare = Bitboard.LSB(pos.Kings & pos.White);
            wkPhase = cKingPhase[whiteKingSquare ^ 56];
        }

        public static void GetKingPhases(TuningData td, float[] cKingPhase, out float wkPhase, out float bkPhase)
        {
            bkPhase = cKingPhase[td.BlackKingSquare];
            wkPhase = cKingPhase[td.WhiteKingSquare ^ 56];
        }

        internal static float EvaluateKingPhase(TuningData td, float[] cKingPhase)
        {
            GetKingPhases(td, cKingPhase, out float wkPhase, out float bkPhase);
            return wkPhase * td.WhiteKingSafety - bkPhase * td.BlackKingSafety;
        }

        internal static double MeanSquareError(List<TuningData> data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = entry.MaterialEval + EvaluateKingPhase(entry, coefficients);
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
                float eval = entry.MaterialEval + EvaluateKingPhase(entry, coefficients);
                float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                //evalKS = WhiteKingPhase * evalWhite - td.BlackKingPhase * evalBlack;
                accu[entry.BlackKingSquare] -= error * entry.BlackKingSafety;
                accu[entry.WhiteKingSquare] += error * entry.WhiteKingSafety;
            }
                        
            for (int i = 0; i < N; i++)
                coefficients[i] -= alpha * accu[i] / data.Count * Weights[i];

            Clamp(coefficients);
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
                    float eval = entry.MaterialEval + EvaluateKingPhase(entry, coefficients);
                    float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                    //evalKS = WhiteKingPhase * evalWhite - td.BlackKingPhase * evalBlack;
                    accu[entry.BlackKingSquare] -= error * entry.BlackKingSafety;
                    accu[entry.WhiteKingSquare] += error * entry.WhiteKingSafety;

                    return accu;
                },
                //executed when each partition has completed.
                (accu) =>
                {
                    lock (coefficients)
                    {
                        for (int i = 0; i < N; i++)
                            coefficients[i] -= alpha * accu[i] / data.Count * Weights[i];

                        Clamp(coefficients);
                    }
                }
            );
        }

        private static void Clamp(float[] coefficients)
        {
            for (int i = 0; i < N; i++)
                coefficients[i] = Math.Max(-1, Math.Min(1, coefficients[i]));
        }
    }
}
