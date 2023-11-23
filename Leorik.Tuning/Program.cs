using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

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

    "leorik241pext_selfplay_0_varied_RND50-0-5s_100ms.pgn",
    "leorik241pext_selfplay_1_varied_RND50-0-5s_100ms.pgn",
    "leorik241pext_selfplay_2_varied_RND50-0-5s_100ms.pgn",
    "leorik241pext_selfplay_3_varied_RND50-0-5s_100ms.pgn",

    "leorik241pext_selfplay_0_varied_RND50--50-5s_100ms.pgn",
    "leorik241pext_selfplay_1_varied_RND50--50-5s_100ms.pgn",
    "leorik241pext_selfplay_2_varied_RND50--50-5s_100ms.pgn",

    "leorik243bpext_selfplay_0_varied_RND50--50-5s_100ms.pgn",
    "leorik243bpext_selfplay_1_varied_RND50--50-5s_100ms.pgn",
    "leorik243bpext_selfplay_2_varied_RND50--50-5s_100ms.pgn",
    "leorik243bpext_selfplay_3_varied_RND50--50-5s_100ms.pgn",
};

string DATA_PATH = "D:/Projekte/Chess/Leorik/TD2/";
string EPD_FILE = "DATA-L26-all.epd";
string BIN_FILE_PATH = "C:/Lager/d7-v3-50M.bin";
string BOOK_FILE_PATH = "D:/Projekte/Chess/Leorik/TD2/lichess-big3-resolved.book";

int FEN_PER_GAME = 10;
int SKIP_OUTLIERS = 200;
int MAX_Q_DEPTH = 10;

float MSE_SCALING = 100;
int ITERATIONS = 500;

int MATERIAL_ALPHA = 33;
int MATERIAL_BATCHES = 1500;
int PHASE_ALPHA = 20;
int PHASE_BATCHES = 500;

int MINI_BATCH_SIZE = 10_000;

DataGen.RunPrompt();

Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v28 ");
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
Console.WriteLine();
Console.WriteLine($"PHASE_ALPHA = {PHASE_ALPHA}");
Console.WriteLine($"PHASE_BATCHES = {PHASE_BATCHES}");
Console.WriteLine();
//BitboardUtils.Repl();
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
//ExtractPositions(PGN_FILES, EPD_FILE);
List<Data> dataSource = new List<Data>();
long t0 = Stopwatch.GetTimestamp();
DataUtils.LoadData(dataSource, DATA_PATH + EPD_FILE);
//DataUtils.LoadBinaryData(dataSource, BIN_FILE_PATH, 3_000_000);
//DataUtils.LoadWdlData(dataSource, BOOK_FILE_PATH);
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
//DataUtils.LoadBinary(data, "C:/Lager/net010_standard_shuffled_20m.book");
//DataUtils.CollectMetrics(data);
//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

//float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
//float[] cFeatures = FeatureTuner.GetLeorikCoefficients();
float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();
//RebalanceCoefficients(cFeatures);
//PrintCoefficients(cFeatures, cPhase);

TuningData[] tuningData = new TuningData[dataSource.Count];
TuningData[] miniBatch = new TuningData[MINI_BATCH_SIZE];
CreateTrainingData();
//ValidateLeorikEval(10);

MobilityTuner.AnalyzeTuningData(tuningData, FeatureTuner.FeatureWeights);

t0 = Stopwatch.GetTimestamp();
double bestMse = double.MaxValue;
float[] cBestFeatures = new float[cFeatures.Length];
for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    double mse = TuneMaterialMicroBatches();
    if (mse < bestMse)
    {
        RebalanceCoefficients(cFeatures);
        TunePhaseMicroBatches();
        Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
        Array.Copy(cFeatures, cBestFeatures, cFeatures.Length);
        bestMse = FeatureTuner.MeanSquareError(tuningData, cBestFeatures, MSE_SCALING);
        Console.WriteLine($"  After rebalancing: {bestMse}");
    }
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

PrintCoefficients(cBestFeatures, cPhase);
double finalMse = FeatureTuner.MeanSquareError(tuningData, cBestFeatures, MSE_SCALING);
Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {finalMse}");
Console.ReadKey();

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

