﻿using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

string DATA_PATH = "D:/Projekte/Chess/Leorik/TD2/";
string EPD_FILE = "DATA-L24-all.epd";
string[] PGN_FILES = {
    //"leorik2X3_selfplay_startpos_5s_200ms_50mb_12112020.pgn",
    //"leorik2X3_selfplay_startpos_5s_200ms_50mb_16112020.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay_2.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay_3.pgn",
    //"leorik228alpha_selfplay_startpos_RND25_100Hash_5s_200ms.pgn",
    //"leorik228alpha_selfplay_startpos_RND25_100Hash_5s_200ms_2.pgn",
    //"leorik228beta_vs_leorik228alpha_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228beta_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_vs_leorik228beta_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_vs_leorik228gamma_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_vs_leorik228delta_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_vs_leorik228delta_startpos_RND35_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_selfplay_startpos_RND50-10_100Hash_5s_200ms.pgn",

    "leorik228epsilon_selfplay_startpos_RND50-10_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228eta_vs_zeta_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta_vs_zeta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228theta-1234672_vs_eta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228theta-1234672_vs_eta_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND100-0_100Hash_5s_200ms.pgn",

    "leorik24net8pext_selfplay_human_0_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_1_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_2_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_3_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_4_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_5_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_6_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_7_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_8_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_9_RND100--250-5s_100ms.pgn",

    "leorik24net8pext_selfplay_varied_1_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_2_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_3_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_4_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_5_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_6_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_7_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_8_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_9_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_10_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_11_RND120--300-5s_100ms.pgn",

    "leorik24net8pext_selfplay_0_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_1_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_2_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_3_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_4_titans_RND100--250-5s_100ms.pgn",

    "leorik24_selfplay_varied_1_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_2_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_3_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_4_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_5_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_6_RND100-0_5s_200ms.pgn",

    "leorik24net8pext_selfplay_startpos_0_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_1_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_2_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_3_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_4_RND100--250-5s_100ms.pgn",
};

int FEN_PER_GAME = 10;
int SKIP_OUTLIERS = 200;
int MAX_Q_DEPTH = 10;

float MSE_SCALING = 100;
int ITERATIONS = 20;

int MATERIAL_ALPHA = 25;
int MATERIAL_BATCHES = 2500;
int MATERIAL_BATCH_SIZE = 5000;

int PHASE_ALPHA = 10;
int PHASE_BATCHES = 500;
int PHASE_BATCH_SIZE = 5000;

//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v26 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"FEN_PER_GAME = {FEN_PER_GAME}");
Console.WriteLine($"SKIP_OUTLIERS = {SKIP_OUTLIERS}");
Console.WriteLine($"MAX_Q_DEPTH = {MAX_Q_DEPTH}");
Console.WriteLine();
Console.WriteLine($"MSE_SCALING = {MSE_SCALING}");
Console.WriteLine($"ITERATIONS = {ITERATIONS}");
Console.WriteLine();
Console.WriteLine($"MATERIAL_ALPHA = {MATERIAL_ALPHA}");
Console.WriteLine($"MATERIAL_BATCHES = {MATERIAL_BATCHES}");
Console.WriteLine($"MATERIAL_BATCH_SIZE = {MATERIAL_BATCH_SIZE}");
Console.WriteLine();
Console.WriteLine($"PHASE_ALPHA = {PHASE_ALPHA}");
Console.WriteLine($"PHASE_BATCHES = {PHASE_BATCHES}");
Console.WriteLine();

//BitboardUtils.Repl();
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
//ExtractPositions();
List<Data> data = DataUtils.LoadData(DATA_PATH + EPD_FILE);
DataUtils.CollectMetrics(data);
//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
//float[] cFeatures = FeatureTuner.GetLeorikCoefficients();
float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();
//PrintCoefficients(cFeatures, cPhase);

Console.WriteLine($"Preparing TuningData for {data.Count} positions");
long t0 = Stopwatch.GetTimestamp();
TuningData[] tuningData = new TuningData[data.Count];
int tdIndex = 0;
foreach (Data entry in data)
{
    tuningData[tdIndex++] = Tuner.GetTuningData(entry, cPhase, cFeatures);
}
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

Console.WriteLine($"Shuffling data...");
t0 = Stopwatch.GetTimestamp();
Tuner.Shuffle(tuningData);
Console.WriteLine($"...and aligning feature arrays in memory...");
Tuner.Localize(tuningData);
GC.Collect();
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
Console.WriteLine();

RebalanceCoefficients(cFeatures);
PrintCoefficients(cFeatures, cPhase);
TestPhaseMSE(cPhase);
TestMaterialMSE(cFeatures);
PhaseTuner.Report(cPhase);

t0 = Stopwatch.GetTimestamp();
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    TuneMaterialMicroBatches();
    TunePhaseMicroBatches();
    Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

