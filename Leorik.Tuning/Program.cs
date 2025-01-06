using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

(string, int)[] BIN_PLAYOUT_FILES = {
    ("2023-11-29T18.54.20_2147483K_D12_12RM_v3.playout.bin", 0),
    ("2023-11-28T19.21.07_50K_D50_12RM_v3.playout.bin", 0),
    ("2023-11-27T13.39.16_50K_D50_14RM_v3.playout.bin", 0),
    ("2023-11-27T11.39.22_50K_D12_14RM_v3.playout.bin", 0),
    ("2023-11-26T12.05.04_50K_D20_14RM_v3.playout.bin", 0),
    //("2023-12-03T12.25.18_2147483K_D9_15RM_v3.playout.bin", 0),
    //("2023-12-02T21.06.33_2147483K_D10_15RM_v3.playout.bin", 0),
    ("2023-12-03T17.28.35_100K_D12_14RM_v3.playout.bin", 0),
    //("2023-12-04T18.50.32_2147483K_D9_15RM_v3.playout.bin", 0),
    //("2023-12-04T21.05.10_2147483K_D9_15RM_v3.playout.bin", 0),
    ("2023-12-05T00.56.48_50K_D50_15RM_v3.playout.bin", 0),
    ("2023-12-05T19.46.50_50K_D50_14RM_v3.playout.bin", 0),
    ("2023-12-05T19.03.48_50K_D50_14RM_v3.playout.bin", 0),
    ("2023-12-08T18.03.58_100K_D99_10RM_v3.playout.bin", 0),
    ("2023-12-10T02.14.13_50K_D99_13RM_v3.playout.bin", 0),
    ("2023-12-11T20.21.55_50K_D99_12RM_v3.playout.bin", 0),
    ("2023-12-13T19.00.28_50K_D99_16RM_v3.playout.bin", 0),
    ("2023-12-12T18.35.44_50K_D99_15RM_v3.playout.bin", 0),
    //("2023-12-18T14.00.02_50K_12D_6R_50T_v4.playout.bin", 0),
    //("2023-12-19T19.00.48_50K_12D_6R_100T_v4.playout.bin", 0),
    //("2023-12-20T11.38.23_50K_12D_6R_75T_v4.playout.bin", 0),
    ("2023-12-21T17.41.40_50K_12D_6R_33T_v4.playout.bin", 0),
    //("2023-12-22T18.52.22_50K_12D_8R_50T_v4.playout.bin", 0),
    //("2023-12-23T14.38.40_50K_12D_8R_75T_v4.playout.bin", 0),
    ("2023-12-24T00.44.25_50K_12D_8R_33T_v4.playout.bin", 0),
    ("2023-12-24T13.23.52_50K_12D_8R_20T_v4.playout.bin", 0),
    //("2024-01-08T15.35.58_100K_9D_10R_0T_v4.playout.bin", 0),
    //("2024-01-09T01.23.27_100K_9D_9R_0T_v4.playout.bin", 0),
    //("2024-01-09T21.23.53_100K_9D_11R_0T_v4.playout.bin", 0),
    //("2024-01-10T03.36.23_100K_9D_9R_30T_v4.playout.bin", 0),
    //("2024-01-10T18.50.45_100K_9D_8R_30T_v4.playout.bin", 0),
    ("2024-01-18T12.08.38_10K_8R_8T50_v5.playout.bin", 0),
    ("2024-01-18T08.47.14_10K_8R_6T0_v5.playout.bin", 0),
    ("2024-01-19T19.02.34_10K_7R_8T50_v5.playout.bin", 0),
    ("2024-01-19T20.19.18_10K_8R_6T0_v5.playout.bin", 0),
    ("2024-01-20T04.53.18_10K_9R_8T50_v5.playout.bin", 0),
    ("2024-01-20T04.50.48_10K_9R_6T0_v5.playout.bin", 0),
    ("2024-01-20T12.07.57_10K_7R_6T0_v5.playout.bin", 0),
    ("2024-01-20T16.54.29_10K_8R_8T50_v5.playout.bin", 0),
    ("2024-01-20T23.29.24_10K_6R_8T50_v5.playout.bin", 0),
    ("2024-01-21T01.31.44_50K_8R_6T0_v5.playout.bin", 0),
    //("2024-01-21T21.57.19_5K_9R_6T0_v5.playout.bin", 0),
    //("2024-01-22T14.57.39_5K_8R_6T0_v5.playout.bin", 0),
    //("2024-01-23T02.42.30_5K_7R_6T0_v5.playout.bin", 0),
    //("2024-01-23T19.09.09_5K_10R_6T0_v5.playout.bin", 0),
    //("2024-01-24T11.38.33_5K_11R_6T0_v5.playout.bin", 0),
    ("2024-01-26T18.59.35_20K_8R_6T50_v5.playout.bin", 0),
    ("2024-01-28T14.08.04_10K_8R_20T50_v5.playout.bin", 0)
};

const string DATA_PATH = "D:/Projekte/Chess/Leorik/TD2/";
const string TD_FILE = "D:/Projekte/Chess/Leorik/TD2/Data/DATA-Strict-0-0-5000-7.bullet.bin";
Filter FILTER_CONFIG = new()
{
    //all values are inclusive
    MinSkip = 0,
    MaxSkip = 0,
    ScoreCap = 5000,
    DrawScoreCap = 5000,
    WrongScoreCap = 5000, 
    MinPieces = 7,
    QSearchDepth = 10
};

//Network.LoadDefaultNetwork();
Network.InitEmptyNetwork(1);
DataGen.RunPrompt();
//BitboardUtils.Repl();
//FilterPlayouts(BIN_PLAYOUT_FILES, TD_FILE, FILTER_CONFIG);
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
return;

void FilterPlayouts((string, int)[] inputFileNames, string outputFileName, Filter filter)
{
    Console.WriteLine($"Extracting quiet positions into {outputFileName}.");
    Console.WriteLine();
    long totalPositions = 0;
    var output = new BinaryWriter(new FileStream(outputFileName, FileMode.Create));
    foreach ((string inputFile, int skip) in inputFileNames)
    {
        var input = File.OpenRead(DATA_PATH + inputFile);
        filter.StartSkip = (short)skip;        
        Console.WriteLine($"Reading {inputFile}");
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.FilterPlayouts(input, output, filter);
        long t_1 = Stopwatch.GetTimestamp();
        totalPositions += positions;
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        input.Close();
    }
    Console.WriteLine($"Done! ({totalPositions/1000_000}M positions)");
    output.Close();
}

void PgnToUci(string pgnFileName)
{
    Console.WriteLine($"Converting PGN to 'position startpos move ...' format..");
    var output = File.CreateText(DATA_PATH + pgnFileName + ".uci");
    var input = File.OpenText(DATA_PATH + pgnFileName);
    DataUtils.PgnToUci(input, output);
    input.Close();
}

int MAX_Q_DEPTH = 10;
int FEN_PER_GAME = 10;
int SKIP_OUTLIERS = 200;

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

double Seconds(long ticks) => ticks / (double)Stopwatch.Frequency;
