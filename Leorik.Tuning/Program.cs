using Leorik.Core;
using Leorik.Tuning;
using System;
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

string[] BIN_PLAYOUT_FILES = {
    "2023-11-29T18.54.20_2147483K_D12_12RM_v3.playout.bin",
    "2023-11-28T19.21.07_50K_D50_12RM_v3.playout.bin",
    "2023-11-27T13.39.16_50K_D50_14RM_v3.playout.bin",
    "2023-11-27T11.39.22_50K_D12_14RM_v3.playout.bin",
    "2023-11-26T12.05.04_50K_D20_14RM_v3.playout.bin",
    "2023-12-03T12.25.18_2147483K_D9_15RM_v3.playout.bin",
    "2023-12-02T21.06.33_2147483K_D10_15RM_v3.playout.bin",
    "2023-12-03T17.28.35_100K_D12_14RM_v3.playout.bin",
    "2023-12-04T18.50.32_2147483K_D9_15RM_v3.playout.bin",
    "2023-12-04T21.05.10_2147483K_D9_15RM_v3.playout.bin",
    "2023-12-05T00.56.48_50K_D50_15RM_v3.playout.bin",
    "2023-12-05T19.46.50_50K_D50_14RM_v3.playout.bin",
    "2023-12-05T19.03.48_50K_D50_14RM_v3.playout.bin",
    "2023-12-08T18.03.58_100K_D99_10RM_v3.playout.bin",
    "2023-12-10T02.14.13_50K_D99_13RM_v3.playout.bin",
    "2023-12-11T20.21.55_50K_D99_12RM_v3.playout.bin",
    "2023-12-13T19.00.28_50K_D99_16RM_v3.playout.bin",
    "2023-12-12T18.35.44_50K_D99_15RM_v3.playout.bin",
    "2023-12-18T14.00.02_50K_12D_6R_50T_v4.playout.bin",
    //"2023-12-19T19.00.48_50K_12D_6R_100T_v4.playout.bin",
    //"2023-12-20T11.38.23_50K_12D_6R_75T_v4.playout.bin",
    "2023-12-21T17.41.40_50K_12D_6R_33T_v4.playout.bin",
    "2023-12-22T18.52.22_50K_12D_8R_50T_v4.playout.bin",
    //"2023-12-23T14.38.40_50K_12D_8R_75T_v4.playout.bin",
    "2023-12-24T00.44.25_50K_12D_8R_33T_v4.playout.bin",
    "2023-12-24T13.23.52_50K_12D_8R_20T_v4.playout.bin",
    "2024-01-08T15.35.58_100K_9D_10R_0T_v4.playout.bin",
    "2024-01-09T01.23.27_100K_9D_9R_0T_v4.playout.bin"
};


const string NNET = "net001-128HL-DATA-L31-lowtemp.bin";

const string DATA_PATH = "D:/Projekte/Chess/Leorik/TD2/";
const string EPD_FILE = "DATA-L26-all.epd";
const string TD_FILE = "DATA-L31-090124-dense.wdl";
const string BIN_FILE_PATH = "C:/Lager/d7-v3-50M.bin";
const string BOOK_FILE_PATH = "D:/Projekte/Chess/Leorik/TD2/lichess-big3-resolved.book";

int FEN_PER_GAME = 10;
int SKIP_OUTLIERS = 200;
int MAX_Q_DEPTH = 10;

float MSE_SCALING = 100;
int ITERATIONS = 200;

int MATERIAL_ALPHA = 100;
int MATERIAL_BATCHES = 1000;
int PHASE_ALPHA = 100;
int PHASE_BATCHES = 1000;

int LARGE_BATCH_SIZE = 5_000_000;
int MINI_BATCH_SIZE = 10_000;

DataGen.RunPrompt();
return;

Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v31 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"NNET = {NNET}");
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
Network.InitDefaultNetwork(NNET);
//BitboardUtils.Repl();
//return;
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
//DoublePlayoutWriter.ValidatePlayout(DATA_PATH + "2023-11-23T10.39.08_100K_12RM_v1.playout");
//ExtractBinaryToBinary(BIN_PLAYOUT_FILES, TD_FILE);
List<Data> dataSource = new List<Data>();
long t0 = Stopwatch.GetTimestamp();
DataUtils.LoadData(dataSource, DATA_PATH + EPD_FILE);

