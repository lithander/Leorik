using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Tuning
{
    static class MaterialTuner
    {
        const int N = 768; //(Midgame + Endgame) * 6 Pieces * 64 Squares = 768 coefficients

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
                    result[index++] = Evaluation.MidgameTables[64 * piece + sq];
                    result[index++] = Evaluation.EndgameTables[64 * piece + sq];
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

        internal static Feature[] _AdjustPhase(BoardState position, Feature[] features, float phase)
        {
            //*** This is the naive but slow approach ***
            float[] rawFeatures = GetFeatures(position, phase);
            Feature[] refResult = Condense(rawFeatures);

            //...but knowing the implementation details we can do it much faster...
            Feature[] result = AdjustPhase(features, phase);

            //...however, the results should be the same!
            if (refResult.Length != result.Length)
                throw new Exception("AdjustPhase is seriously buggy");
            float error = 0;
            for(int i = 0; i < result.Length; i++)
            {
                ref Feature a = ref refResult[i];
                ref Feature b = ref result[i];
                if(a.Index != b.Index)
                    throw new Exception("AdjustPhase is seriously buggy");

                error += Math.Abs(a.Value - b.Value);
            }
            if (error > 0.1)
                throw new Exception("AdjustPhase is seriously buggy");

            //...which is now verified! (Don't use outside debugging, obviuosly)
            return result;
        }

        internal static Feature[] AdjustPhase(Feature[] features, float phase)
        {
            //The amount of features could change when phase is or was zero. So let's count the mg features first
            //mg features are those with an even index
            int count = 0;
            foreach (var feature in features)
                if (feature.Index % 2 == 0)
                    count++;

            //1. no eg features present or needed -> no change!
            if (phase == 0 && features.Length == count)
                return features;

            //2. get rid of the endgame features
            if (phase == 0 && features.Length == 2 * count) 
            {
                Feature[] result = new Feature[count];
                for (int i = 0; i < features.Length; i += 2)
                    result[i/2] = features[i];

                return result;
            }

            //3. just update the eg values
            if (phase > 0 && features.Length == 2 * count)
            {
                for (int i = 0; i < features.Length; i += 2)
                    features[i + 1].Value = features[i].Value * phase;

                return features;
            }

            //4. construct eg features from the mg features
            Debug.Assert(phase > 0 && features.Length == count);
            {
                Feature[] result = new Feature[2 * count];
                int index = 0;
                foreach (var feature in features)
                {
                    result[index++] = feature;
                    result[index++] = new()
                    {
                        Index = (short)(feature.Index + 1),
                        Value = feature.Value * phase
                    };
                }
                return result;
            }
        }

        internal static void GetEvalTerms(BoardState pos, float[] cMaterial, out float midgame, out float endgame)
        {
            midgame = 0; 
            endgame = 0;
            
            ulong occupied = pos.Black | pos.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = pos.GetPiece(square);
                int pieceOffset = ((int)piece >> 2) - 1;
                int squareIndex = (piece & Piece.ColorMask) == Piece.White ? square ^ 56 : square;
                int sign = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;

                int iMg = pieceOffset * 128 + 2 * squareIndex;
                midgame += sign * cMaterial[iMg];
                endgame += sign * cMaterial[iMg+1];
            }
        }

        public static double MeanSquareError(List<TuningData> data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry.MaterialFeatures, coefficients);
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
                float eval = Evaluate(entry.MaterialFeatures, coefficients);
                float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                foreach (Feature f in entry.MaterialFeatures)
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
                    float eval = Evaluate(entry.MaterialFeatures, coefficients);
                    float error = Sigmoid(eval, scalingCoefficient) - entry.Result;

                    foreach (Feature f in entry.MaterialFeatures)
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
