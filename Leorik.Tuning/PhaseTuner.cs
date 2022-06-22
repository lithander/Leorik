using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    static class PhaseTuner
    {
        const int N = 4; //[N, B, R, Q] = 4

        public static float[] GetUntrainedCoefficients()
        {
            return new float[]
            {
                200, //4xKnight
                300, //4xBishop
                500, //4xRook  
                700, //2xQueen 
            };
        }

        public static float[] GetLeorikPhaseCoefficients()
        {
            return new float[]
            {
                Evaluation.PhaseValues[1], //Knight
                Evaluation.PhaseValues[2], //Bishop
                Evaluation.PhaseValues[3], //Rook
                Evaluation.PhaseValues[4], //Queen
            };
        }

        public static byte[] CountPieces(BoardState pos)
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
            return result;
        }

        public static double MeanSquareError(List<TuningData> data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Evaluate(TuningData features, float[] coefficients)
        {
            float phaseValue = GetPhaseValue(features.PieceCounts, coefficients);
            float phase = Phase(phaseValue);
            return Evaluate(features, phase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetPhaseValue(byte[] pieceCounts, float[] cPhase)
        {
            float phaseValue = 0;
            for (int i = 0; i < N; i++)
                phaseValue += pieceCounts[i] * cPhase[i];
            return phaseValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetPhase(byte[] pieceCounts, float[] cPhase)
        {
            float phaseValue = GetPhaseValue(pieceCounts, cPhase);
            return Phase(phaseValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Phase(float phaseValue)
        {
            return Math.Clamp((Evaluation.PhaseSum - phaseValue) / Evaluation.PhaseSum, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate(TuningData entry, float phase)
        {
            return entry.MidgameEval + phase * entry.EndgameEval +
                   PawnEval(entry.Pawns, phase) +
                   entry.Mobility;
        }

        internal static void Minimize(List<TuningData> data, float[] coefficients, float scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (TuningData entry in data)
            {
                float phaseValue = 0;
                for (int i = 0; i < N; i++)
                    phaseValue += entry.PieceCounts[i] * coefficients[i];

                float phase = Phase(phaseValue);
                float eval = Evaluate(entry, phase);

                float error = SignedError(entry.Result, eval, scalingCoefficient);
                float errorMg = SignedError(entry.Result, entry.MidgameEval, scalingCoefficient);
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

            Normalize(coefficients);
        }

        private static void Normalize(float[] coefficients)
        {
            float nf = Evaluation.PhaseSum / PhaseSum(coefficients);
            for (int i = 0; i < N; i++)
                coefficients[i] *= nf;
        }

        private static float PhaseSum(float[] coefficients)
        {
            return 4 * coefficients[0] + // N
                   4 * coefficients[1] + // B
                   4 * coefficients[2] + // R
                   2 * coefficients[3];  // Q
        }

        internal static void MinimizeParallel(List<TuningData> data, float[] coefficients, float scalingCoefficient, float alpha)
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
                    float eval = Evaluate(entry, phase);

                    float error = SignedError(entry.Result, eval, scalingCoefficient);
                    float errorMg = SignedError(entry.Result, entry.MidgameEval, scalingCoefficient);
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

                        Normalize(coefficients);
                    }
                }
            );
        }
    }
}
