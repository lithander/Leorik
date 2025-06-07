using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

string[] BIN_PLAYOUT_FILES = {
    "2025-06-02T15.40.31_5K_DFRC_8R_v7",
    "2025-05-31T11.00.07_5K_DFRC_8R_v7",
    "2025-05-30T23.19.01_5K_DFRC_8R_v7",
    "2025-05-30T00.25.35_5K_DFRC_8R_v7",
    "2025-05-28T17.24.45_5K_3moves_FRC_5R_FRCv1",
    "2025-05-27T18.07.20_5K_3moves_FRC_5R_FRCv1",
    "2025-05-24T22.16.46_5K_3moves_FRC_5R_FRCv1",

    //"2025-05-22T18.18.29_5K_3moves_FRC_5R_50T50_v7",
    //"2025-05-21T17.39.23_5K_3moves_FRC_5R_50T50_v7",
    //"2025-05-19T23.31.54_5K_3moves_FRC_6R_50T50_v7",
    //"2025-05-15T15.37.54_5K_3moves_FRC_5R_v7",
    //"2025-05-14T15.30.05_5K_3moves_FRC_5R_v7",
    //"2025-05-14T01.19.08_5K_3moves_FRC_4R_50T50_v7",
    //"2025-05-14T00.58.22_5K_3moves_FRC_4R_99T50_v7",
    //"2025-03-24T15.40.25_5K_8R_v18",
    //"2025-03-22T12.24.12_5K_8R_v18",
    //"2025-03-20T18.25.05_5K_9R_v17",
    //"2025-03-19T17.52.21_5K_9R_v17",
    //"2025-03-18T18.04.27_5K_9R_v17",
    //"2025-03-17T19.44.40_5K_8R_v17",
    //"2025-03-16T14.25.10_5K_8R_v17",
    //"2025-03-14T18.20.08_5K_8R_v17",
    //"2025-03-13T18.24.56_5K_8R_v17",
    //"2025-03-12T15.38.54_5K_8R_v17",
    //"2025-03-07T16.38.00_5K_8R_v17",
    //"2025-03-06T15.36.30_5K_8R_v16",
    //"2025-02-28T17.54.38_5K_8R_v16-640HL",
    //"2025-02-27T18.20.49_5K_8R_v16-640HL",
    //"2025-02-26T23.40.22_5K_8R_v16-640HL",
    //"2025-02-26T15.40.47_5K_8R_v16-640HL",
    //"2025-03-01T21.24.51_5K_8R_v16-640HL",
    //"2025-03-05T15.11.26_5K_8R_v16-256HL",
    //"2025-03-04T19.26.36_5K_8R_v16-256HL",
    //"2025-02-24T15.42.06_5K_8R_v15",
    //"2025-02-24T15.42.06_5K_8R_v15"
    //"2025-02-20T15.40.09_5K_8R_v15",
    //"2025-02-17T20.19.11_5K_8R_v15",
    //"2025-02-16T19.31.04_5K_8R_v15",
    //"2025-02-16T01.31.25_5K_8R_v14",
    //"2025-02-14T17.59.40_5K_8R_v14",
    //"2025-02-13T18.15.45_5K_8R_v14",
    //"2025-02-12T15.19.52_5K_8R_v14",
    //"2025-02-10T15.39.09_5K_8R_v14",
    //"2025-02-09T23.58.00_5K_8R_v14",
    //"2025-02-07T10.17.21_5K_8R_v14",
    //"2025-02-05T13.32.13_5K_8R_v14",
    //"2025-01-27T15.18.51_5K_8R_v13",
    //"2025-01-28T15.38.41_5K_8R_v13",
    //"2025-01-26T10.54.40_5K_8R_v13",
    //"2025-01-25T12.46.26_5K_8R_v13",
    //"2025-01-24T10.48.15_5K_8R_v13",
    //"2025-01-20T19.24.20_5K_8R_v11",
    //"2025-01-20T11.22.34_5K_8R_v11",
    //"2025-01-15T15.33.46_5K_8R_v10",
    //"2025-01-14T23.31.13_5K_8R_v10",
    //"2025-01-14T15.34.57_5K_8R_v9",
    //"2025-01-14T11.11.28_5K_8R_v9",
    //"2025-01-13T10.20.06_5K_8R_v8",
    //"2025-01-13T15.33.57_5K_8R_v8",
    //"2025-03-25T15.39.35_5K_8R_T50_v18"
    //"2025-03-10T15.42.24_5K_8R_T50_v17",
    //"2025-03-09T23.19.58_5K_8R_T50_v17",
    //"2025-03-09T00.58.49_5K_8R_T50_v17",
    //"2025-03-03T18.06.06_5K_8R_T50_v16-256HL",
    //"2025-02-19T17.56.02_5K_8R_T50_v15",
    //"2025-02-11T15.36.01_5K_8R_T100_v14",
    //"2025-02-08T21.06.47_5K_8R_T100_v14",
    //"2025-02-06T14.53.09_5K_8R_T100_v14",
    //"2025-01-26T22.10.14_5K_8R_T100_v13"
    //"2025-01-22T14.46.15_5K_8R_T100_v13",
    //"2025-01-20T23.50.57_5K_8R_T100_v12",
    //"2025-01-19T19.53.07_5K_8R_T100_v12",
    //"2025-01-19T16.31.41_5K_8R_T100_v12",
    //"2025-01-17T14.58.40_5K_8R_T100_v11",
    //"2025-01-16T18.20.47_5K_8R_T100_v11",
    //"2025-01-13T20.21.30_5K_8R_T100_v9",
    //"2025-01-12T16.07.42_5K_8R_T100_v8",
    //"2025-01-12T04.03.08_5K_8R_T100_v7",
    //"2025-01-11T22.01.49_5K_8R_T100_v6",
    //"2025-01-11T18.20.21_5K_8R_T100_v5",
    //"2025-01-10T19.11.25_5K_8R_T100_v4",
    //"2025-01-09T19.13.27_5K_8R_T100_v3",
    //"2025-01-08T18.15.04_5K_8R_T100_v2",
    //"2025-01-06T21.13.24_5K_8R_T100_v0",
};

