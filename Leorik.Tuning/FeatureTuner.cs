using static Leorik.Tuning.Tuner;
using Leorik.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Numerics;

namespace Leorik.Tuning
{
    static class FeatureTuner
    {
        //(Midgame + Endgame) * (6 Pieces + Isolated + Passed + Protected + Connected) * 64 = 1280 coefficients
        public const int MaterialTables = 6;
        public const int PawnStructureTables = 4;
        public const int FeatureTables = MaterialTables + PawnStructureTables;

        public const int Dimensions = 18;
        public const int MaterialWeights = Dimensions * MaterialTables * 64;
        public const int PawnStructureWeights = Dimensions * PawnStructureTables * 64;

        public const int FeatureWeights = MaterialWeights + PawnStructureWeights;

        public static float[] GetUntrainedCoefficients()
        {
            float[] c = AllocArray();

            int index = 0;
            for (int sq = 0; sq < 64; sq++, index += Dimensions)
                c[index] = 100; //Pawns
            for (int sq = 0; sq < 64; sq++, index += Dimensions)
                c[index] = 300; //Knights
            for (int sq = 0; sq < 64; sq++, index += Dimensions)
                c[index] = 300; //Bishops
            for (int sq = 0; sq < 64; sq++, index += Dimensions)
                c[index] = 500; //Rooks
            for (int sq = 0; sq < 64; sq++, index += Dimensions)
                c[index] = 900; //Queens

            return c;
        }

        public static float[] GetLeorikCoefficients()
        {
            float[] result = AllocArray();
            int index = 0;
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    index = Dimensions * (64 * piece + sq);
                    for(int i = 0; i < Dimensions; i++)
                        result[index + i] = Weights.MaterialWeights[index + i];
                }
            }

