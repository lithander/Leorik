using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

float MSE_SCALING = 100;
int ITERATIONS = 200;
int PHASE_ITERATIONS = 115;
int MATERIAL_ALPHA = 500;
int FEATURE_ALPHA = 50;
int PHASE_ALPHA = 100;
int MATERIAL_BATCH = 100;
int PHASE_BATCH = 5;

string DATA_PATH = "D:/Projekte/Chess/Leorik/TD/";
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
    "leorik228gamma_vs_leorik228beta_startpos_RND30_100Hash_5s_200ms.pgn",
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
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_varied_RND40-0_100Hash_5s_200ms.pgn"

};
string EPD_FILE = "DATA-ZETA004.epd";
int FEN_PER_GAME = 15;
int SKIP_MARGIN = 5;
int SKIP_OUTLIERS = -1;

//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v21 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"SKIP_OUTLIERS = {SKIP_OUTLIERS}");
Console.WriteLine($"SKIP_MARGIN = {SKIP_MARGIN}");
Console.WriteLine($"FEN_PER_GAME = {FEN_PER_GAME}");
Console.WriteLine($"MSE_SCALING = {MSE_SCALING}");
Console.WriteLine($"ITERATIONS = {ITERATIONS}");
Console.WriteLine($"PHASE_ITERATIONS = {PHASE_ITERATIONS}");
Console.WriteLine($"MATERIAL_ALPHA = {MATERIAL_ALPHA}");
Console.WriteLine($"FEATURE_ALPHA = {FEATURE_ALPHA}");
Console.WriteLine($"PHASE_ALPHA = {PHASE_ALPHA}");
Console.WriteLine($"MATERIAL_BATCH = {MATERIAL_BATCH}");
Console.WriteLine($"PHASE_BATCH = {PHASE_BATCH}");
Console.WriteLine();

//BitboardUtils.Repl();
//PrepareData(FEN_PER_GAME, SKIP_MARGIN, SKIP_OUTLIERS);
List<Data> data = DataUtils.LoadData(DATA_PATH + EPD_FILE);

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
    if(it <= PHASE_ITERATIONS)
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

void PrepareData(int positionsPerGame, int skipMargin, int skipOutliers)
{
    Console.WriteLine($"Extracting {positionsPerGame} positions per game skipping the {skipMargin} first and last moves and all positions that disagree by >{skipOutliers} with the previous eval...");
    var output = File.CreateText(DATA_PATH + EPD_FILE);
    foreach (string pgnFile in PGN_FILES)
    {
        var input = File.OpenText(DATA_PATH + pgnFile);
        Console.WriteLine($"{pgnFile} -> {EPD_FILE}");
        DataUtils.ExtractData(input, output, positionsPerGame, skipMargin, skipOutliers);
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