string[] SHUFFLE_FILES = {
    "2025-06-02T15.40.31_5K_DFRC_8R_FRCv1_Q5",
    "2025-05-31T11.00.07_5K_DFRC_8R_FRCv1_Q5",
    "2025-05-30T23.19.01_5K_DFRC_8R_FRCv1_Q5",
    "2025-05-30T00.25.35_5K_DFRC_8R_FRCv1_Q5",
    "2025-05-28T17.24.45_5K_3moves_FRC_5R_FRCv1_Q5",
    "2025-05-27T18.07.20_5K_3moves_FRC_5R_FRCv1_Q5",
    "2025-05-24T22.16.46_5K_3moves_FRC_5R_FRCv1_Q5",

    "2025-05-22T18.18.29_5K_3moves_FRC_5R_50T50_FRCv0_Q5",
    "2025-05-21T17.39.23_5K_3moves_FRC_5R_50T50_FRCv0_Q5",
    "2025-05-19T23.31.54_5K_3moves_FRC_6R_50T50_FRCv0_Q5",
    "2025-05-15T15.37.54_5K_3moves_FRC_5R_FRCv0_Q5",
    "2025-05-14T15.30.05_5K_3moves_FRC_5R_FRCv0_Q5",
    "2025-05-14T01.19.08_5K_3moves_FRC_4R_50T50_FRCv0_Q5",
    "2025-05-14T00.58.22_5K_3moves_FRC_4R_99T50_FRCv0_Q5",

    "2025-03-24T15.40.25_5K_8R_v18_Q5",
    "2025-03-22T12.24.12_5K_8R_v18_Q5",
    "2025-03-20T18.25.05_5K_9R_v17_Q5",
    "2025-03-19T17.52.21_5K_9R_v17_Q5",
    "2025-03-18T18.04.27_5K_9R_v17_Q5",
    "2025-03-17T19.44.40_5K_8R_v17_Q5",
    "2025-03-16T14.25.10_5K_8R_v17_Q5",
    "2025-03-14T18.20.08_5K_8R_v17_Q5",
    "2025-03-16T14.25.10_5K_8R_v17_Q5",
    "2025-03-18T18.04.27_5K_9R_v17_Q5",
    "2025-03-17T19.44.40_5K_8R_v17_Q5",
    "2025-03-13T18.24.56_5K_8R_v17_Q5",
    "2025-03-12T15.38.54_5K_8R_v17_Q5",
    "2025-03-07T16.38.00_5K_8R_v17_Q5",
    "2025-03-06T15.36.30_5K_8R_v16_Q5",
    "2025-02-28T17.54.38_5K_8R_v16-640HL_Q5",
    "2025-02-27T18.20.49_5K_8R_v16-640HL_Q5",
    "2025-02-26T23.40.22_5K_8R_v16-640HL_Q5",
    "2025-02-26T15.40.47_5K_8R_v16-640HL_Q5",
    "2025-03-01T21.24.51_5K_8R_v16-640HL_Q5",
    "2025-03-05T15.11.26_5K_8R_v16-256HL_Q5",
    "2025-03-04T19.26.36_5K_8R_v16-256HL_Q5",
    "2025-02-24T15.42.06_5K_8R_v15_Q5",
    "2025-02-24T15.42.06_5K_8R_v15_Q5",
    "2025-02-20T15.40.09_5K_8R_v15_Q5",
    "2025-02-17T20.19.11_5K_8R_v15_Q5",
    "2025-02-16T19.31.04_5K_8R_v15_Q5",
    "2025-02-16T01.31.25_5K_8R_v14_Q5",
    "2025-02-14T17.59.40_5K_8R_v14_Q5",
    "2025-02-13T18.15.45_5K_8R_v14_Q5",
    "2025-02-12T15.19.52_5K_8R_v14_Q5",
    "2025-02-10T15.39.09_5K_8R_v14_Q5",
    "2025-02-09T23.58.00_5K_8R_v14_Q5",
    "2025-02-07T10.17.21_5K_8R_v14_Q5",
    "2025-02-05T13.32.13_5K_8R_v14_Q5",
    "2025-01-27T15.18.51_5K_8R_v13_Q5",
    "2025-01-28T15.38.41_5K_8R_v13_Q5",
    "2025-01-26T10.54.40_5K_8R_v13_Q5",
    "2025-01-25T12.46.26_5K_8R_v13_Q5",
    "2025-01-24T10.48.15_5K_8R_v13_Q5",
    //"2025-01-20T19.24.20_5K_8R_v11_Q5",
    //"2025-01-20T11.22.34_5K_8R_v11_Q5",
    //"2025-01-15T15.33.46_5K_8R_v10_Q5",
    //"2025-01-14T23.31.13_5K_8R_v10_Q5",
    //"2025-01-14T15.34.57_5K_8R_v9_Q5",
    //"2025-01-14T11.11.28_5K_8R_v9_Q5",
    //"2025-01-13T10.20.06_5K_8R_v8_Q5",
    //"2025-01-13T15.33.57_5K_8R_v8_Q5",
    "2025-03-25T15.39.35_5K_8R_T50_v18_Q5",
    "2025-03-10T15.42.24_5K_8R_T50_v17_Q5",
    "2025-03-09T23.19.58_5K_8R_T50_v17_Q5",
    "2025-03-09T00.58.49_5K_8R_T50_v17_Q5",
    "2025-03-03T18.06.06_5K_8R_T50_v16-256HL_Q5",
    "2025-02-19T17.56.02_5K_8R_T50_v15_Q5",
    "2025-02-11T15.36.01_5K_8R_T100_v14_Q5",
    "2025-02-08T21.06.47_5K_8R_T100_v14_Q5",
    "2025-02-06T14.53.09_5K_8R_T100_v14_Q5",
    "2025-01-26T22.10.14_5K_8R_T100_v13_Q5",
    "2025-01-22T14.46.15_5K_8R_T100_v13_Q5",
    //"2025-01-20T23.50.57_5K_8R_T100_v12_Q5",
    //"2025-01-19T19.53.07_5K_8R_T100_v12_Q5",
    //"2025-01-19T16.31.41_5K_8R_T100_v12_Q5",
    //"2025-01-17T14.58.40_5K_8R_T100_v11_Q5",
    //"2025-01-16T18.20.47_5K_8R_T100_v11_Q5",
    //"2025-01-13T20.21.30_5K_8R_T100_v9_Q5",
    //"2025-01-12T16.07.42_5K_8R_T100_v8_Q5",
    //"2025-01-12T04.03.08_5K_8R_T100_v7_Q5",
    //"2025-01-11T22.01.49_5K_8R_T100_v6_Q5",
    //"2025-01-11T18.20.21_5K_8R_T100_v5_Q5",
    //"2025-01-10T19.11.25_5K_8R_T100_v4",
    //"2025-01-09T19.13.27_5K_8R_T100_v3",
    //"2025-01-08T18.15.04_5K_8R_T100_v2",
    //"2025-01-06T21.13.24_5K_8R_T100_v0",
};

