using Leorik.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Leorik.Tuning.Tuner;

namespace Leorik.Tuning
{
    struct Feature
    {
        public short Index;
        public float Value;
    }

    struct TuningData
    {
        public sbyte Result;
        public Feature[] Features;
        public float MidgameEval;
        public float EndgameEval;
        public float Phase;
        public byte[] PieceCounts;
    }

    static class Tuner
    {
        public const int AllWeigths = FeatureTuner.FeatureWeights + MobilityTuner.MobilityWeights;
        public static float[] AllocArray() => new float[AllWeigths];

        private static int[] FeaturePairs; //stores the 'source' index of features that were multiplied with phase
        private static int[] SparseToDense; //temporary buffer mapping sparse indices to dense indices

        static Tuner()
        {
            FeaturePairs = new int[AllWeigths];
            SparseToDense = new int[AllWeigths];
            FeatureTuner.DescribeFeaturePairs(FeaturePairs);
            MobilityTuner.DescribeFeaturePairs(FeaturePairs, FeatureTuner.FeatureWeights);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Sigmoid(float eval, float scalingCoefficient)
        {
            //[-1..1] f(0) = 0
            return (float)(2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedError(float reference, float value, float scalingCoefficient)
        {
            return Sigmoid(value, scalingCoefficient) - reference;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SquareError(float reference, float value, float scalingCoefficient)
        {
            float error = reference - Sigmoid(value, scalingCoefficient);
            return error * error;
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

        public static double MeanSquareError(List<Data> data, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (Data entry in data)
            {
                var eval = new Evaluation(entry.Position);
                squaredErrorSum += SquareError(entry.Result, eval.RawScore, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        internal static TuningData GetTuningData(Data input, float[] cPhase, float[] cFeatures)
        {
            byte[] pieceCounts = PhaseTuner.CountPieces(input.Position);
            float phase = PhaseTuner.GetPhase(pieceCounts, cPhase);

            float[] sparseFeatures = AllocArray();
            List<int> indices = new List<int>();
            FeatureTuner.AddFeatures(sparseFeatures, input.Position, phase);
            IndexBuffer(indices, sparseFeatures, 0);
            MobilityTuner.AddFeatures(sparseFeatures, input.Position, phase, FeatureTuner.FeatureWeights);
            IndexBuffer(indices, sparseFeatures, FeatureTuner.FeatureWeights);
            Feature[] features = Condense(indices.ToArray(), sparseFeatures);
            GetEvalTerms(features, cFeatures, out float mgEval, out float egEval);

            var td = new TuningData
            {
                Result = input.Result,               
                Features = features,
                MidgameEval = mgEval,
                EndgameEval = egEval,
                PieceCounts = pieceCounts,
                Phase = phase,
            };
            return td;
        }

        public static void IndexBuffer(List<int> indices, float[] values, int offset)
        {
            for (int i = offset; i < values.Length; i++)
                if (values[i] != 0)
                    indices.Add(i);
        }

        public static Feature[] Condense(int[] indices, float[] features)
        {
            Feature[] denseFeatures = new Feature[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                denseFeatures[i].Index = (short)index;
                denseFeatures[i].Value = features[index];
            }
            return denseFeatures;
        }

        internal static void SyncFeaturesChanges(TuningData[] data, float[] cFeatures)
        {
            //This is called after the material coefficients have been tuned. Now the phase-fatures need to be adjusted
            //so that the phase-eval and material-eval will agree again.
            for (int i = 0; i < data.Length; i++)
            {
                ref TuningData td = ref data[i];
                GetEvalTerms(td.Features, cFeatures, out float mgEval, out float egEval);
                td.MidgameEval = mgEval;
                td.EndgameEval = egEval;
            }
        }

        internal static void SyncPhaseChanges(TuningData[] data, float[] cPhase)
        {
            //This is called after the phase coefficients have been tuned. Now the material-fatures need to be adjusted
            //so that the phase-eval and material-eval will agree again.
            for (int i = 0; i < data.Length; i++)
            {
                ref TuningData td = ref data[i];
                td.Phase = PhaseTuner.GetPhase(td.PieceCounts, cPhase);
                //td.Features = MaterialTuner._AdjustPhase(td.Position, td.Features, phase);
                td.Features = AdjustPhase(td.Features, td.Phase);
            }
        }
        
        internal static void ValidateConsistency(TuningData[] data, float[] cPhase, float[] cFeatures)
        {
            //This is called after the king-phase coefficients have been tuned. Re-Evaluate the white and black phases! 
            foreach (var td in data)
            {
                float m = FeatureTuner.Evaluate(td, cFeatures);
                float p = PhaseTuner.Evaluate(td, cPhase);
                if (Math.Abs(m - p) > 0.1f)
                    throw new Exception("TuningData is out of Sync!");
            }
        }

        public static void GetEvalTerms(Feature[] features, float[] coefficients, out float midgame, out float endgame)
        {
            midgame = 0;
            endgame = 0;
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            foreach (Feature feature in features)
            {
                if (FeaturePairs[feature.Index] > 0)//this is an endgame feature and scaled by phase
                    endgame += feature.Value * coefficients[feature.Index];
                else
                    midgame += feature.Value * coefficients[feature.Index];
            }
        }

        internal static Feature[] AdjustPhase(Feature[] features, float phase)
        {
            for(int i = 0; i < features.Length; i++)
            {
                int sparseIndex = features[i].Index;
                if (FeaturePairs[sparseIndex] > 0)
                {
                    //this is an endgame feature and scaled by phase - restore it from unscaled partner
                    int sparseMgIndex = FeaturePairs[sparseIndex];
                    int denseMgIndex = SparseToDense[sparseMgIndex];
                    features[i].Value = phase * features[denseMgIndex].Value;
                }
                else
                {
                    //this is a midgame feature
                    SparseToDense[sparseIndex] = i;
                }
            }
            return features;
        }

        internal static void Rebalance(Piece piece, float[] featureWeights)
        {
            int featureTables = FeatureTuner.MaterialTables + FeatureTuner.PawnStructureTables;
            int mobilityOffset = 128 * featureTables;
            var avg = MobilityTuner.Rebalance(piece, mobilityOffset, featureWeights);
            FeatureTuner.Rebalance(piece, avg, featureWeights);
        }
        internal static void SampleRandomSlice(TuningData[] source, TuningData[] batch)
        {
            Random rng = new Random();
            int start = rng.Next(source.Length - batch.Length);
            Array.Copy(source, start, batch, 0, batch.Length);
        }

        internal static void Shuffle(TuningData[] data)
        {
            //https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
            Random rng = new Random();
            int n = data.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (data[k], data[n]) = (data[n], data[k]);
            }
        }

        internal static void Localize(TuningData[] tuningData)
        {
            for (int i = 0; i < tuningData.Length; i++)
                tuningData[i].Features = (Feature[])tuningData[i].Features.Clone();

            for (int i = 0; i < tuningData.Length; i++)
                tuningData[i].PieceCounts = (byte[])tuningData[i].PieceCounts.Clone();

        }
    }
}