//DataUtils.LoadData(dataSource, DATA_PATH + TD_FILE + ".epd");
DataUtils.LoadBinaryData(dataSource, DATA_PATH + TD_FILE + ".bin");
//DataUtils.LoadWdlData(dataSource, BOOK_FILE_PATH);
long t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");
//DataUtils.LoadBinary(data, "C:/Lager/net010_standard_shuffled_20m.book");
//DataUtils.CollectMetrics(data);
//MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
TestLeorikMSE();

float[] cPhase = PhaseTuner.GetUntrainedCoefficients();
float[] cFeatures = FeatureTuner.GetUntrainedCoefficients();
//RebalanceCoefficients(cFeatures);
//PrintCoefficients(cFeatures, cPhase);

TuningData[] _tuningData = new TuningData[LARGE_BATCH_SIZE];
TuningData[] miniBatch = new TuningData[MINI_BATCH_SIZE];
//ValidateLeorikEval(10);
Console.WriteLine($"Initializing tuning data...");
CreateTrainingData(_tuningData, 1f);
Console.WriteLine();

t0 = Stopwatch.GetTimestamp();
double bestMse = double.MaxValue;
float[] cBestFeatures = new float[cFeatures.Length];
float[] cBestPhase = new float[cPhase.Length];

for (int it = 0; it < ITERATIONS; it++)
{
    Console.WriteLine($"{it}/{ITERATIONS} ");
    CreateTrainingData(_tuningData, 0.1f);
    double mse = TuneMicroBatches(_tuningData);
    if (mse < bestMse)
    {
        Array.Copy(cFeatures, cBestFeatures, cFeatures.Length);
        Array.Copy(cPhase, cBestPhase, cPhase.Length);
        Console.WriteLine($"  New best MSE={mse} Delta={mse - bestMse}");
        bestMse = mse;
    }
}
t1 = Stopwatch.GetTimestamp();
Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

PrintCoefficients(cBestFeatures, cBestPhase);
Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {bestMse}");
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

void ExtractPgnToEpd(string[] pgnFileNames, string epdFileName)
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
        (int games, int positions) = DataUtils.ExtractPgnToEpd(input, output, FEN_PER_GAME, SKIP_OUTLIERS, MAX_Q_DEPTH);
        long t_1 = Stopwatch.GetTimestamp();
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        Console.WriteLine();
        input.Close();
    }
    output.Close();
}

void ExtractBinaryToBinary(string[] inputFileNames, string fileName)
{
    Console.WriteLine($"Extracting quiet positions per game into {fileName}.");
    Console.WriteLine();
    var output = new BinaryWriter(new FileStream(DATA_PATH + fileName + ".bin", FileMode.Create));
    foreach (string inputFile in inputFileNames)
    {
        var input = File.OpenRead(DATA_PATH + inputFile);
        Console.WriteLine($"Reading {inputFile}");
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.ExtractBinaryToBinary(input, output, MAX_Q_DEPTH);
        long t_1 = Stopwatch.GetTimestamp();
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        Console.WriteLine();
        input.Close();
    }
    output.Close();
}

void ExtractBinaryToEpd(string[] inputFileNames, string fileName)
{
    Console.WriteLine($"Extracting quiet positions per game into {fileName}.");
    Console.WriteLine();
    var output = File.CreateText(DATA_PATH + fileName + ".epd");
    foreach (string inputFile in inputFileNames)
    {
        var input = File.OpenRead(DATA_PATH + inputFile);
        Console.WriteLine($"Reading {inputFile}");
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.ExtractBinaryToEpd(input, output, MAX_Q_DEPTH);
        long t_1 = Stopwatch.GetTimestamp();
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        Console.WriteLine();
        input.Close();
    }

    output.Close();
}

