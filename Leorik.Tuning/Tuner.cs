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
        //Material
        public Feature[] MaterialFeatures;
        //Phase
        public float MidgameEval;
        public float EndgameEval;
        public float Phase;
        public byte[] PieceCounts;
        //King Safety
        public Feature[] BlackFeatures;
        public Feature[] WhiteFeatures;
        public float BlackKingPhase;
        public float WhiteKingPhase;
        //King Phase
        public int BlackKingSquare;
        public int WhiteKingSquare;
        public float WhiteKingSafety;
        public float BlackKingSafety;

        public float MaterialEval => MidgameEval + Phase * EndgameEval;

        public float KingSafety => WhiteKingPhase * WhiteKingSafety - BlackKingPhase * BlackKingSafety;
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

        internal static TuningData GetTuningData(Data input, float[] cPhase, float[] cMaterial, float[] cKingPhase, float[] cKingSafety)
        {
            MaterialTuner.GetEvalTerms(input.Position, cMaterial, out float mgEval, out float egEval);
            byte[] pieceCounts = PhaseTuner.CountPieces(input.Position);
            float phase = PhaseTuner.GetPhase(pieceCounts, cPhase);
            float[] matFeatures = MaterialTuner.GetFeatures(input.Position, phase);

            KingPhaseTuner.GetKingPhases(input.Position, cKingPhase, out float wkPhase, out float bkPhase);
            Feature[] blackFeatures = Condense(KingSafetyTuner.GetFeaturesBlack(input.Position));
            Feature[] whiteFeatures = Condense(KingSafetyTuner.GetFeaturesWhite(input.Position));

            int blackKingSquare = Bitboard.LSB(input.Position.Kings & input.Position.Black);
            int whiteKingSquare = Bitboard.LSB(input.Position.Kings & input.Position.White);
            float evalBlackKs = Evaluate(blackFeatures, cKingSafety);
            float evalWhiteKs = Evaluate(whiteFeatures, cKingSafety);

            return new TuningData
            {
                Position = input.Position,
                Result = input.Result,
                
                MaterialFeatures = Condense(matFeatures),
                
                MidgameEval = mgEval,
                EndgameEval = egEval,
                PieceCounts = pieceCounts,
                Phase = phase,

                BlackFeatures = blackFeatures,
                BlackKingPhase = bkPhase,
                WhiteFeatures = whiteFeatures,
                WhiteKingPhase = wkPhase,

                BlackKingSquare = blackKingSquare,
                WhiteKingSquare = whiteKingSquare,
                WhiteKingSafety = evalWhiteKs,
                BlackKingSafety = evalBlackKs,
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

        internal static void SyncMaterialChanges(List<TuningData> data, float[] cMaterial)
        {
            //This is called after the material coefficients have been tuned. Now the phase-fatures need to be adjusted
            //so that the phase-eval and material-eval will agree again.
            foreach (var td in data)
            {
                MaterialTuner.GetEvalTerms(td.Position, cMaterial, out float mgEval, out float egEval);
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
                td.MaterialFeatures = MaterialTuner.AdjustPhase(td.MaterialFeatures, td.Phase);
            }
        }

        internal static void SyncKingSafetyChanges(List<TuningData> data, float[] cKingSafety)
        {
            //This is called after the king-safety coefficients have been tuned. Re-Evaluate the features! 
            foreach (var td in data)
            {
                td.WhiteKingSafety = Evaluate(td.WhiteFeatures, cKingSafety);
                td.BlackKingSafety = Evaluate(td.BlackFeatures, cKingSafety);
            }
        }

        internal static void SyncKingPhaseChanges(List<TuningData> data, float[] cKingPhase)
        {
            //This is called after the king-phase coefficients have been tuned. Re-Evaluate the white and black phases! 
            foreach (var td in data)
            {
                KingPhaseTuner.GetKingPhases(td, cKingPhase, out float wkPhase, out float bkPhase);
                td.BlackKingPhase = bkPhase;
                td.WhiteKingPhase = wkPhase;
            }
        }

        internal static void ValidateConsistency(List<TuningData> data, float[] cPhase, float[] cMaterial, float[] cKingPhase, float[] cKingSafety)
        {
            //This is called after the king-phase coefficients have been tuned. Re-Evaluate the white and black phases! 
            foreach (var td in data)
            {
                float a = KingPhaseTuner.EvaluateKingPhase(td, cKingPhase);
                float b = KingSafetyTuner.EvaluateKingSafety(td, cKingSafety);
                float c = Evaluate(td.MaterialFeatures, cMaterial);
                float d = PhaseTuner.Evaluate(td, cPhase);
                if (Math.Abs(a - b) > 0.1f)
                    throw new Exception("TuningData is out of Sync!");
                if (Math.Abs(c - d) > 0.1f)
                    throw new Exception("TuningData is out of Sync!");
                if (Math.Abs((a + c) - (b + d)) > 0.1f)
                    throw new Exception("TuningData is out of Sync!");
            }
        }

    }
}
