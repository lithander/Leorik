using Leorik.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Leorik.Tuning.Tuner;

namespace Leorik.Tuning
{
    class Data
    {
        public BoardState Position;
        public sbyte Result;
    }

    struct Feature
    {
        public short Index;
        public float Value;
    }

    class MaterialTuningData
    {
        public Feature[] Features;
        public sbyte Result;
    }

    class PhaseTuningData
    {
        public float MidgameScore;
        public float EndgameScore;
        public byte[] PieceCounts;
        public sbyte Result;
    }

    static class Tuner
    {
        public static double SignedError(float reference, float value, double scalingCoefficient)
        {
            double sigmoid = 2 / (1 + Math.Exp(-(value / scalingCoefficient))) - 1;
            return sigmoid - reference;
        }

        public static double SquareError(float reference, float value, double scalingCoefficient)
        {
            double sigmoid = 2 / (1 + Math.Exp(-(value / scalingCoefficient))) - 1;
            double error = reference - sigmoid;
            return (error * error);
        }

        public static double SquareError(int reference, int value, double scalingCoefficient)
        {
            double sigmoid = 2 / (1 + Math.Exp(-(value / scalingCoefficient))) - 1;
            double error = reference - sigmoid;
            return (error * error);
        }

        public static double Minimize(Func<double, double> func, double range0, double range1)
        {
            //find k that minimizes result of func(k)
            Console.WriteLine($"[{range0:F3}..{range1:F3}]");
            double step = (range1 - range0) / 10.0;
            double min_k = range0;
            double min = func(min_k);
            for (double k = range0; k < range1; k += step)
            {
                double y = func(k);
                if (y < min)
                {
                    min = y;
                    min_k = k;
                }
            }
            Console.WriteLine($"min_k: {min_k:F3}, step: {step:F3}");
            if (step < 0.1)
                return min_k;

            //min_k is not precise enough! Try values in the interval of [-step, step] around min_k
            return Minimize(func, min_k - step, min_k + step);
        }

