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
                data.Add(ParseEntry(file.ReadLine()));

            Console.WriteLine($"{data.Count} labeled positions loaded!");
            return data;
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
