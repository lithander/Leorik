using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

double MSE_SCALING = 100;
float ALPHA = 1000; //learning rate
int EPOCHS = 20000;
int BATCH = 20;

//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v3 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

List<Data> data = LoadData("data/quiet-labeled.epd");
Console.WriteLine();
Console.WriteLine($"Converting {data.Count} DATA entries...");
List<MaterialTuningData> materialData = new(data.Count);
List<PhaseTuningData> phaseData = new(data.Count);
foreach (Data entry in data)
{
    materialData.Add(MaterialTuner.GetTuningData(entry.Position, entry.Result));
    phaseData.Add(PhaseTuner.GetTuningData(entry.Position, entry.Result));
}
Console.WriteLine($"{data.Count} labeled positions converted!");
Console.WriteLine();

TestLeorikMSE();
//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
//TestLeorikMSE();

Console.WriteLine("Initializing Coefficients with Leoriks PSTs");
float[] cm_leorik = MaterialTuner.GetLeorikCoefficients(); 
TestMaterialMSE(cm_leorik);

float[] cp_leorik = PhaseTuner.GetLeorikPhaseCoefficients();
Console.WriteLine("Before:");
TestPhaseMSE(cp_leorik);
PrintPhaseCoefficients(cp_leorik);
cp_leorik[0] = 1000;
cp_leorik[1] = 1000;
cp_leorik[2] = 1000;
cp_leorik[3] = 1000;
Console.WriteLine("After:");
PrintPhaseCoefficients(cp_leorik);
TestPhaseMSE(cp_leorik);
double best = float.MaxValue;
int max = 500;
for (int j = 1; j < max; j++)
{
    float alpha = 2000;
    PhaseTuner.Minimize(phaseData, cp_leorik, MSE_SCALING, alpha);
    double mse = PhaseTuner.MeanSquareError(phaseData, cp_leorik, MSE_SCALING);
    Console.WriteLine($"Iteration {j}/{max}, alpha = {alpha}, MSE(phase)  = {mse}");
    if (mse > best)
        break;
    else
        best = mse;
}
PrintPhaseCoefficients(cp_leorik);

Console.ReadKey();

Console.WriteLine("Initializing Coefficients with material values");
float[] C = MaterialTuner.GetMaterialCoefficients();
TestMaterialMSE(C);

long t0 = Stopwatch.GetTimestamp();
for (int j = 1; j < EPOCHS/BATCH; j++)
{
    Console.Write($"Iteration {j}/{EPOCHS / BATCH}, alpha = {ALPHA} ");
    MinimizeBatch(C, ALPHA, BATCH);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning {EPOCHS} epochs took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

//PrintCoefficientsDelta(Cref, C);
TestMaterialMSE(C);
PrintCoefficients(C);
Console.ReadKey();

/*
 * 
 * FUNCTIONS 
 * 
 */

void PrintPhaseCoefficients(float[] c)
{
    float R(int i) => (int)Math.Round(c[i]);
    Console.WriteLine($"N:{R(0),5} B:{R(1),5} R:{R(2),5} Q:{R(3),5} -> [{R(4),5}..{R(5),5}]");
}

void MinimizeBatch(float[] coefficients, float alpha, int batchSize)
{
    long t0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        Console.Write(".");
        MaterialTuner.MinimizeParallel(materialData, coefficients, MSE_SCALING, alpha);
    }
    double mse = MaterialTuner.MeanSquareError(materialData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($" MSE={mse} - {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
}

void TestLeorikMSE()
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestMaterialMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = MaterialTuner.MeanSquareError(materialData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(Material, C) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(phaseData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(Phase, C) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
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
