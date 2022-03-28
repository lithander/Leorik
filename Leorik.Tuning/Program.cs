using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

double MSE_SCALING = 102; //found via 'tune_mse2' with PeSTO weights on the full quiet-labeled dataset
//https://www.desmos.com/calculator/k7qsivwcdc

Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v1 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

List<Data> data = LoadData("data/quiet-labeled.epd");
List<Data2> data2 = ConvertData(data);
Console.WriteLine();

TestMSE();

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
//mse = Tuner.MeanSquareError(data, MSE_SCALING);
//Console.WriteLine($"Leorik's MSE with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
//Console.WriteLine();

Console.WriteLine("Initializing Coefficientss with Leoriks PSTs");
float[] Cref = Tuner.GetLeorikCoefficients();
TestMSE2(Cref);

Console.WriteLine("Initializing Coefficientss with material values");
float[] C = Tuner.GetMaterialCoefficients();
TestMSE2(C);
TestMSE2(C);
TestMSE2(C);
TestMSE2(C);
//float[] C = Tuner.GetLeorikCoefficients();
//PrintCoefficients(C);

for (int j = 1; j < 200; j++)
{
    float alpha = 2000;
    Console.Write($"Iteration #{j}, alpha = {alpha} ");
    MinimizeBatch(C, alpha);
}

//PrintCoefficientsDelta(C, C2);
TestMSE2(C);
PrintCoefficients(C);
//for (int i = 0; i < C.Length; i++)
//    Console.WriteLine($"{C[i]} -> {C2[i]} Delta:{C[i] - C2[i]}");

void MinimizeBatch(float[] coefficients, float alpha)
{
    long t0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < 10; i++)
    {
        Console.Write(".");
        Tuner.MinimizeSIMD(data2, coefficients, MSE_SCALING, alpha);
    }
    double mse = Tuner.MeanSquareError(data2, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($" MSE={mse} - {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
}


void TestMSE()
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestMSE2(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data2, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(data2, C) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

/*
 * 
 * FUNCTIONS 
 * 
 */

List<Data2> ConvertData(List<Data> data)
{
    List<Data2> result = new List<Data2>(data.Count);
    Console.WriteLine($"Converting {data.Count} DATA entries from Position to Feature vector'");

    foreach (Data entry in data)
    {
        var eval = new Evaluation(entry.Position);
        double phase = Math.Clamp((double)(eval.Phase - Evaluation.Midgame) / (Evaluation.Endgame - Evaluation.Midgame), 0, 1);
        float[] features = Tuner.GetFeatures(entry.Position, phase);
        short[] indices = Tuner.IndexBuffer(features);
        result.Add(new Data2
        {
            Features = features,
            Indices = indices,
            Result = entry.Result
        });
    }
    Console.WriteLine($"{result.Count} labeled positions converted!");
    return result;
}


List<Data> LoadData(string epdFile)
{
    List<Data> data = new List<Data>();
    Console.WriteLine($"Loading DATA from '{epdFile}'");
    var file = File.OpenText(epdFile);
    while (!file.EndOfStream)
        data.Add(ParseEntry(file.ReadLine()));

    Console.WriteLine($"{data.Count} labeled positions loaded!");
    return data;
}

Data ParseEntry(string line)
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

void PrintCoefficientsDelta(float[] c0, float[] c1)
{
    float[] c = new float[c0.Length];
    for (int i = 0; i < c0.Length; i++)
        c[i] = c1[i] - c0[i];

    //MIDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128, c);
    //ENDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, c);
}

void PrintCoefficients(float[] coefficients)
{
    //MIDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128, coefficients);
    //ENDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, coefficients);
}

void WriteTable(int offset, float[] coefficients)
{
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            float c = coefficients[offset + i * 16 + j * 2];
            Console.Write($"{(int)Math.Round(c),5},");
        }
        Console.WriteLine();
    }
    Console.WriteLine();
}