void CreateTrainingData(TuningData[] data, float ratio)
{
    long t0 = Stopwatch.GetTimestamp();

    int[] indices = new int[dataSource.Count];
    for (int i = 0; i < dataSource.Count; i++)
        indices[i] = i;

    Tuner.Shuffle(indices);

    Random rng = new Random();
    for (int i = 0; i < data.Length; i++)
        if(rng.NextDouble() < ratio)
            data[i] = Tuner.GetTuningData(dataSource[indices[i]], cPhase, cFeatures);
    
    long t1 = Stopwatch.GetTimestamp();

    double duration = Seconds(t1 - t0);
    double durationPerPosition = Seconds(1000000 * (t1 - t0) / (1 + data.Length));
    Console.WriteLine($"  Creating {(int)(data.Length*ratio)} ({(int)(ratio*100)}%) positions took {duration:0.###}s. ({durationPerPosition:0.#}µs/Position)");
}

double TuneMicroBatches(TuningData[] tuningData)
{
    CopyCoefficients(cFeatures, cPhase);
    double msePre = Tuner.MeanSquareError(dataSource, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} ");

    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, miniBatch);
        FeatureTuner.MinimizeParallel(miniBatch, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Console.Write('.');

    RebalanceCoefficients(_tuningData, cFeatures);
    Console.Write('.');

    Tuner.SyncFeaturesChanges(_tuningData, cFeatures);
    Console.Write('.');

    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, miniBatch);
        PhaseTuner.MinimizeParallel(miniBatch, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Console.Write('.');
    long t_1 = Stopwatch.GetTimestamp();

    Tuner.SyncPhaseChanges(_tuningData, cPhase);
    Console.Write('.');
    Tuner.SyncFeaturesChanges(_tuningData, cFeatures);
    Console.Write('.');
    Tuner.ValidateConsistency(_tuningData, cPhase, cFeatures);
    Console.Write('.');

    CopyCoefficients(cFeatures, cPhase);
    double msePost = Tuner.MeanSquareError(dataSource, MSE_SCALING);
    Console.WriteLine($" Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
    return msePost;
}

void ValidateLeorikEval(TuningData[] data, float errorThreshold)
{
    //the idea is that with identical coefficients and proper implementation the tuner should evaluate
    //positions not significantly different than the engine.
    float accError = 0;
    float maxError = 0;
    for (int i = 0; i < data.Length; i++)
    {
        TuningData entry = data[i];
        float eval = FeatureTuner.Evaluate(entry, cFeatures);
        var hce = new Evaluation(entry.Position);
        float eval2 = hce.RawScore;
        float error = Math.Abs(eval - eval2);
        accError += error;
        maxError = Math.Max(error, maxError);

        if (Math.Abs(error) > errorThreshold)
        {
            Console.WriteLine(Notation.GetFen(entry.Position));
            Console.WriteLine($"Phase: {entry.Phase} vs {hce.Phase}");
            Console.WriteLine($"{i}: {eval} vs {eval2} Delta: {error}");
        }
    }
    Console.WriteLine($"Difference between Tuner's and Leorik's eval: {accError / data.Length} avg, {maxError} max");
}

double TestLeorikMSE()
{
    double mse = Tuner.MeanSquareError(dataSource, MSE_SCALING);
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine();
    return mse;
}

void RebalanceCoefficients(TuningData[] data, float[] featureWeights)
{
    //Both the square-feature of a piece and the mobility-feature of a piece can encode material.
    //...but if mobility isn't updated in Qsearch for performance reasons it should all go into the square-features
    int[] buckets = MobilityTuner.GetFeatureDistribution(data, FeatureTuner.FeatureWeights);
    //Tuner.Rebalance(Piece.Knight, buckets, featureWeights);
    Tuner.Rebalance(Piece.Bishop, buckets, featureWeights);
    Tuner.Rebalance(Piece.Rook, buckets, featureWeights);
    Tuner.Rebalance(Piece.Queen, buckets, featureWeights);
    Tuner.Rebalance(Piece.King, buckets, featureWeights);
}

void CopyCoefficients(float[] featureWeights, float[] phaseWeights)
{
    FeatureTuner.CopyMaterial(featureWeights, Weights.MaterialWeights);
    FeatureTuner.CopyPawns(featureWeights, Weights.PawnWeights);
    MobilityTuner.CopyMobility(featureWeights, Weights.Mobility);
    PhaseTuner.CopyPhase(phaseWeights, Weights.PhaseValues);
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
        FeatureTuner.ReportMinimal(material.Length + i, featureWeights);
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