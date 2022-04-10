using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

double MSE_SCALING = 100;

int ITERATIONS = 500;

int MATERIAL_ALPHA = 1000; //learning rate
int PHASE_ALPHA = 1000;

int MATERIAL_BATCH = 50;
int PHASE_BATCH = 5;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v6 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

List<Data> data = LoadData("data/quiet-labeled.epd");
Console.WriteLine();

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cMaterial = MaterialTuner.GetLeorikCoefficients();
float[] cMaterial = MaterialTuner.GetUntrainedCoefficients();
//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
float[] cPhase = PhaseTuner.GetUntrainedCoefficients();

Console.WriteLine($"Preparing TuningData for {data.Count} positions");
List<TuningData> tuningData = new(data.Count);
foreach (Data entry in data)
{
    var td = Tuner.GetTuningData(entry, cPhase, cMaterial);
    //PhaseTuner.Evaluation and MaterialTuner.Evaluation should agree!
    Debug.Assert(Tuner.GetSyncError(td, cPhase, cMaterial) < 0.01);
    tuningData.Add(td);
}
Console.WriteLine();

TestMaterialMSE(cMaterial);
TestPhaseMSE(cPhase);

long t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");

    double msePre = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.WriteLine($"Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA}");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCH && MATERIAL_ALPHA > 0; i++)
    {
        MaterialTuner.MinimizeParallel(tuningData, cMaterial, MSE_SCALING, MATERIAL_ALPHA);
    }
    Tuner.SyncMaterialChanges(tuningData, cMaterial);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.WriteLine($"Material MSE={msePost:N12} Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");

    msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.WriteLine($"   Phase MSE={msePre:N12} Alpha={PHASE_ALPHA}");
    t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < PHASE_BATCH && PHASE_ALPHA > 0; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    t_1 = Stopwatch.GetTimestamp();
    msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"   Phase MSE={msePost:N12} Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s ");
    PrintPhaseCoefficients(cPhase);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
Console.ReadKey();
PrintCoefficients(cMaterial);
PrintPhaseCoefficients(cPhase);
/*
 * 
 * FUNCTIONS 
 * 
 */

void PrintPhaseCoefficients(float[] c)
{
    float R(int i) => (int)Math.Round(c[i]);
    Console.WriteLine($"N:{R(0),4} B:{R(1),4} R:{R(2),4} Q:{R(3),4}");
}

void MinimizeBatch(float[] coefficients, float alpha, int batchSize)
{
    long t0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        Console.Write(".");
        MaterialTuner.MinimizeParallel(tuningData, coefficients, MSE_SCALING, alpha);
    }
    double mse = MaterialTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
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
    double mse = MaterialTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(Material, C) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
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
        data.Add(Tuner.ParseEntry(file.ReadLine()));

    Console.WriteLine($"{data.Count} labeled positions loaded!");
    return data;
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