string[] SHUFFLE_TARGETS = {
    "FiSh_5K_Q5_v13-FRCv1_01",
    "FiSh_5K_Q5_v13-FRCv1_02",
    "FiSh_5K_Q5_v13-FRCv1_03",
    "FiSh_5K_Q5_v13-FRCv1_04",
};

const string PLAYOUT_PATH = "E:/";//"D:/Projekte/Chess/Leorik/TD3/";
const string PLAYOUT_EXT = ".playout.bin";

const string TD_PATH = "F:/TD3/Filtered/";
const string TD_EXT = ".bullet.bin";

const string SHUFFLE_PATH = "F:/TD3/";
const string SHUFFLE_EXT = ".bullet.bin";


Filter FILTER_CONFIG = new()
{
    //all values are inclusive
    StartSkip = 10,
    MinSkip = 0,
    MaxSkip = 0,
    DrawScoreCap = 5000,
    WrongScoreCap = 5000, 
    MinPieces = 6,
    QSearchDepth = 5
};

Network.LoadDefaultNetwork();
//Network.InitEmptyNetwork(1);
//DataGen.RunPrompt();
//BitboardUtils.Repl();
//FilterPlayouts(BIN_PLAYOUT_FILES, "_Q5", FILTER_CONFIG);
//ShuffleSlices(SHUFFLE_FILES, SHUFFLE_TARGETS);
GetMetrics(SHUFFLE_PATH, SHUFFLE_TARGETS);
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
return;

