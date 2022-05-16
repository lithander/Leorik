using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;
using System.Globalization;

float MSE_SCALING = 100;
int ITERATIONS = 200;
int MATERIAL_ALPHA = 700;
int PHASE_ALPHA = 1000;
int MATERIAL_BATCH = 50;
int PHASE_BATCH = 5;


//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v10 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();

ReplPawnStructure();

List<Data> data = LoadData("data/quiet-labeled.epd");
Console.WriteLine();

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
//float[] cFeatures = FeatureTuner.GetLeorikCoefficients();

float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();

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

t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TunePhaseBatch(PHASE_BATCH, PHASE_ALPHA);
    TuneMaterialBatch(MATERIAL_BATCH, MATERIAL_ALPHA);
    Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

PrintMaterialCoefficients(cFeatures);
PrintPhaseCoefficients(cPhase);

Console.ReadKey();
/*
* 
* FUNCTIONS 
* 
*/

void TuneMaterialBatch(int batchSize, float alpha)
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={alpha,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < batchSize; i++)
    {
        FeatureTuner.MinimizeParallel(tuningData, cFeatures, MSE_SCALING, alpha);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
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
    Console.WriteLine("MIDGAME");
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128, 2, coefficients);
    
    Console.WriteLine("ENDGAME");
    for (int i = 0; i < 6; i++)
        WriteTable(i * 128 + 1, 2, coefficients);

    Console.WriteLine("ConnectedOrProtected - MG");
    WriteTable(6 * 128, 2, coefficients);
    Console.WriteLine("ConnectedOrProtected - EG");
    WriteTable(6 * 128 + 1, 2, coefficients);

    Console.WriteLine("DoubledPawns - MG");
    WriteTable(7 * 128, 2, coefficients);
    Console.WriteLine("DoubledPawns - EG");
    WriteTable(7 * 128 + 1, 2, coefficients);

    Console.WriteLine("PassedPawns - MG");
    WriteTable(8 * 128, 2, coefficients);
    Console.WriteLine("PassedPawns - EG");
    WriteTable(8 * 128 + 1, 2, coefficients);

    Console.WriteLine("IsolatedPawns - MG");
    WriteTable(9 * 128, 2, coefficients);
    Console.WriteLine("IsolatedPawns - EG");
    WriteTable(9 * 128 + 1, 2, coefficients);

    Console.WriteLine("ConnectedPassedPawns - MG");
    WriteTable(10 * 128, 2, coefficients);
    Console.WriteLine("ConnectedPassedPawns - EG");
    WriteTable(10 * 128 + 1, 2, coefficients);
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

void PrintBitboard(ulong bits)
{
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            int sq = (7-i) * 8 + j;
            bool bit = (bits & (1UL << sq)) != 0;
            Console.Write(bit ? "O " : "- ");
        }
        Console.WriteLine();
    }
}

void ReplPawnStructure()
{
    while (true)
    {
        string fen = Console.ReadLine();
        if (fen == "")
            break;
        //fen = "8/8/7p/1P2Pp1P/2Pp1PP1/8/8/8 w - - 0 1";
        BoardState board = Notation.GetBoardState(fen);
        Console.WriteLine("Black Pawns");
        PrintBitboard(board.Black & board.Pawns);
        //Console.WriteLine("Isolated Black Pawns");
        //PrintBitboard(PawnStructure.GetIsolatedPawns(board, Color.Black));
        //Console.WriteLine("Passed Black Pawns");
        //PrintBitboard(PawnStructure.GetPassedPawns(board, Color.Black));
        //Console.WriteLine("Doubled Black Pawns");
        //PrintBitboard(PawnStructure.GetDoubledPawns(board, Color.Black));
        //Console.WriteLine("Protected Black Pawns");
        //PrintBitboard(PawnStructure.GetProtectedPawns(board, Color.Black));
        Console.WriteLine("Connected Black Pawns");
        PrintBitboard(PawnStructure.GetConnectedPawns(board, Color.Black));
        
        Console.WriteLine("White Pawns");
        PrintBitboard(board.White & board.Pawns);
        //Console.WriteLine("Isolated White Pawns");
        //PrintBitboard(PawnStructure.GetIsolatedPawns(board, Color.White));
        //Console.WriteLine("Passed White Pawns");
        //PrintBitboard(PawnStructure.GetPassedPawns(board, Color.White));
        //Console.WriteLine("Doubled White Pawns");
        //PrintBitboard(PawnStructure.GetDoubledPawns(board, Color.White));
        //Console.WriteLine("Protected White Pawns");
        //PrintBitboard(PawnStructure.GetProtectedPawns(board, Color.White));
        Console.WriteLine("Connected Black Pawns");
        PrintBitboard(PawnStructure.GetConnectedPawns(board, Color.White));

        Console.WriteLine("Connected Passed Pawns");
        PrintBitboard(PawnStructure.GetConnectedPassedPawns(board));

    }
}
