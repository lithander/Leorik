using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Tuning
{
    static class FeatureTuner
    {
        //(Midgame + Endgame) * 6 Pieces * 64 Squares = 768 coefficients
        //(Midgame + Endgame) * (Doubled + Isolated + Passed) * 64 = 384 coefficients
        const int N = 768;
        //const int N = 1152 + 128 + 128; 

        public static float[] GetUntrainedCoefficients()
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
                    result[index++] = MaterialEval.MidgameTables[64 * piece + sq];
                    result[index++] = MaterialEval.EndgameTables[64 * piece + sq];
                }
            }
            return result;
        }

        public static float[] GetRandomCoefficients(int min, int max, int seed)
        {
            Random random = new Random(seed);
            float[] result = new float[N];
            int index = 0;
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    result[index++] = min + (max - min) * (float)random.NextDouble();
                    result[index++] = min + (max - min) * (float)random.NextDouble();
                }
            }
            return result;
        }


        delegate void FeatureHandler(int table, int square, int value);

        private static void IteratePieces(BoardState pos, ulong pieces, FeatureHandler action, int table)
        {
            for (ulong bits = pieces & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                action(table, square, -1);
            }
            for (ulong bits = pieces & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                action(table, square ^ 56, 1);
            }
        }

        public static float[] GetFeatures(BoardState pos, float phase)
        {
            float[] result = new float[N];

            //phase is used to interpolate between endgame and midgame score but we want to incorporate it into the features vector
            //score = midgameScore + phase * endgameScore
            void AddFeature(int table, int square, int value)
            {
                int index = table * 128 + 2 * square;
                result[index] += value;
                result[index + 1] += value * phase;
            }


            IteratePieces(pos, pos.Pawns,   AddFeature, 0);
            IteratePieces(pos, pos.Knights, AddFeature, 1);
            IteratePieces(pos, pos.Bishops, AddFeature, 2);
            IteratePieces(pos, pos.Rooks,   AddFeature, 3);
            IteratePieces(pos, pos.Queens,  AddFeature, 4);
            IteratePieces(pos, pos.Kings,   AddFeature, 5);

            //Pawn Structure
            //IteratePieces(pos, PawnStructure.GetConnectedOrProtected(pos), AddFeature, 6);
            //IteratePieces(pos, PawnStructure.GetDoubledPawns(pos), AddFeature, 7);
            //IteratePieces(pos, PawnStructure.GetPassedPawns(pos), AddFeature, 8);
            //IteratePieces(pos, PawnStructure.GetIsolatedPawns(pos), AddFeature, 9);
            //IteratePieces(pos, PawnStructure.GetConnectedPassedPawns(pos), AddFeature, 10);
            return result;
        }

        internal static void GetEvalTerms(Feature[] features, float[] coefficients, out float midgame, out float endgame)
        {
            midgame = 0;
            endgame = 0;
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            foreach (Feature feature in features)
            {
                if (feature.Index % 2 != 0)
                    continue; //ignore features with built-in phase!

                midgame += feature.Value * coefficients[feature.Index];
                endgame += feature.Value * coefficients[feature.Index + 1];
            }
        }

        public static double MeanSquareError(List<TuningData> data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry.Features, coefficients) + entry.Pawns.GetScore(entry.Phase);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        public static void Minimize(List<TuningData> data, float[] coefficients, float scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry.Features, coefficients) + entry.Pawns.GetScore(entry.Phase);
                float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                foreach (Feature f in entry.Features)
                    accu[f.Index] += error * f.Value;
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
                    float eval = Evaluate(entry.Features, coefficients) + entry.Pawns.GetScore(entry.Phase);
                    float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                    foreach (Feature f in entry.Features)
                        accu[f.Index] += error * f.Value;

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
