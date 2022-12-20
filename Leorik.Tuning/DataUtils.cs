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

        public static void ExtractData(StreamReader input, StreamWriter output, int posPerGame, int skipOutliers, int maxCaptures)
        {
            //Output Format Example:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            Quiesce quiesce = new();
            PgnParser parser = new PgnParser(input);
            int games = 0;
            int positions = 0;
            while (parser.NextGame())
            {
                if (++games % 1000 == 0)
                    Console.WriteLine($"{games} games, {positions} positions");

                if (parser.Result == "*")
                    continue;

                if (parser.Result == DRAW)
                    continue;

                int count = parser.Positions.Count;
                for (int i = 0; i < posPerGame; i++)
                {
                    int pi = count * (i+1) / (posPerGame+1);
                    var pos = parser.Positions[pi];

                    var quiet = quiesce.QuiescePosition(pos);
                    if (quiet == null)
                        continue;

                    //is the position too complicated?
                    int numPiecesBefore = Bitboard.PopCount(pos.Black | pos.White);
                    int numPiecesAfter = Bitboard.PopCount(quiet.Black | quiet.White);
                    int numCaptures = numPiecesBefore - numPiecesAfter;
                    //Console.WriteLine($"{numCaptures} --- {numPiecesBefore} vs {numPiecesAfter}");
                    if (numCaptures > maxCaptures)
                        continue; //position is too complicated

                    //Confirmation bias: Let's not weaken the eval by something the eval can't understand
                    if (skipOutliers > 0)
                    {
                        if (quiet.Eval.Score < -skipOutliers && parser.Result != BLACK)
                            continue;
                        if (quiet.Eval.Score > skipOutliers && parser.Result != WHITE)
                            continue;
                    }

                    positions++;
                    output.WriteLine($"{Notation.GetFen(quiet)} c9 \"{parser.Result}\";");
                }
            }
        }
    }    
}
