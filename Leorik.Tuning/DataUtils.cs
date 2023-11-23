using Leorik.Core;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Leorik.Tuning
{
    class Data
    {
        public BoardState Position;
        public sbyte Result; //{1 (White Wins), 0, -1 (Black wins)}
    }

    static class DataUtils
    {
        const string WHITE = "1-0";
        const string DRAW = "1/2-1/2";
        const string BLACK = "0-1";

        public static Data ParseEpdEntry(string line)
        {
            //Expected Format:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            //Labels: "1/2-1/2", "1-0", "0-1"

            int iLabel = line.IndexOf('"');
            string fen = line.Substring(0, iLabel - 1);
            string label = line.Substring(iLabel + 1, line.Length - iLabel - 3);
            Debug.Assert(label == BLACK || label == WHITE || label == DRAW);
            int result = (label == WHITE) ? 1 : (label == BLACK) ? -1 : 0;
            return new Data
            {
                Position = Notation.GetBoardState(fen),
                Result = (sbyte)result
            };
        }

        public static void LoadData(List<Data> data, string epdFile, int maxCount = int.MaxValue)
        {
            Console.WriteLine($"Loading Epd DATA from '{epdFile}'");
            var file = File.OpenText(epdFile);
            while (!file.EndOfStream && data.Count < maxCount)
            {
                string line = file.ReadLine();
                if (IsComment(line))
                    continue;
                data.Add(ParseEpdEntry(line));
            }

            Console.WriteLine($"{data.Count} labeled positions loaded!");
        }

        public static Data ParseWdlEntry(string line)
        {
            //Expected Format:
            //r5r2/p4pk1/2pb4/8/1p2rN2/4p3/PPPB4/3K4 w - - 0 3 [0.0];
            //Labels: "[0.5]", "[1.0]", "[0.0]" where 1.0 is white winning

            int iLabel = line.IndexOf('[');
            string fen = line.Substring(0, iLabel - 1);
            string label = line.Substring(iLabel + 1, 3);

            Debug.Assert(label == "0.5" || label == "1.0" || label == "0.0");
            sbyte result = (sbyte)(2 * float.Parse(label, NumberFormatInfo.InvariantInfo) - 1);
            return new Data
            {
                Position = Notation.GetBoardState(fen),
                Result = result
            };
        }

        public static void LoadWdlData(List<Data> data, string wdlFile, int maxCount = int.MaxValue)
        {
            Console.WriteLine($"Loading WDL DATA from '{wdlFile}'");
            var file = File.OpenText(wdlFile);
            while (!file.EndOfStream && data.Count < maxCount)
            {
                string line = file.ReadLine();
                if (IsComment(line))
                    continue;
                data.Add(ParseWdlEntry(line));
            }

            Console.WriteLine($"{data.Count} labeled positions loaded!");
        }

        internal static void LoadBinaryData(List<Data> data, string binFile, int maxCount = int.MaxValue)
        {
            Console.WriteLine($"Loading Marlinflow DATA from '{binFile}'");
            MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(binFile);
            var stream = mmf.CreateViewStream();
            BinaryReader reader = new BinaryReader(stream);
            PackedBoard packed = new();
            PackedBoard repacked = new();
            while (stream.Position < stream.Length && data.Count < maxCount)
            {
                packed.Read(reader);
                BoardState unpacked = packed.Unpack(out short moveNr, out short eval, out byte wdl, out byte extra);
                repacked.Pack(unpacked, moveNr, eval, wdl, extra);

                if (!repacked.Equals(packed))
                {
                    Console.WriteLine($"Occupancy: {repacked.Occupancy} vs {packed.Occupancy} = {repacked.Occupancy == packed.Occupancy}");
                    Console.Write($"Pieces:    ");
                    for (int i = 0; i < 16; i++)
                        Console.Write($"{(repacked.Pieces[i] == packed.Pieces[i] ? '-' : 'X')} ");
                    Console.WriteLine();
                    Console.WriteLine($"Occupancy: {repacked.Data} vs {packed.Data} = {repacked.Data == packed.Data}");
                    Console.WriteLine();
                }

                sbyte result = (sbyte)(wdl - 1);//convert from 2 = White, 1 = Draw, 0 = Black
                data.Add(new Data
                {
                    Position = unpacked,
                    Result = result
                });
                if (data.Count % 1_000_000 == 0)
                    Console.Write('.');
                //Console.Write(Notation.GetFen(PackedBoard.Unpack(ref packed), packed.FullmoveNumber));
                //string wdl = (packed.Wdl / 2f).ToString("F1", CultureInfo.InvariantCulture);
                //Console.WriteLine($" | {packed.Eval} | {wdl}");
            }
            Console.WriteLine();
            Console.WriteLine($"{data.Count} labeled positions loaded!");
        }

        private static bool IsComment(string line)
        {
            return line.Length > 1 && line[0] == '/' && line[1] == '/';
        }

        public static (int games, int positions) ExtractData(StreamReader input, StreamWriter output, int posPerGame, int skipOutliers, int maxQDepth)
        {
            //Output Format Example:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            Quiesce quiesce = new();
            PgnParser parser = new PgnParser(input);
            int games = 0;
            int positions = 0;
            while (parser.NextGame())
            {
                if (parser.Positions.Count == 0)
                    continue;

                games++;
                if (parser.Result == "*" || parser.Result == DRAW)
                    continue;

                int count = parser.Positions.Count;
                int skip = count / posPerGame;
                for (int i = 0; i < count; i++)
                {
                    var pos = parser.Positions[i];

                    var quiet = quiesce.QuiescePosition(pos, maxQDepth);
                    if (quiet == null)
                        continue;

                    //Confirmation bias: Let's not weaken the eval by something the eval can't understand
                    if (skipOutliers > 0)
                    {
                        if (quiet.Eval.Score < -skipOutliers && parser.Result != BLACK)
                            continue;
                        if (quiet.Eval.Score > skipOutliers && parser.Result != WHITE)
                            continue;
                    }

                    i += skip;
                    positions++;
                    output.WriteLine($"{Notation.GetFen(quiet)} c9 \"{parser.Result}\";");
                }
            }
            return (games, positions);
        }

        public static void PgnToUci(StreamReader input, StreamWriter output)
        {
            //Output Format Example:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            PgnParser parser = new PgnParser(input);
            int games = 0;
            while (parser.NextGame())
            {
                if (++games % 100 == 0)
                    Console.WriteLine($"{games} games");

                int plys = parser.Moves.Count;
                output.WriteLine($"Game #{games} - {plys} Plys - Result: {parser.Result}");
                output.WriteLine("{");
                for (int i = 0; i < plys; i++)
                {
                    if (i % 2 == 0)
                        output.WriteLine($"{1 + i / 2}.");

                    output.Write($"position startpos moves ");
                    for (int j = 0; j <= i; j++)
                    {
                        Move move = parser.Moves[j];
                        output.Write(Notation.GetMoveName(move));
                        output.Write(' ');
                    }
                    output.WriteLine();
                }
                output.WriteLine("}");
            }
        }

        internal static void CollectMetrics(List<Data> data)
        {
            int[] black = new int[64];
            int[] white = new int[64];
            foreach (var entry in data)
            {
                var pos = entry.Position;
                black[Bitboard.LSB(pos.Black & pos.Kings)]++;
                white[Bitboard.LSB(pos.White & pos.Kings)]++;
            }

            Console.WriteLine();
            Console.WriteLine("[Squares]");
            Console.WriteLine();
            BitboardUtils.PrintData(sq => sq);

            Console.WriteLine();
            Console.WriteLine("[Black King]");
            Console.WriteLine();
            int max = black.Max();
            BitboardUtils.PrintData(square => (int)(999 * black[square] / (float)max));

            Console.WriteLine();
            Console.WriteLine("[White King]");
            Console.WriteLine();
            max = white.Max();
            BitboardUtils.PrintData(square => (int)(999 * white[square] / (float)max));
        }
    }
}
