using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

float MSE_SCALING = 100;

int ITERATIONS = 50;

int MATERIAL_ALPHA = 1000; //learning rate
int PHASE_ALPHA = 1000;
int KS_ALPHA = 1000;

int MATERIAL_BATCH = 50;
int PHASE_BATCH = 5;
int KS_BATCH = 10;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v7 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

//ReplKingPhase();

List<Data> data = LoadData("data/quiet-labeled.epd");
Console.WriteLine();

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();
//DebugKingPhase();

float[] cMaterial = MaterialTuner.GetLeorikCoefficients();
//float[] cMaterial = MaterialTuner.GetUntrainedCoefficients();
float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
//float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cKingPhase = KingSafetyTuner.GetLeorikKingPhaseCoefficients();
float[] cKingSafety = KingSafetyTuner.GetLeorikKingSafetyCoefficients();


Console.WriteLine($"Preparing TuningData for {data.Count} positions");
List<TuningData> tuningData = new(data.Count);
foreach (Data entry in data)
{
    var td = Tuner.GetTuningData(entry, cPhase, cMaterial, cKingPhase);
    //PhaseTuner.Evaluation and MaterialTuner.Evaluation should agree!
    Debug.Assert(PhaseTuner.Evaluate(td, cPhase) - Tuner.Evaluate(td.MaterialFeatures, cMaterial) < 0.01);
    Debug.Assert(KingSafetyTuner.EvaluateKingSafety(entry.Position) - Tuner.Evaluate(td.KingSafetyFeatures, cKingSafety) < 0.01);
    tuningData.Add(td);
}
Console.WriteLine();


TestMaterialMSE(cMaterial);
TestPhaseMSE(cPhase);
TestKingSafetyMSE(cKingSafety);

long t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TuneKingSafetyBatch(KS_BATCH, KS_ALPHA);
    //TuneMaterialBatch(MATERIAL_BATCH, MATERIAL_ALPHA);
    //TunePhaseBatch(PHASE_BATCH, PHASE_ALPHA);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
Console.ReadKey();
PrintMaterialCoefficients(cMaterial);
PrintPhaseCoefficients(cPhase);
PrintKingSafetyCoefficients(cKingSafety);
/*
 * 
 * FUNCTIONS 
 * 
 */

void TuneKingSafetyBatch(int batchSize, float alpha)
{
    double msePre = KingSafetyTuner.MeanSquareError(tuningData, cKingSafety, MSE_SCALING);
    Console.WriteLine($"KingSafety MSE={msePre:N12} Alpha={alpha}");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        KingSafetyTuner.MinimizeParallel(tuningData, cKingSafety, MSE_SCALING, alpha);
    }
    //Tuner.SyncMaterialChanges(tuningData, cMaterial);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = KingSafetyTuner.MeanSquareError(tuningData, cKingSafety, MSE_SCALING);
    Console.WriteLine($"KingSafety MSE={msePost:N12} Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");
}

void TuneMaterialBatch(int batchSize, float alpha)
{
    double msePre = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.WriteLine($"  Material MSE={msePre:N12} Alpha={alpha}");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        MaterialTuner.MinimizeParallel(tuningData, cMaterial, MSE_SCALING, alpha);
    }
    Tuner.SyncMaterialChanges(tuningData, cMaterial);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = MaterialTuner.MeanSquareError(tuningData, cMaterial, MSE_SCALING);
    Console.WriteLine($"Material MSE={msePost:N12} Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s");
}

void TunePhaseBatch(int batchSize, float alpha)
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.WriteLine($"     Phase MSE={msePre:N12} Alpha={alpha}");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, alpha);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"   Phase MSE={msePost:N12} Delta={msePre - msePost:N10} Time={(t_1 - t_0) / (double)Stopwatch.Frequency:0.###}s ");
    PrintPhaseCoefficients(cPhase);
}

void DebugKingPhase()
{
    float[] cMaterial = MaterialTuner.GetLeorikCoefficients();
    float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
    float[] cKingPhase = KingSafetyTuner.GetLeorikKingPhaseCoefficients();
    float[] cKingSafety = KingSafetyTuner.GetLeorikKingSafetyCoefficients();

    foreach (Data entry in data)
    {
        var td = Tuner.GetTuningData(entry, cPhase, cMaterial, cKingPhase);
        float evalP = PhaseTuner.Evaluate(td, cPhase);
        float evalM = Tuner.Evaluate(td.MaterialFeatures, cMaterial);
        var errorPvsM = Math.Abs(evalP - evalM) < 0.001 ? 0 : evalP - evalM;

        if (errorPvsM != 0)
            Console.WriteLine($"Phase vs Material: {errorPvsM}");


        var evalRef = new Evaluation(entry.Position);
        float ks = KingSafetyTuner.EvaluateKingSafety(entry.Position);
        float evalKS = evalM + ks;
        var errorMvsRef = Math.Abs(evalKS - evalRef.Score) < 1 ? 0 : evalKS - evalRef.Score;
        
        if (errorMvsRef != 0)
            Console.WriteLine($"Tuner vs Leorik: {errorMvsRef} KS: {ks}");


        KingSafetyTuner.GetKingPhases(entry.Position, cKingPhase, out float wkPhase, out float bkPhase);
        float[] ksFeatures = KingSafetyTuner.GetFeatures(entry.Position, wkPhase, bkPhase);
        Feature[] features = Tuner.Condense(ksFeatures);
        float ks2 = Tuner.Evaluate(features, cKingSafety);

        var errorKsVsKs2 = Math.Abs(ks - ks2) < 0.01 ? 0 : ks - ks2;
        if (errorKsVsKs2 != 0)
            Console.WriteLine($"KS: {ks} vs KS2 {ks2}: {errorKsVsKs2}");

        Console.WriteLine(".");
    }
}

void ReplKingPhase()
{
    var eg = Evaluation.EndgameTables;
    for (int i = 0; i < 6 * 64; i++)
        eg[i] = 0;

    var mg = Evaluation.MidgameTables;
    int index = 0;
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 100; //Pawns         
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 300; //Knights        
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 300; //Bishops        
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 500; //Rooks          
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 900; //Queens
    for (int sq = 0; sq < 64; sq++, index++)
        mg[index] = 0; //King

    while (true)
    {
        string fen = Console.ReadLine();
        BoardState board = Notation.GetBoardState(fen);
        var eval = new Evaluation(board);
        Console.WriteLine(eval.Score);
    }
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

void TestKingSafetyMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = KingSafetyTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cKingSafety) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
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
        WriteTable(i * 128, 2, c);
    //ENDGAME PSTs
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, 2, c);
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

void PrintKingSafetyCoefficients(float[] coefficients)
{
    for (int i = 0; i < 6; i++)
        WriteTable(i * 64, 1, coefficients);
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