        public static double MeanSquareError(List<Data> data, double scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (Data entry in data)
            {
                var eval = new Evaluation(entry.Position);
                squaredErrorSum += SquareError(entry.Result, eval.Score, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }
    }

    static class PhaseTuner
    {
        const int N = 6; //4 pieces (N, B, R, Q) + phase0 + phase1 = 6 coefficients

        public static float[] GetLeorikPhaseCoefficients()
        {
            return new float[]
            {
                Evaluation.PhaseValues[1], //Knight
                Evaluation.PhaseValues[2], //Bishop
                Evaluation.PhaseValues[3], //Rook
                Evaluation.PhaseValues[4], //Queen
                Evaluation.Phase0,
                Evaluation.Phase1
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
            byte[] result = new byte[4];
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
            for (int i = 0; i < 4; i++)
                phaseValue += features.PieceCounts[i] * coefficients[i];

            float phase = Phase(phaseValue, coefficients[4], coefficients[5]);
            return features.MidgameScore + phase * features.EndgameScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Phase(float phaseValue, float phase0, float phase1)
        {
            return Math.Clamp((float)(phaseValue - phase0) / (phase1 - phase0), 0, 1);
        }

        internal static void Minimize(List<PhaseTuningData> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            float[] accu = new float[4];
            foreach (PhaseTuningData entry in data)
            {
                float phaseValue = 0;
                for (int i = 0; i < 4; i++)
                    phaseValue += entry.PieceCounts[i] * coefficients[i];

                float phase = Phase(phaseValue, coefficients[4], coefficients[5]);
                float eval = entry.MidgameScore + phase * entry.EndgameScore;
                              
                float error = (float)SignedError(entry.Result, eval, scalingCoefficient);              
                
                float errorMg = (float)SignedError(entry.Result, entry.MidgameScore, scalingCoefficient);
                float delta = error - errorMg;

                //if error is positive a lower eval would have been better
                //the more positive the endgameScore is the more increasing the phase would increase eval
                //the higher the feature value the more increasing the coefficient would help
                for (int i = 0; i < 4; i++)
                {
                    accu[i] += error * delta * entry.PieceCounts[i];
                }
            }

            for (int i = 0; i < 4; i++)
                coefficients[i] += alpha * accu[i] / data.Count;
        }
    }


    static class MaterialTuner
    {
        const int N = 768; //(Midgame + Endgame) * 6 Pieces * 64 Squares = 768 coefficients

        public static float[] GetMaterialCoefficients()
        {
            float[] c = new float[N];

            int index = 0;
            for (int sq = 0; sq < 64; sq++, index += 2)
                c[index] = 100; //Pawns
            for (int sq = 0; sq < 64; sq++, index += 2)
                c[index] = 300; //Knights
            for (int sq = 0; sq < 64; sq++, index += 2)
                c[index] = 300; //Bishops
            for (int sq = 0; sq < 64; sq++, index += 2)
                c[index] = 500; //Rooks
            for (int sq = 0; sq < 64; sq++, index += 2)
                c[index] = 900; //Queens

            return c;
        }

        public static float[] GetLeorikCoefficients()
        {
            float[] result = new float[N];
            int index = 0;
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    result[index++] = Evaluation.MidgameTables[64 * piece + sq];
                    result[index++] = Evaluation.EndgameTables[64 * piece + sq];
                }
            }
            return result;
        }

        public static float[] GetFeatures(BoardState pos, float phase)
        {
            float[] result = new float[N];

            //phase is used to interpolate between endgame and midgame score but we want to incorporate it into the features vector
            //score = midgameScore + phase * endgameScore

            ulong occupied = pos.Black | pos.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = pos.GetPiece(square);
                int pieceOffset = ((int)piece >> 2) - 1;
                int squareIndex = (piece & Piece.ColorMask) == Piece.White ? square ^ 56 : square;
                int sign = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;

                int iMg = pieceOffset * 128 + 2 * squareIndex;
                int iEg = iMg + 1;
                result[iMg] += sign;
                result[iEg] += sign * phase;
            }
            return result;
        }

        internal static MaterialTuningData GetTuningData(BoardState position, sbyte result)
        {
            var eval = new Evaluation(position);
            float[] features = GetFeatures(position, eval.Phase);
            Feature[] denseFeatures = Condense(features);
            return new MaterialTuningData
            {
                Features = denseFeatures,
                Result = result
            };
        }

        public static short[] IndexBuffer(float[] values)
        {
            List<short> indices = new List<short>();
            for (short i = 0; i < N; i++)
                if (values[i] != 0)
                    indices.Add(i);

            return indices.ToArray();
        }

        public static Feature[] Condense(float[] features)
        {
            short[] indices = IndexBuffer(features);
            Feature[] denseFeatures = new Feature[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                short index = indices[i];
                denseFeatures[i].Index = index;
                denseFeatures[i].Value = features[index];
            }
            return denseFeatures;
        }

        public static double MeanSquareError(List<MaterialTuningData> data, float[] coefficients, double scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (MaterialTuningData entry in data)
            {
                float eval = Evaluate(entry.Features, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        public static void Minimize(List<MaterialTuningData> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (MaterialTuningData entry in data)
            {
                float eval = Evaluate(entry.Features, coefficients);
                double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
                float error = (float)(sigmoid - entry.Result);

                foreach (Feature f in entry.Features)
                    accu[f.Index] += error * f.Value;
            }

            for (int i = 0; i < N; i++)
                coefficients[i] -= alpha * accu[i] / data.Count;
        }

        public static void MinimizeParallel(List<MaterialTuningData> data, float[] coefficients, double scalingCoefficient, float alpha)
        {            
            //each thread maintains a local accu. After the loop is complete the accus are combined
            Parallel.ForEach(data,
                //initialize the local variable accu
                () => new float[N],
                //invoked by the loop on each iteration in parallel
                (entry, loop, accu) => 
                {
                    float eval = Evaluate(entry.Features, coefficients);
                    double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
                    float error = (float)(sigmoid - entry.Result);

                    foreach (Feature f in entry.Features)
                        accu[f.Index] += error * f.Value;

                    return accu;
                },
                //executed when each partition has completed.
                (accu) =>
                {
                    lock(coefficients)
                    {
                        for (int i = 0; i < N; i++)
                            coefficients[i] -= alpha * accu[i] / data.Count;
                    }
                }
            );
        }

        //public static void MinimizeSIMD(List<Data2> data, float[] coefficients, double scalingCoefficient, float alpha)
        //{
        //    int slots = Vector<float>.Count;
        //
        //    float[] accu = new float[N];
        //    foreach (Data2 entry in data)
        //    {
        //        //float eval = EvaluateSIMD(entry.Features, coefficients);
        //        float eval = Evaluate(entry.Features, entry.Indices, coefficients);
        //        double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
        //        float error = (float)(sigmoid - entry.Result);
        //
        //        for (int i = 0; i < N; i += slots)
        //        {
        //            Vector<float> vF = new Vector<float>(entry.Features, i);
        //            Vector<float> vA = new Vector<float>(accu, i);
        //            vA = Vector.Add(vA, Vector.Multiply(error, vF));
        //            vA.CopyTo(accu, i);
        //        }
        //    }
        //
        //    for (int i = 0; i < N; i++)
        //        coefficients[i] -= (alpha * accu[i]) / data.Count;
        //}

        //public static void MinimizeSparse(List<Data2> data, float[] coefficients, double scalingCoefficient, float alpha)
        //{
        //    float[] accu = new float[N];
        //    foreach (Data2 entry in data)
        //    {
        //        //float eval = EvaluateSIMD(entry.Features, coefficients);
        //        float eval = Evaluate(entry.Features, entry.Indices, coefficients);
        //        double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
        //        double error = (sigmoid - entry.Result);
        //
        //        foreach (short i in entry.Indices)
        //            accu[i] += (float)error * entry.Features[i];
        //    }
        //
        //    for (int i = 0; i < N; i++)
        //        coefficients[i] -= alpha * accu[i] / data.Count;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Evaluate(Feature[] features, float[] coefficients)
        {
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            float result = 0;
            foreach (Feature f in features)
                result += f.Value * coefficients[f.Index];
            return result;
        }

        //public static float Evaluate(float[] features, float[] coefficients)
        //{
        //    //dot product of features vector with coefficients vector
        //    float result = 0;
        //    for (int i = 0; i < N; i++)
        //        result += features[i] * coefficients[i];
        //    return result;
        //}
        //
        //public static float EvaluateSIMD(float[] features, float[] coefficients)
        //{
        //    //dot product of features vector with coefficients vector
        //    float result = 0;
        //    int slots = Vector<float>.Count;
        //    for (int i = 0; i < N; i += slots)
        //    {
        //        Vector<float> vF = new Vector<float>(features, i);
        //        Vector<float> vC = new Vector<float>(coefficients, i);
        //        result += Vector.Dot(vF, vC);
        //    }
        //    return result;
        //}
    }
}