void ExtractPositions(string[] pgnFileNames, string epdFileName)
{
    Console.WriteLine($"Extracting {FEN_PER_GAME} positions per game into {epdFileName}.");
    Console.WriteLine($"All positions that disagree by >{SKIP_OUTLIERS}cp with the previous eval...");
    Console.WriteLine();
    var output = File.CreateText(DATA_PATH + epdFileName);
    foreach (string pgnFile in pgnFileNames)
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

void CreateTrainingData()
{
    long t0 = Stopwatch.GetTimestamp();

    int[] indices = new int[dataSource.Count];
    for (int i = 0; i < dataSource.Count; i++)
        indices[i] = i;

    Tuner.Shuffle(indices);
    
    for (int i = 0; i < indices.Length; i++)
        tuningData[i] = Tuner.GetTuningData(dataSource[indices[i]], cPhase, cFeatures);
    
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Creating tuningData took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
}

double TuneMaterialMicroBatches()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, miniBatch);
        FeatureTuner.MinimizeParallel(miniBatch, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Console.Write('.');
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    Console.Write('.');
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"  Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
    return msePost;
}

double TunePhaseMicroBatches()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"  Phase    MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, miniBatch);
        PhaseTuner.MinimizeParallel(miniBatch, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Console.Write('.');
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    Console.Write('.');
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    Console.Write('.');
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($" Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s ");
    PhaseTuner.Report(cPhase);
    return msePost;
}

void ValidateLeorikEval(float errorThreshold)
{
    //the idea is that with identical coefficients and proper implementation the tuner should evaluate
    //positions not significantly different than the engine.
    float accError = 0;
    float maxError = 0;
    for (int i = 0; i < tuningData.Length; i++)
    {
        TuningData entry = tuningData[i];
        float eval = FeatureTuner.Evaluate(entry, cFeatures);
        float eval2 = entry.Position.Eval.RawScore;
        float error = Math.Abs(eval - eval2);
        accError += error;
        maxError = Math.Max(error, maxError);

        if (Math.Abs(error) > errorThreshold)
        {
            Console.WriteLine(Notation.GetFen(entry.Position));
            Console.WriteLine($"Phase: {entry.Phase} vs {entry.Position.Eval.Phase}");
            Console.WriteLine($"{i}: {eval} vs {eval2} Delta: {error}");
        }
    }
    Console.WriteLine($"Difference between Tuner's and Leorik's eval: {accError / tuningData.Length} avg, {maxError} max");
}

void TestLeorikMSE()
{
    double mse = Tuner.MeanSquareError(dataSource, MSE_SCALING);
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine();
}

void TestMaterialMSE(float[] coefficients)
{
    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    Console.WriteLine($"MSE(cPhase) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine();
}

void RebalanceCoefficients(float[] featureWeights)
{
    //Both the square-feature of a piece and the mobility-feature of a piece can encode material.
    //...but if mobility isn't updated in Qsearch for performance reasons it should all go into the square-features
    int[] buckets = MobilityTuner.GetFeatureDistribution(tuningData, FeatureTuner.FeatureWeights);
    //Tuner.Rebalance(Piece.Knight, buckets, featureWeights);
    Tuner.Rebalance(Piece.Bishop, buckets, featureWeights);
    Tuner.Rebalance(Piece.Rook, buckets, featureWeights);
    Tuner.Rebalance(Piece.Queen, buckets, featureWeights);
    Tuner.Rebalance(Piece.King, buckets, featureWeights);
}

void PrintCoefficients(float[] featureWeights, float[] phaseWeights)
{
    Console.WriteLine("Features");
    var material = new string[] { "Pawns", "Knights", "Bishops", "Rooks", "Queens", "Kings" };
    for (int i = 0; i < material.Length; i++)
    {
        Console.WriteLine($"//{material[i]}");
        FeatureTuner.Report(i, featureWeights);
    }

    var pawns = new string[] { "Isolated Pawns", "Passed Pawns", "Protected Pawns", "Connected Pawns" };
    for (int i = 0; i < pawns.Length; i++)
    {
        Console.WriteLine($"//{pawns[i]}");
        FeatureTuner.Report(material.Length + i, featureWeights);
    }


    Console.WriteLine();
    Console.WriteLine("Mobility");
    MobilityTuner.Report(Piece.Pawn, FeatureTuner.FeatureWeights, featureWeights);
    MobilityTuner.Report(Piece.Knight, FeatureTuner.FeatureWeights, featureWeights);
    MobilityTuner.Report(Piece.Bishop, FeatureTuner.FeatureWeights, featureWeights);
    MobilityTuner.Report(Piece.Rook, FeatureTuner.FeatureWeights, featureWeights);
    MobilityTuner.Report(Piece.Queen, FeatureTuner.FeatureWeights, featureWeights);
    MobilityTuner.Report(Piece.King, FeatureTuner.FeatureWeights, featureWeights);
    Console.WriteLine();

    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(phaseWeights);
}