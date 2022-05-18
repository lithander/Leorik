using Leorik.Core;
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

    class Data
    {
        public BoardState Position;
        public sbyte Result;
    }

    class TuningData
    {
        public BoardState Position;
        public sbyte Result;
        //Pawns
        public PawnEval Pawns;
        //Material
        public Feature[] Features;
        //Phase
        public float MidgameEval;
        public float EndgameEval;
        public float Phase;
        public byte[] PieceCounts;
    }

    static class Tuner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Sigmoid(float eval, float scalingCoefficient)
        {
            //[-1..1] f(0) = 0
            return (float)(2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Evaluate(Feature[] features, float[] coefficients)
        {
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            float result = 0;
            foreach (Feature f in features)
                result += f.Value * coefficients[f.Index];
            return result;
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
                squaredErrorSum += SquareError(entry.Result, eval.Score, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        public static Data ParseEntry(string line)
        {
            //Expected Format:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            //Labels: "1/2-1/2", "1-0", "0-1"

            const string WHITE = "1-0";
            const string DRAW = "1/2-1/2";
            const string BLACK = "0-1";

            int iLabel = line.IndexOf('"');
            string fen = line.Substring(0, iLabel - 1);
            string label = line.Substring(iLabel + 1, line.Length - iLabel - 3);
            Debug.Assert(label == BLACK || label == WHITE || label == DRAW);
            int result = (label == WHITE) ? 1 : (label == BLACK) ? -1 : 0;
            return new Data
            {
                Position = Notation.GetBoardState(fen),
                Result = (sbyte)result
            };
        }

        internal static TuningData GetTuningData(Data input, float[] cPhase, float[] cFeatures)
        {
            byte[] pieceCounts = PhaseTuner.CountPieces(input.Position);
            float phase = PhaseTuner.GetPhase(pieceCounts, cPhase);
            Feature[] features = Condense(FeatureTuner.GetFeatures(input.Position, phase));
            FeatureTuner.GetEvalTerms(features, cFeatures, out float mgEval, out float egEval);
            PawnEval pawns = new PawnEval(input.Position);

            return new TuningData
            {
                Position = input.Position,
                Result = input.Result,               
                Features = features,
                Pawns = pawns,
                MidgameEval = mgEval,
                EndgameEval = egEval,
                PieceCounts = pieceCounts,
                Phase = phase,
            };
        }

        public static short[] IndexBuffer(float[] values)
        {
            List<short> indices = new List<short>();
            for (short i = 0; i < values.Length; i++)
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

        internal static void SyncFeaturesChanges(List<TuningData> data, float[] cFeatures)
        {
            //This is called after the material coefficients have been tuned. Now the phase-fatures need to be adjusted
            //so that the phase-eval and material-eval will agree again.
            foreach (var td in data)
            {
                FeatureTuner.GetEvalTerms(td.Features, cFeatures, out float mgEval, out float egEval);
                td.MidgameEval = mgEval;
                td.EndgameEval = egEval;
            }
        }

        internal static void SyncPhaseChanges(List<TuningData> data, float[] cPhase)
        {
            //This is called after the phase coefficients have been tuned. Now the material-fatures need to be adjusted
            //so that the phase-eval and material-eval will agree again.
            foreach (var td in data)
            {
                td.Phase = PhaseTuner.GetPhase(td.PieceCounts, cPhase);
                //td.Features = MaterialTuner._AdjustPhase(td.Position, td.Features, phase);
                td.Features = AdjustPhase(td.Features, td.Phase);
            }
        }
        
        internal static void ValidateConsistency(List<TuningData> data, float[] cPhase, float[] cFeatures)
        {
            //This is called after the king-phase coefficients have been tuned. Re-Evaluate the white and black phases! 
            foreach (var td in data)
            {
                float m = Evaluate(td.Features, cFeatures) + td.Pawns.GetScore(td.Phase);
                float p = PhaseTuner.Evaluate(td, cPhase);
                if (Math.Abs(m - p) > 0.1f)
                    throw new Exception("TuningData is out of Sync!");
            }
        }

        internal static Feature[] _AdjustPhase(BoardState position, Feature[] features, float phase)
        {
            //*** This is the naive but slow approach ***
            float[] rawFeatures = FeatureTuner.GetFeatures(position, phase);
            Feature[] refResult = Condense(rawFeatures);

            //...but knowing the implementation details we can do it much faster...
            Feature[] result = AdjustPhase(features, phase);

            //...however, the results should be the same!
            if (refResult.Length != result.Length)
                throw new Exception("AdjustPhase is seriously buggy");

            float error = 0;
            for (int i = 0; i < result.Length; i++)
            {
                ref Feature a = ref refResult[i];
                ref Feature b = ref result[i];
                if (a.Index != b.Index)
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
                    result[i / 2] = features[i];

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
    }
}