RebalanceCoefficients(cFeatures);
PrintCoefficients(cFeatures, cPhase);

double mse = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");

WriteResults($"completely_random_{data.Count}", cFeatures, data);
Console.ReadKey();

void WriteResults(string fileName, float[] cFeatures, List<Data> data)
{
    string filePath;
    // Loop until a filename that doesn't exist is found
    int i = 0;
    do
        filePath = Path.Combine(DATA_PATH, $"{fileName}{i++}.txt");
    while (File.Exists(filePath));

    // Create the new file
    Console.WriteLine($"Writing results into {fileName}...");
    using (StreamWriter sw = File.CreateText(filePath))
    {
        sw.WriteLine(data.Count);
        foreach (var td in data)
        {
            sw.WriteLine(Notation.GetFen(td.Position));
        }
        sw.WriteLine("#Weights:");
        for(i = 0; i < cFeatures.Length; i += 2)
            sw.WriteLine($"{cFeatures[i]}, {cFeatures[i+1]}");
    }
    Console.WriteLine($"Done!");
}

/*
* 
* FUNCTIONS 
* 
*/

double Seconds(long ticks) => ticks / (double)Stopwatch.Frequency;

void PgnToUci(string pgnFileName)
{
    Console.WriteLine($"Converting PGN to 'position startpos move ...' format..");
    var output = File.CreateText(DATA_PATH + pgnFileName + ".uci");
    var input = File.OpenText(DATA_PATH + pgnFileName);
    DataUtils.PgnToUci(input, output);
    input.Close();
}

void ExtractPositions()
{
    Console.WriteLine($"Extracting {FEN_PER_GAME} positions per game into {EPD_FILE}.");
    Console.WriteLine($"All positions that disagree by >{SKIP_OUTLIERS}cp with the previous eval...");
    Console.WriteLine();
    var output = File.CreateText(DATA_PATH + EPD_FILE);
    foreach (string pgnFile in PGN_FILES)
    {
        var input = File.OpenText(DATA_PATH + pgnFile);
        Console.WriteLine($"Reading {pgnFile}");
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.ExtractData(input, output, FEN_PER_GAME, SKIP_OUTLIERS, MAX_Q_DEPTH);
        long t_1 = Stopwatch.GetTimestamp();
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        Console.WriteLine();
        input.Close();
    }
    output.Close();
}

void TuneMaterial()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        FeatureTuner.MinimizeParallel(tuningData, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
}

void TuneMaterialMicroBatches()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    TuningData[] batch = new TuningData[MATERIAL_BATCH_SIZE];
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, batch);
        FeatureTuner.MinimizeParallel(batch, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
}

void TunePhase()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s ");
    PhaseTuner.Report(cPhase);
}

void TunePhaseMicroBatches()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    TuningData[] batch = new TuningData[PHASE_BATCH_SIZE];
    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, batch);
        PhaseTuner.MinimizeParallel(batch, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s ");
    PhaseTuner.Report(cPhase);
}

void TestLeorikMSE()
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void TestMaterialMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cPhase) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void RebalanceCoefficients(float[] featureWeights)
{
    //Both the square-feature of a piece and the mobility-feature of a piece can encode material.
    //...but if mobility isn't updated in Qsearch for performance reasons it should all go into the square-features
    Console.WriteLine("Rebalancing...");
    Tuner.Rebalance(Piece.Knight, featureWeights);
    Tuner.Rebalance(Piece.Bishop, featureWeights);
    Tuner.Rebalance(Piece.Rook, featureWeights);
    Tuner.Rebalance(Piece.Queen, featureWeights);
    Tuner.Rebalance(Piece.King, featureWeights);
}

void PrintCoefficients(float[] featureWeights, float[] phaseWeights)
{
    int featureTables = FeatureTuner.MaterialTables + FeatureTuner.PawnStructureTables;
    int mobilityOffset = 128 * featureTables;

    Console.WriteLine("[White Features]");
    for (int i = 0; i < featureTables; i++)
    {
        Console.WriteLine($"//{FeatureTuner.TableNames[i]}");
        FeatureTuner.Report(i, 0, featureWeights);
    }
    Console.WriteLine();
    Console.WriteLine("[Black Features]");
    for (int i = 0; i < featureTables; i++)
    {
        Console.WriteLine($"//{FeatureTuner.TableNames[i]}");
        FeatureTuner.Report(i, FeatureTuner.AllWeights, featureWeights);
    }

    Console.WriteLine();
    Console.WriteLine("[White Mobility]");
    MobilityTuner.Report(mobilityOffset, featureWeights);
    Console.WriteLine();
    Console.WriteLine("[Black Mobility]");
    MobilityTuner.Report(FeatureTuner.AllWeights + mobilityOffset, featureWeights);

    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(phaseWeights);
}
