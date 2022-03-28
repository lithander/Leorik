using Leorik.Core;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    class Data
    {
        public BoardState Position;
        public sbyte Result;
    }

    class Data2
    {
        public float[] Features;
        public short[] Indices;
        public sbyte Result;
    }

    static class Tuner
    {
        const int N = 768; //(Midgame + Endgame) * 6 Pieces * 64 Squares = 768 coefficients

        public static float[] GetMaterialCoefficients()
        {
            float[] c = new float[N];

            int index = 0;
            for (int sq = 0; sq < 128; sq++)
                c[index++] = 100; //Pawns
            for (int sq = 0; sq < 128; sq++)
                c[index++] = 300; //Knights
            for (int sq = 0; sq < 128; sq++)
                c[index++] = 300; //Bishops
            for (int sq = 0; sq < 128; sq++)
                c[index++] = 500; //Rooks
            for (int sq = 0; sq < 128; sq++)
                c[index++] = 900; //Queens

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

        public static float[] GetFeatures(BoardState pos, double phase)
        {
            float[] result = new float[N];

            //phase is used to interpolate between endgame and midgame score but we want to incorporate it into the features vector
            //score = midgameScore + phase * (endgameScore - midgameScore)
            //score = midgameScore + phase * endgameScore - phase * midgameScore
            //score = phase * endgameScore + (1 - phase) * midgameScore;
            float phaseEg = (float)(phase);
            float phaseMG = (float)(1 - phase);

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
                result[iMg] += sign * phaseMG;
                result[iEg] += sign * phaseEg;
            }
            return result;
        }

        public static short[] IndexBuffer(float[] values)
        {
            List<short> indices = new List<short>();
            for (short i = 0; i < N; i++)
                if (values[i] != 0)
                    indices.Add(i);

            //Console.WriteLine(indices.Count / (float)values.Length);
            return indices.ToArray();
        }

        public static double MeanSquareError(List<Data2> data, float[] coefficients, double scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (Data2 entry in data)
            {
                //float eval = EvaluateSIMD(entry.Features, coefficients);
                float eval = Evaluate(entry.Features, entry.Indices, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        public static void Minimize(List<Data2> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (Data2 entry in data)
            {
                //float eval = EvaluateSIMD(entry.Features, coefficients);
                float eval = Evaluate(entry.Features, entry.Indices, coefficients);
                double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
                double error = (sigmoid - entry.Result);

                for (int i = 0; i < N; i++)
                    accu[i] += (float)error * entry.Features[i];
            }

            for (int i = 0; i < N; i++)
                coefficients[i] -= alpha * accu[i] / data.Count;
        }

        public static void MinimizeSIMD(List<Data2> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            int slots = Vector<float>.Count;

            float[] accu = new float[N];
            foreach (Data2 entry in data)
            {
                //float eval = EvaluateSIMD(entry.Features, coefficients);
                float eval = Evaluate(entry.Features, entry.Indices, coefficients);
                double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
                float error = (float)(sigmoid - entry.Result);

                for (int i = 0; i < N; i += slots)
                {
                    Vector<float> vF = new Vector<float>(entry.Features, i);
                    Vector<float> vA = new Vector<float>(accu, i);
                    vA = Vector.Add(vA, Vector.Multiply(error, vF));
                    vA.CopyTo(accu, i);
                }
            }

            for (int i = 0; i < N; i++)
                coefficients[i] -= (alpha * accu[i]) / data.Count;
        }

        public static void MinimizeSparse(List<Data2> data, float[] coefficients, double scalingCoefficient, float alpha)
        {
            float[] accu = new float[N];
            foreach (Data2 entry in data)
            {
                //float eval = EvaluateSIMD(entry.Features, coefficients);
                float eval = Evaluate(entry.Features, entry.Indices, coefficients);
                double sigmoid = 2 / (1 + Math.Exp(-(eval / scalingCoefficient))) - 1;
                double error = (sigmoid - entry.Result);

                foreach (short i in entry.Indices)
                    accu[i] += (float)error * entry.Features[i];
            }

            for (int i = 0; i < N; i++)
                coefficients[i] -= alpha * accu[i] / data.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Evaluate(float[] features, short[] indices, float[] coefficients)
        {
            //dot product of a selection (indices) of elements from the features vector with coefficients vector
            float result = 0;
            foreach (short i in indices)
                result += features[i] * coefficients[i];
            return result;
        }

        public static float Evaluate(float[] features, float[] coefficients)
        {
            //dot product of features vector with coefficients vector
            float result = 0;
            for (int i = 0; i < N; i++)
                result += features[i] * coefficients[i];
            return result;
        }

        public static float EvaluateSIMD(float[] features, float[] coefficients)
        {
            //dot product of features vector with coefficients vector
            float result = 0;
            int slots = Vector<float>.Count;
            for (int i = 0; i < N; i += slots)
            {
                Vector<float> vF = new Vector<float>(features, i);
                Vector<float> vC = new Vector<float>(coefficients, i);
                result += Vector.Dot(vF, vC);
            }
            return result;
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

        public static double SquareError(int reference, float value, double scalingCoefficient)
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
    }
}
