using Leorik.Core;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;

namespace Leorik.Tuning
{
    class Data
    {
        public BoardState Position;
        public sbyte Result; //{1 (White Wins), 0, -1 (Black wins)}
    }

    struct Filter
    {
        public short StartSkip;
        public short MinSkip;
        public short MaxSkip;
        public short ScoreCap;
        public short DrawScoreCap;
        public short WrongScoreCap;
        public short MinPieces;
        public short QSearchDepth;
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
            return new Data
            {
                Position = Notation.GetBoardState(fen),
                Result = EpdLabelToResult(label)
            };
        }

        public static sbyte EpdLabelToResult(string label)
        {
            Debug.Assert(label == BLACK || label == WHITE || label == DRAW);
            int result = (label == WHITE) ? 1 : (label == BLACK) ? -1 : 0;
            return (sbyte)result;
        }

        public static string WdlToEpdLabel(int wdl)
        {
            switch(wdl)
            {
                case 2:
                    return WHITE;
                case 1: 
                    return DRAW;
                case 0:
                    return BLACK;
            }
            throw new ArgumentOutOfRangeException(nameof(wdl));
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
            MarlinFormat packed = new();
            MarlinFormat repacked = new();
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
                    Console.WriteLine($"Data: {repacked.Data} vs {packed.Data} = {repacked.Data == packed.Data}");
                    Console.WriteLine();
                    continue;
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

        public static (int games, int positions) ExtractPgnToEpd(StreamReader input, StreamWriter output, int posPerGame, int skipOutliers, int maxQDepth)
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
                        if (quiet.Score() < -skipOutliers && parser.Result != BLACK)
                            continue;
                        if (quiet.Score() > skipOutliers && parser.Result != WHITE)
                            continue;
                    }

                    i += skip;
                    positions++;
                    output.WriteLine($"{Notation.GetFen(quiet)} c9 \"{parser.Result}\";");
                }
            }
            return (games, positions);
        }

        public static (int games, int positions) FilterPlayouts(FileStream input, BinaryWriter output, Filter filter, int randomSeed = 1337)
        {
            int games = 0;
            int positions = 0;

            Random rnd = new Random(randomSeed);
            Quiesce quiesce = new();
            MarlinFormat marlin = new MarlinFormat();
            BulletFormat bullet = new BulletFormat();
            var reader = new BinaryReader(input);
            while (input.Position < input.Length)
            {
                games++;

                marlin.Read(reader);
                BoardState board = marlin.Unpack(out short fullMoveNumber, out short eval, out byte wdl, out byte extra);

                //Console.WriteLine(Notation.GetFen(board));
                int skipMoves = filter.StartSkip;
                for (int iMove = 0; iMove < extra; iMove++)
                {
                    Move move = (Move)reader.ReadInt32();
                    board.Play(move);
                    short score = reader.ReadInt16();
                    //Console.WriteLine($"{Notation.GetMoveName(move)} {score}");

                    if (--skipMoves >= 0)
                        continue;

                    //skip positions with too few pieces on the board
                    int pieceCount = Bitboard.PopCount(board.Black | board.White);
                    if (pieceCount < filter.MinPieces)
                        continue;

                    //skip positions with extreme scores
                    if (Math.Abs(score) > filter.ScoreCap)
                        continue;

                    //Wdl: 1 = Draw
                    if (wdl == 1 && Math.Abs(score) > filter.DrawScoreCap)
                        continue;

                    //2.An epd that is a white win with a score < -X is removed.
                    if (wdl == 2 && score < -filter.WrongScoreCap)
                        continue;

                    //3.An epd that is a white loss with a score > X is removed.
                    if (wdl == 0 && score > filter.WrongScoreCap)
                        continue;

                    var quiet = quiesce.QuiescePosition(board, filter.QSearchDepth);
                    if (quiet == null)
                        continue;

                    bullet.PackDestructive(quiet, score, wdl);
                    bullet.Write(output);

                    //int dtz = extra - iMove;
                    //int fullMove = fullMoveNumber + iMove / 2;
                    //marlin.Pack(quiet, (short)fullMove, score, wdl, (byte)dtz);
                    //marlin.Write(output);

                    positions++;
                    skipMoves = rnd.Next(filter.MinSkip, filter.MaxSkip+1);
                }
            }
            return (games, positions);
        }

        public static (int games, int positions) ExtractBinaryToEpd(FileStream input, StreamWriter output, int maxQDepth)
        {
            int games = 0;
            int positions = 0;

            Quiesce quiesce = new();
            MarlinFormat packed = new MarlinFormat();
            var reader = new BinaryReader(input);
            while (input.Position < input.Length)
            {
                games++;

                packed.Read(reader);
                BoardState board = packed.Unpack(out short fullMoveNumber, out short eval, out byte wdl, out byte extra);
                //Console.WriteLine(Notation.GetFen(board));
                for (int iMove = 0; iMove < extra; iMove++)
                {
                    Move move = (Move)reader.ReadInt32();
                    board.Play(move);
                    short score = reader.ReadInt16();
                    //Console.WriteLine($"{Notation.GetMoveName(move)} {score}");

                    var quiet = quiesce.QuiescePosition(board, maxQDepth);
                    if (quiet == null)
                        continue;

                    //TODO: skip positions!?

                    positions++;
                    string label = WdlToEpdLabel(wdl);
                    //the closer to the last move (dtz) the more wdl reflects the true value of the position
                    output.WriteLine($"{Notation.GetFen(quiet)} c9 \"{label}\";");
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
    }
}