            for (int pawns = 0; pawns < 4; pawns++)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    index = Dimensions * (64 * (6 + pawns) + sq);
                    (short mg, short eg) = Weights.PawnWeights[64 * pawns + sq];
                    result[index] = mg;
                    result[index + 1] = eg;
                }
            }

            index = FeatureWeights;
            for (int i = 0; i < 88; i++)
            {
                (short mg, short eg) = Weights.Mobility[i];
                result[index++] = mg;
                result[index++] = eg;
            }

            return result;
        }

        private static void IteratePieces(float[] features, float phase, BoardState pos, ulong pieces, int table)
        {
            int blackKingSquare = Bitboard.LSB(pos.Black & pos.Kings);
            int whiteKingSquare = Bitboard.LSB(pos.White & pos.Kings) ^ 56;

            for (ulong bits = pieces & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                int index = Dimensions * (table * 64 + square);
                AddFeatures(features, index, -1, phase, blackKingSquare, whiteKingSquare);
            }

            for (ulong bits = pieces & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                int index = Dimensions * (table * 64 + square);
                AddFeatures(features, index, +1, phase, whiteKingSquare, blackKingSquare);
            }
        }

        private static void IteratePiecesMinimal(float[] features, float phase, BoardState pos, ulong pieces, int table)
        {
            for (ulong bits = pieces & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                int index = Dimensions * (table * 64 + square);
                features[index]--;
                features[index + 1] -= phase;
            }
            for (ulong bits = pieces & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits) ^ 56;
                int index = Dimensions * (table * 64 + square);
                features[index]++;
                features[index + 1] += phase;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddFeatures(float[] features, int index, int sign, float phase, int kingSqr, int oppKingSqr)
        {
            float kFile = Bitboard.File(kingSqr) / 3.5f - 1f;
            float kRank = Bitboard.Rank(kingSqr) / 3.5f - 1f;
            float oppKFile = Bitboard.File(oppKingSqr) / 3.5f - 1f;
            float oppKRank = Bitboard.Rank(oppKingSqr) / 3.5f - 1f;
            //Base
            features[index++] += sign;
            features[index++] += sign * kFile * kFile;
            features[index++] += sign * kFile;
            features[index++] += sign * kRank * kRank;
            features[index++] += sign * kRank;
            features[index++] += sign * oppKFile * oppKFile;
            features[index++] += sign * oppKFile;
            features[index++] += sign * oppKRank * oppKRank;
            features[index++] += sign * oppKRank;
            //Endgame (* phase)
            features[index++] += phase * sign;
            features[index++] += phase * sign * kFile * kFile;
            features[index++] += phase * sign * kFile;
            features[index++] += phase * sign * kRank * kRank;
            features[index++] += phase * sign * kRank;
            features[index++] += phase * sign * oppKFile * oppKFile;
            features[index++] += phase * sign * oppKFile;
            features[index++] += phase * sign * oppKRank * oppKRank;
            features[index++] += phase * sign * oppKRank;
        }

        public static void AddFeatures(float[] features, BoardState pos, float phase)
        {
            //phase is used to interpolate between endgame and midgame score but we want to incorporate it into the features vector
            //score = midgameScore + phase * endgameScore
            
            IteratePieces(features, phase, pos, pos.Pawns, 0);
            IteratePieces(features, phase, pos, pos.Knights, 1);
            IteratePieces(features, phase, pos, pos.Bishops, 2);
            IteratePieces(features, phase, pos, pos.Rooks, 3);
            IteratePieces(features, phase, pos, pos.Queens, 4);
            IteratePieces(features, phase, pos, pos.Kings, 5);

            //Pawn Structure
            IteratePiecesMinimal(features, phase, pos, Features.GetIsolatedPawns(pos), 6);
            IteratePiecesMinimal(features, phase, pos, Features.GetPassedPawns(pos), 7);
            IteratePiecesMinimal(features, phase, pos, Features.GetProtectedPawns(pos), 8);
            IteratePiecesMinimal(features, phase, pos, Features.GetConnectedPawns(pos), 9);
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


        internal static void ReportMinimal(int table, float[] coefficients)
        {
            int offset = Dimensions * table * 64;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int k = offset + Dimensions * (8 * i + j);
                    float mg = (int)Math.Round(coefficients[k]);
                    Console.Write($"({mg,4}");
                    float eg = (int)Math.Round(coefficients[k + 1]);
                    Console.Write($",{eg,4}), ");
                }
                Console.WriteLine();
            }
        }

        internal static void Report(int table, float[] coefficients)
        {
            for (int i = 0; i < 64; i++)
            {
                int k = Dimensions * (table * 64 + i);
                for (int d = 0; d < Dimensions; d++)
                {
                    float c = coefficients[k + d];
                    string cStr = c.ToString("F2", CultureInfo.InvariantCulture);
                    if (d == 0)
                        Console.Write($"{(int)Math.Round(c),4}, ");
                    else if (d < Dimensions - 1)
                        Console.Write($"{cStr,6}f, ");
                    else
                        Console.Write($"{cStr,6}f");
                }
                Console.WriteLine($", ");
            }
        }

        public static double MeanSquareError(TuningData[] data, float[] coefficients, float scalingCoefficient)
        {
            double squaredErrorSum = 0;
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry, coefficients);
                squaredErrorSum += SquareError(entry.Result, eval, scalingCoefficient);
            }
            double result = squaredErrorSum / data.Length;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate(TuningData entry, float[] coefficients)
        {
            return Tuner.Evaluate(entry.Features, coefficients);
        }

        public static void Minimize(TuningData[] data, float[] coefficients, float evalScaling, float alpha)
        {
            float[] accu = AllocArray();
            foreach (TuningData entry in data)
            {
                float eval = Evaluate(entry, coefficients);
                float error = Sigmoid(eval, evalScaling) - entry.Result;

                foreach (Feature f in entry.Features)
                    accu[f.Index] += error * f.Value;
            }

            for (int i = 0; i < AllWeigths; i++)
                coefficients[i] -= alpha * accu[i] / data.Length;
        }

        public static void MinimizeParallel(TuningData[] data, float[] coefficients, float evalScaling, float alpha)
        {
            //each thread maintains a local accu. After the loop is complete the accus are combined
            Parallel.ForEach(data,
                //initialize the local variable accu
                () => AllocArray(),
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
                        int N0 = FeatureWeights;
                        for (int i = 0; i < N0; i++)
                            coefficients[i] -= alpha * accu[i] / data.Length;

                        int N1 = N0 + MobilityTuner.MobilityWeights;
                        for (int i = N0; i < N1; i++)
                            coefficients[i] -= 0.25f * alpha * accu[i] / data.Length;
                    }
                }
            );
        }

        internal static void Rebalance(Piece piece, (int mg, int eg) delta, float[] coefficients)
        {
            int table = ((int)piece >> 2) - 1; //Pawn: 0, Knight: 1 ... King: 5
            int index = table * 128;
            for (int sq = 0; sq < 64; sq++)
            {
                coefficients[index++] += delta.mg;
                coefficients[index++] += delta.eg;
            }
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