void GetMetrics(string path, string[] inputFileNames)
{
    string[] inputFiles = new string[inputFileNames.Length];
    for (int i = 0; i < inputFileNames.Length; i++)
        inputFiles[i] = path + inputFileNames[i] + TD_EXT;

    DataUtils.GetMetrics(inputFiles);
}

void Shuffle(string[] inputFileNames, string outputFileName)
{
    string[] inputFiles = new string[inputFileNames.Length];
    for (int i = 0; i < inputFileNames.Length; i++)
        inputFiles[i] = TD_PATH + inputFileNames[i] + TD_EXT;

    DataUtils.Shuffle(inputFiles, TD_PATH + outputFileName);
}

void ShuffleSlices(string[] inputFileNames, string[] outputFileNames)
{
    string[] inputFiles = new string[inputFileNames.Length];
    for (int i = 0; i < inputFileNames.Length; i++)
        inputFiles[i] = TD_PATH + inputFileNames[i] + TD_EXT;

    string[] outputFiles = new string[outputFileNames.Length];
    for (int i = 0; i < outputFileNames.Length; i++)
        outputFiles[i] = SHUFFLE_PATH + outputFileNames[i] + SHUFFLE_EXT;

    for(int i = 0; i < outputFiles.Length; i++)
        DataUtils.Shuffle(inputFiles, outputFiles[i], i, outputFiles.Length);
}

void FilterPlayouts(string[] fileNames, string postFix, Filter filter)
{
    Console.WriteLine();
    long totalPositions = 0;
    foreach (string inputFile in fileNames)
    {
        Console.WriteLine($"Filtering {inputFile}...");
        var output = new BinaryWriter(new FileStream(TD_PATH + inputFile + postFix + TD_EXT, FileMode.Create));
        var input = File.OpenRead(PLAYOUT_PATH + inputFile + PLAYOUT_EXT);
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.FilterPlayouts(input, output, filter);
        long t_1 = Stopwatch.GetTimestamp();
        totalPositions += positions;
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        input.Close();
        output.Close();
    }
    Console.WriteLine($"Done! ({totalPositions/1000_000}M positions)");
}

void PgnToUci(string pgnFileName)
{
    Console.WriteLine($"Converting PGN to 'position startpos move ...' format..");
    var output = File.CreateText(PLAYOUT_PATH + pgnFileName + ".uci");
    var input = File.OpenText(PLAYOUT_PATH + pgnFileName);
    DataUtils.PgnToUci(input, output, Variant.Standard);
    input.Close();
}

const int MAX_Q_DEPTH = 10;
const int FEN_PER_GAME = 10;
const int SKIP_OUTLIERS = 200;

void ExtractPgnToEpd(string[] pgnFileNames, string epdFileName)
{
    Console.WriteLine($"Extracting {FEN_PER_GAME} positions per game into {epdFileName}.");
    Console.WriteLine($"All positions that disagree by >{SKIP_OUTLIERS}cp with the previous eval...");
    Console.WriteLine();
    var output = File.CreateText(PLAYOUT_PATH + epdFileName);
    foreach (string pgnFile in pgnFileNames)
    {
        var input = File.OpenText(PLAYOUT_PATH + pgnFile);
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

void ExtractBinaryToEpd(string[] inputFileNames, string fileName)
{
    Console.WriteLine($"Extracting quiet positions per game into {fileName}.");
    Console.WriteLine();
    var output = File.CreateText(PLAYOUT_PATH + fileName + ".epd");
    foreach (string inputFile in inputFileNames)
    {
        var input = File.OpenRead(PLAYOUT_PATH + inputFile);
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

double Seconds(long ticks) => ticks / (double)Stopwatch.Frequency;
