using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

string DATA_PATH = "D:/Projekte/Chess/Leorik/TD/";
string EPD_FILE = "DATA-THETA003-NoDraws-Filtered200-incTheta1-v2.epd";
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
    "leorik228gamma_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    "leorik228gamma_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    "leorik228delta_vs_leorik228gamma_startpos_RND30_100Hash_5s_200ms.pgn",
    "leorik228delta_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    "leorik228delta_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    "leorik228epsilon_vs_leorik228delta_startpos_RND30_100Hash_5s_200ms.pgn",
    "leorik228epsilon_vs_leorik228delta_startpos_RND35_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_startpos_RND50-10_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_one_with_book_startpos_RND50-10_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228eta_vs_zeta_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta_vs_zeta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta-1566807_vs_zeta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta-1560976_vs_zeta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta-1560976_vs_zeta_varied_RND100-10_100Hash_5s_200ms.pgn",//is actually played from startpos
    "leorik228theta-1234672_vs_eta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228theta-1234672_vs_eta_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND100-0_100Hash_5s_200ms.pgn"
};

int FEN_PER_GAME = 15;
int SKIP_OUTLIERS = 200;
int MAX_CAPTURES = 5;

float MSE_SCALING = 100;
int ITERATIONS = 120;
int MATERIAL_ALPHA = 1000;
int FEATURE_ALPHA = 100;
int PHASE_ALPHA = 200;
int MATERIAL_BATCH = 100;
int PHASE_BATCH = 10;

//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v23 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"FEN_PER_GAME = {FEN_PER_GAME}");
Console.WriteLine($"SKIP_OUTLIERS = {SKIP_OUTLIERS}");
Console.WriteLine($"MAX_CAPTURES = {MAX_CAPTURES}");
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
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
//PrepareData();
List<Data> data = DataUtils.LoadData(DATA_PATH + EPD_FILE);

//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
float[] cFeatures = FeatureTuner.GetLeorikCoefficients();
//float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
//float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();

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

RebalanceCoefficients(cFeatures);
PrintCoefficients(cFeatures, cPhase);

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

RebalanceCoefficients(cFeatures);
PrintCoefficients(cFeatures, cPhase);

double mse = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");

Console.ReadKey();
/*
* 
* FUNCTIONS 
* 
*/


void PgnToUci(string pgnFileName)
{
    Console.WriteLine($"Converting PGN to 'position startpos move ...' format..");
    var output = File.CreateText(DATA_PATH + pgnFileName + ".uci");
    var input = File.OpenText(DATA_PATH + pgnFileName);
    DataUtils.PgnToUci(input, output);
    input.Close();
}


void PrepareData()
{
    Console.WriteLine($"Extracting {FEN_PER_GAME} positions per game. All positions that disagree by >{SKIP_OUTLIERS}cp with the previous eval...");
    var output = File.CreateText(DATA_PATH + EPD_FILE);
    foreach (string pgnFile in PGN_FILES)
    {
        var input = File.OpenText(DATA_PATH + pgnFile);
        Console.WriteLine($"{pgnFile} -> {EPD_FILE}");
        DataUtils.ExtractData(input, output, FEN_PER_GAME, SKIP_OUTLIERS, MAX_CAPTURES);
        input.Close();
    }
    output.Close();
}

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

    Console.WriteLine("Features");
    for (int i = 0; i < featureTables; i++)
    {
        Console.WriteLine($"//{FeatureTuner.TableNames[i]}");
        FeatureTuner.Report(i, featureWeights);
    }

    Console.WriteLine();
    Console.WriteLine("Mobility");
    MobilityTuner.Report(Piece.Pawn, mobilityOffset, featureWeights);
    MobilityTuner.Report(Piece.Knight, mobilityOffset, featureWeights);
    MobilityTuner.Report(Piece.Bishop, mobilityOffset, featureWeights);
    MobilityTuner.Report(Piece.Rook, mobilityOffset, featureWeights);
    MobilityTuner.Report(Piece.Queen, mobilityOffset, featureWeights);
    MobilityTuner.Report(Piece.King, mobilityOffset, featureWeights);
    Console.WriteLine();

    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(phaseWeights);
}
