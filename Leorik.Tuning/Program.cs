using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;

string[] BIN_PLAYOUT_FILES = {
    "2025-01-27T15.18.51_5K_8R_v13",
    "2025-01-28T15.38.41_5K_8R_v13",
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
    "2025-01-26T22.10.14_5K_8R_T100_v13"
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
    "2025-01-27T15.18.51_5K_8R_v13_Q5",
    "2025-01-28T15.38.41_5K_8R_v13_Q5",
    "2025-01-26T10.54.40_5K_8R_v13_Q5",
    "2025-01-25T12.46.26_5K_8R_v13_Q5",
    "2025-01-24T10.48.15_5K_8R_v13_Q5",
    "2025-01-20T19.24.20_5K_8R_v11_Q5",
    "2025-01-20T11.22.34_5K_8R_v11_Q5",
    "2025-01-15T15.33.46_5K_8R_v10_Q5",
    "2025-01-14T23.31.13_5K_8R_v10_Q5",
    "2025-01-14T15.34.57_5K_8R_v9_Q5",
    "2025-01-14T11.11.28_5K_8R_v9_Q5",
    //"2025-01-13T10.20.06_5K_8R_v8_Q5",
    //"2025-01-13T15.33.57_5K_8R_v8_Q5",
    "2025-01-26T22.10.14_5K_8R_T100_v13_Q5",
    "2025-01-22T14.46.15_5K_8R_T100_v13_Q5",
    "2025-01-20T23.50.57_5K_8R_T100_v12_Q5",
    "2025-01-19T19.53.07_5K_8R_T100_v12_Q5",
    "2025-01-19T16.31.41_5K_8R_T100_v12_Q5",
    "2025-01-17T14.58.40_5K_8R_T100_v11_Q5",
    "2025-01-16T18.20.47_5K_8R_T100_v11_Q5",
    "2025-01-13T20.21.30_5K_8R_T100_v9_Q5",
    //"2025-01-12T16.07.42_5K_8R_T100_v8_Q5",
    //"2025-01-12T04.03.08_5K_8R_T100_v7_Q5",
    //"2025-01-11T22.01.49_5K_8R_T100_v6_Q5",
    //"2025-01-11T18.20.21_5K_8R_T100_v5_Q5",
    //"2025-01-10T19.11.25_5K_8R_T100_v4",
    //"2025-01-09T19.13.27_5K_8R_T100_v3",
    //"2025-01-08T18.15.04_5K_8R_T100_v2",
    //"2025-01-06T21.13.24_5K_8R_T100_v0",
};


const string PLAYOUT_PATH = "D:/Projekte/Chess/Leorik/TD3/";
const string PLAYOUT_EXT = ".playout.bin";

const string TD_PATH = "D:/Projekte/Chess/Leorik/TD3/Filtered/";
const string TD_EXT = ".bullet.bin";

const string SHUFFLE_TARGET = "FiSh_5K_8R_Tmixed_v9-v13_Q5.bullet.bin";

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
FilterPlayouts(BIN_PLAYOUT_FILES, "_Q5", FILTER_CONFIG);
ShuffleFiltered(SHUFFLE_FILES, SHUFFLE_TARGET);
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");
return;

void ShuffleFiltered(string[] inputFileNames, string outputFileName)
{
    string[] inputFiles = new string[inputFileNames.Length];
    for (int i = 0; i < inputFileNames.Length; i++)
        inputFiles[i] = TD_PATH + inputFileNames[i] + TD_EXT;

    DataUtils.Shuffle(inputFiles, TD_PATH + outputFileName);
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
