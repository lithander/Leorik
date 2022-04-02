using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    class PhaseTuningData
    {
        public float MidgameScore;
        public float EndgameScore;
        public byte[] PieceCounts;
        public sbyte Result;
    }

    static class PhaseTuner
    {
        const int N = 5; //[N, B, R, Q, 1] = 5 coefficients

        public static float[] GetLeorikPhaseCoefficients()
        {
            return new float[]
            {
                Evaluation.PhaseValues[1], //Knight
                Evaluation.PhaseValues[2], //Bishop
                Evaluation.PhaseValues[3], //Rook
                Evaluation.PhaseValues[4], //Queen
                Evaluation.Phase0 - Evaluation.Phase1,
            };
        }

        internal static PhaseTuningData GetTuningData(BoardState position, sbyte result)
        {
            var eval = new Evaluation(position);
            byte[] pieceCounts = CountPieces(position);
            return new PhaseTuningData
            {
                MidgameScore = eval.MG,
                EndgameScore = eval.EG,
                PieceCounts = pieceCounts,
                Result = result
            };
        }

        private static byte[] CountPieces(BoardState pos)
        {
            byte[] result = new byte[N];
            ulong occupied = pos.Black | pos.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = pos.GetPiece(square);
                int pieceOffset = ((int)piece >> 2) - 2; //P = -1, N = 0...
                if (pieceOffset >= 0 && pieceOffset <= 3) //no Pawns or Kings
                    result[pieceOffset]++;
            }
            //the fifth feature is always 1 to allow for a constant offset
            result[4] = 1;
            return result;
        }

        public static double MeanSquareError(List<PhaseTuningData> data, float[] coefficients, double scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (PhaseTuningData entry in data)
            {
                float eval = Evaluate(entry, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Evaluate(PhaseTuningData features, float[] coefficients)
        {
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            float phaseValue = 0;
            for (int i = 0; i < N; i++)
                phaseValue += features.PieceCounts[i] * coefficients[i];

            float phase = Phase(phaseValue);
            return features.MidgameScore + phase * features.EndgameScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Phase(float phaseValue)
        {
            return Math.Clamp((Evaluation.Phase0 - phaseValue) / Evaluation.Phase0, 0, 1);
        }

        internal static void Minimize(List<PhaseTuningData> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (PhaseTuningData entry in data)
            {
                float phaseValue = 0;
                for (int i = 0; i < N; i++)
                    phaseValue += entry.PieceCounts[i] * coefficients[i];

                float phase = Phase(phaseValue);
                float eval = entry.MidgameScore + phase * entry.EndgameScore;

                float error = (float)SignedError(entry.Result, eval, scalingCoefficient);
                float errorMg = (float)SignedError(entry.Result, entry.MidgameScore, scalingCoefficient);
                float delta = error - errorMg;

                //if error is positive a lower eval would have been better
                //the more positive the 'delta' is the more increasing the phase would increase eval
                //the higher the feature value the more increasing the coefficient would help
                for (int i = 0; i < N; i++)
                {
                    accu[i] += error * delta * entry.PieceCounts[i];
                }
            }

            for (int i = 0; i < N; i++)
                coefficients[i] += alpha * accu[i] / data.Count;
        }

        internal static void MinimizeParallel(List<PhaseTuningData> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            //each thread maintains a local accu. After the loop is complete the accus are combined
            Parallel.ForEach(data,
                //initialize the local variable accu
                () => new float[N],
                //invoked by the loop on each iteration in parallel
                (entry, loop, accu) =>
                {
                    float phaseValue = 0;
                    for (int i = 0; i < N; i++)
                        phaseValue += entry.PieceCounts[i] * coefficients[i];

                    float phase = Phase(phaseValue);
                    float eval = entry.MidgameScore + phase * entry.EndgameScore;

                    float error = (float)SignedError(entry.Result, eval, scalingCoefficient);
                    float errorMg = (float)SignedError(entry.Result, entry.MidgameScore, scalingCoefficient);
                    float delta = error - errorMg;

                    //if error is positive a lower eval would have been better
                    //the more positive the 'delta' is the more increasing the phase would increase eval
                    //the higher the feature value the more increasing the coefficient would help
                    for (int i = 0; i < N; i++)
                    {
                        accu[i] += error * delta * entry.PieceCounts[i];
                    }
                    return accu;
                },
                //executed when each partition has completed.
                (accu) =>
                {
                    lock (coefficients)
                    {
                        for (int i = 0; i < N; i++)
                            coefficients[i] += alpha * accu[i] / data.Count;
                    }
                }
            );
        }
    }
}
