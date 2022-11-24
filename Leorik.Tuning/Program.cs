using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;
using System.Globalization;

float MSE_SCALING = 100;
int ITERATIONS = 50;
int MATERIAL_ALPHA = 1000;
int FEATURE_ALPHA = 50;
int PHASE_ALPHA = 250;
int MATERIAL_BATCH = 100;
int PHASE_BATCH = 10;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v18 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"MSE_SCALING = {MSE_SCALING}");
Console.WriteLine($"ITERATIONS = {ITERATIONS}");
Console.WriteLine($"MATERIAL_ALPHA = {MATERIAL_ALPHA}");
Console.WriteLine($"FEATURE_ALPHA = {FEATURE_ALPHA}");
Console.WriteLine($"PHASE_ALPHA = {PHASE_ALPHA}");
Console.WriteLine($"MATERIAL_BATCH = {MATERIAL_BATCH}");
Console.WriteLine($"PHASE_BATCH = {PHASE_BATCH}");
Console.WriteLine();

//BitboardUtils.Repl();
//DataUtils.ExtractData("data/parser_test.pgn", "data/DATA001.epd", 1000000);
//DataUtils.ExtractData("data/leorik2X3_selfplay_startpos_5s_200ms_50mb_16112020.pgn", "data/DATA001.epd", 2000000);


List<Data> data = DataUtils.LoadData("data/DATA001.epd");

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
//float[] cFeatures = FeatureTuner.GetLeorikCoefficients();

float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();

//PrintCoefficients(cFeatures);

Console.WriteLine($"Preparing TuningData for {data.Count} positions");
long t0 = Stopwatch.GetTimestamp();
List<TuningData> tuningData = new(data.Count);
foreach (Data entry in data)
{
    var td = Tuner.GetTuningData(entry, cPhase, cFeatures);
    tuningData.Add(td);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
Console.WriteLine();

TestMaterialMSE(cFeatures);
TestPhaseMSE(cPhase);
PhaseTuner.Report(cPhase);

t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TuneMaterialBatch();
    TunePhaseBatch();
    Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

PrintCoefficients(cFeatures);

double mse = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");

Console.ReadKey();
/*
* 
* FUNCTIONS 
* 
*/

void TuneMaterialBatch()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCH; i++)
    {
        FeatureTuner.MinimizeParallel(tuningData, cFeatures, MSE_SCALING, MATERIAL_ALPHA, FEATURE_ALPHA);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");
}

void TunePhaseBatch()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < PHASE_BATCH; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s ");
    PhaseTuner.Report(cPhase);
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
    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
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

void PrintCoefficients(float[] coefficients)
{
    int Tmax = FeatureTuner.MaterialTables + FeatureTuner.PawnStructureTables;

    Console.WriteLine("MIDGAME");
    for (int i = 0; i < Tmax; i++)
        WriteTable(i, false, coefficients);

    Console.WriteLine();
    Console.WriteLine("ENDGAME");
    for (int i = 0; i < Tmax; i++)
        WriteTable(i, true, coefficients);

    Console.WriteLine();
    Console.WriteLine("Mobility - MG");
    WriteMobilityTable(Tmax, false, coefficients);

    Console.WriteLine();
    Console.WriteLine("Mobility - EG");
    WriteMobilityTable(Tmax, true, coefficients);

    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(cPhase);
}

void WriteMobilityTable(int table, bool endgame, float[] coefficients)
{
    MobilityTuner.Report(Piece.Pawn, table, endgame, coefficients);
    MobilityTuner.Report(Piece.Knight, table, endgame, coefficients);
    MobilityTuner.Report(Piece.Bishop, table, endgame, coefficients);
    MobilityTuner.Report(Piece.Rook, table, endgame, coefficients);
    MobilityTuner.Report(Piece.Queen, table, endgame, coefficients);
    MobilityTuner.Report(Piece.King, table, endgame, coefficients);
    Console.WriteLine();
}

void WriteTable(int table, bool endgame, float[] coefficients)
{
    Console.WriteLine($"//{FeatureTuner.TableNames[table]}");
    const int step = 2;
    int offset = table * 128 + (endgame ? 1 : 0);
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            int k = offset + 8 * i * step + j * step;
            float c = (k < coefficients.Length) ? coefficients[k] : 0;
            Console.Write($"{(int)Math.Round(c),5},");
        }
        Console.WriteLine();
    }
}
