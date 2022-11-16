using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    static class FeatureTuner
    {
        //(Midgame + Endgame) * (6 Pieces + Isolated + Passed + Protected + Connected) * 64 = 1280 coefficients
        public const int MaterialTables = 6;
        public const int PawnStructureTables = 4;
        public const int PawnStructureWeights = 2 * PawnStructureTables * 64;
        public const int MaterialWeights = 2 * MaterialTables * 64;
        public const int MobilityWeights = 2 * 88;
        public const int AllWeigths = MaterialWeights + PawnStructureWeights + MobilityWeights;

        public static string[] TableNames = new string[]
        {
            "Pawns", "Knights", "Bishops", "Rooks", "Queens", "Kings",
            "Isolated Pawns", "Passed Pawns", "Protected Pawns", "Connected Pawns"
        };


        public static float[] GetUntrainedCoefficients()
        {
            float[] c = new float[AllWeigths];

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
            float[] result = new float[AllWeigths];
            int index = 0;
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    result[index++] = Weights.MidgameTables[64 * piece + sq];
                    result[index++] = Weights.EndgameTables[64 * piece + sq];
                }
            }
            return result;
        }

        public static float[] GetRandomCoefficients(int min, int max, int seed)
        {
            Random random = new Random(seed);
            float[] result = new float[AllWeigths];
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
            float[] result = new float[AllWeigths];

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
            IteratePieces(pos, Features.GetIsolatedPawns(pos), AddFeature, 6);
            IteratePieces(pos, Features.GetPassedPawns(pos), AddFeature, 7);
            IteratePieces(pos, Features.GetProtectedPawns(pos), AddFeature, 8);
            IteratePieces(pos, Features.GetConnectedPawns(pos), AddFeature, 9);

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

        internal static void Report(int offset, int count, int step, float[] coefficients)
        {
            for (int i = 0; i < count; i++)
            {
                int c = (int)Math.Round(coefficients[offset + i * step]);
                Console.Write(c);
                Console.Write(", ");
            }
            Console.WriteLine();
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
        public static float Evaluate(TuningData entry, float[] coefficients)
        {
            return Tuner.Evaluate(entry.Features, coefficients);
        }

        public static void Minimize(List<TuningData> data, float[] coefficients, float evalScaling, float alpha)
        {
            float[] accu = new float[AllWeigths];
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry, coefficients);
                float error = Sigmoid(eval, evalScaling) - entry.Result;

                foreach (Feature f in entry.Features)
                    accu[f.Index] += error * f.Value;
            }

            for (int i = 0; i < AllWeigths; i++)
                coefficients[i] -= alpha * accu[i] / data.Count;
        }

        public static void MinimizeParallel(List<TuningData> data, float[] coefficients, float evalScaling, float materialAlpha, float featureAlpha)
        {
            //each thread maintains a local accu. After the loop is complete the accus are combined
            Parallel.ForEach(data,
                //initialize the local variable accu
                () => new float[AllWeigths],
                //invoked by the loop on each iteration in parallel
                (entry, loop, accu) =>
                {
                    float eval = Evaluate(entry, coefficients);
                    float error = Sigmoid(eval, evalScaling) - entry.Result;

                    foreach (Feature f in entry.Features)
                        accu[f.Index] += error * f.Value;

                    return accu;
                },
                //executed when each partition has completed.
                (accu) =>
                {
                    lock (coefficients)
                    {
                        for (int i = 0; i < MaterialWeights; i++)
                            coefficients[i] -= materialAlpha * accu[i] / data.Count;

                        for (int i = MaterialWeights; i < AllWeigths; i++)
                            coefficients[i] -= featureAlpha * accu[i] / data.Count;
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
