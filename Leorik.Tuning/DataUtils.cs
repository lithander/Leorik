using Leorik.Core;
using System.Diagnostics;
using System.Text;

namespace Leorik.Tuning
{
    class Data
    {
        public BoardState Position;
        public sbyte Result; //{1 (White Wins), 0, -1 (Black wins)}
    }

    struct Bucket
    {
        public Bucket(string comment)
        {
            Comment = comment;
            Data = new List<Data>();
        }
        public string Comment;
        public List<Data> Data;
    }

    static class DataUtils
    {
        const string WHITE = "1-0";
        const string DRAW = "1/2-1/2";
        const string BLACK = "0-1";

        public static Data ParseEntry(string line)
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

        public static List<Data> LoadData(string epdFile)
        {
            List<Data> data = new List<Data>();
            Console.WriteLine($"Loading DATA from '{epdFile}'");
            var file = File.OpenText(epdFile);
            while (!file.EndOfStream)
            {
                string line = file.ReadLine();
                if (IsComment(line))
                    continue;

                data.Add(ParseEntry(line));
            }

            Console.WriteLine($"{data.Count} labeled positions loaded!");
            return data;
        }

        public static List<Bucket> LoadDataBuckets(string epdFile)
        {
            Console.WriteLine($"Loading DATA from '{epdFile}'");
            List<Bucket> result = new List<Bucket>();
            int count = 0;
            var file = File.OpenText(epdFile);

            string line = file.ReadLine();
            if (!IsComment(line))
                throw new Exception("Data blocks need to start with a comment!");

            Bucket bucket = new Bucket(line);

            while (!file.EndOfStream)
            {
                line = file.ReadLine();
                if (IsComment(line))
                {
                    result.Add(bucket);
                    bucket = new Bucket(line);
                }
                else
                {
                    count++;
                    bucket.Data.Add(ParseEntry(line));
                }
            }

            result.Add(bucket);
            Console.WriteLine($"{result.Count} buckets with {count} labeled positions loaded!");
            return result;
        }

        private static bool IsComment(string line)
        {
            return line.Length > 1 && line[0] == '/' && line[1] == '/';
        }

        public static (int games, int positions) ExtractData(StreamReader input, int skipOpening, int maxQDepth, DataCollector collector)
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
                for (int i = 0; i < count; i++)
                {
                    if (i < skipOpening)
                        continue;

                    var quiet = quiesce.QuiescePosition(parser.Positions[i], maxQDepth);
                    if (quiet != null)
                        positions += collector.Collect(quiet, parser.Result, count);
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
                for(int i = 0; i < plys; i++)
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
