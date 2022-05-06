using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;
using System.Globalization;

float MSE_SCALING = 100;
int ITERATIONS = 125;
int MATERIAL_ALPHA = 1000;
int PHASE_ALPHA = 1000;
int MATERIAL_BATCH = 100;
int PHASE_BATCH = 5;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v9 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

List<Data> data = LoadData("data/quiet-labeled.epd");
Console.WriteLine();

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
//float[] cMaterial = MaterialTuner.GetLeorikCoefficients();

float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cMaterial = MaterialTuner.GetUntrainedCoefficients();

Console.WriteLine($"Preparing TuningData for {data.Count} positions");
List<TuningData> tuningData = new(data.Count);
foreach (Data entry in data)
{
    var td = Tuner.GetTuningData(entry, cPhase, cMaterial);
    tuningData.Add(td);
}
Tuner.ValidateConsistency(tuningData, cPhase, cMaterial);
Console.WriteLine();

TestMaterialMSE(cMaterial);
TestPhaseMSE(cPhase);

long t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TunePhaseBatch(PHASE_BATCH, PHASE_ALPHA);
    TuneMaterialBatch(MATERIAL_BATCH, MATERIAL_ALPHA);
    Tuner.ValidateConsistency(tuningData, cPhase, cMaterial);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
Console.ReadKey();
PrintMaterialCoefficients(cMaterial);
PrintPhaseCoefficients(cPhase);

/*
* 
* FUNCTIONS 
* 
*/

void TuneMaterialBatch(int batchSize, float alpha)
{
    double msePre = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={alpha,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        MaterialTuner.MinimizeParallel(tuningData, cMaterial, MSE_SCALING, alpha);
    }
    Tuner.SyncMaterialChanges(tuningData, cMaterial);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");
}

void TunePhaseBatch(int batchSize, float alpha)
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={alpha,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, alpha);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s ");
    PrintPhaseCoefficients(cPhase);
}

void PrintPhaseCoefficients(float[] c)
{
    float R(int i) => (int)Math.Round(c[i]);
    Console.WriteLine($"N:{R(0),4} B:{R(1),4} R:{R(2),4} Q:{R(3),4}");
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
    Console.WriteLine($"MSE(cMaterial) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cPhase) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
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

void PrintMaterialCoefficients(float[] coefficients)
{
    //MIDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128, 2, coefficients);
    //ENDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, 2, coefficients);
}

void WriteTable(int offset, int step, float[] coefficients)
{
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            float c = coefficients[offset + 8 * i * step + j * step];
            Console.Write($"{(int)Math.Round(c),5},");
        }
        Console.WriteLine();
    }
    Console.WriteLine();
}
