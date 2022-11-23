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

    class PgnParser
    {
        public bool EndOfStream => _file.EndOfStream;
        public string Result { get; private set; }
        public IEnumerable<BoardState> Positions => _positions;

        enum PGNParserState { Result, Moves, Stop };

        private BoardState _board;
        private PGNParserState _state = PGNParserState.Result;
        private StreamReader _file;
        private List<BoardState> _positions = new();

        public PgnParser(StreamReader file)
        {
            _file = file;
        }

        public bool NextGame()
        {
            Reset();
            while (_state != PGNParserState.Stop)
            {
                if (_state == PGNParserState.Result)
                    ParseResult();
                else if (_state == PGNParserState.Moves)
                    ParseMoves();
            }
            return !_file.EndOfStream;
        }

        private void Reset()
        {
            Result = "";
            _board = Notation.GetStartingPosition();
            _state = PGNParserState.Result;
            _positions.Clear();
        }

        private void ParseResult()
        {
            while (true)
            {
                string line = _file.ReadLine();
                if (!string.IsNullOrEmpty(line) && line[0] == '[' && line.StartsWith("[Result "))
                {
                    int stop = line.IndexOf(']');
                    Result = line.Substring(8, stop - 8);
                    //Console.WriteLine(result);
                    _state = PGNParserState.Moves;
                    return;
                }
            }
        }


        private void ParseMoves()
        {
            while (true)
            {
                string line = _file.ReadLine();
                if (!string.IsNullOrEmpty(line) && line.StartsWith("1."))
                {
                    int move = 1;
                    while (true)
                    {
                        string move0 = move.ToString() + '.';
                        string move1 = (++move).ToString() + '.';
                        int c0 = line.IndexOf(move0);
                        int c1 = line.IndexOf(move1);
                        if (c1 > c0)
                            ParseMove(line.Substring(c0, c1 - c0), false);
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(line.Substring(c0));
                            sb.Append(' ');
                            line = _file.ReadLine();
                            c1 = line.IndexOf(move1);
                            if (c1 == -1)
                            {
                                sb.Append(line);
                                ParseMove(sb.ToString(), true);
                                _state = PGNParserState.Stop;
                                return;
                            }
                            else
                            {
                                sb.Append(line.Substring(0, c1));
                                ParseMove(sb.ToString(), false);
                            }
                        }
                    }
                }
            }
        }

        private void ParseMove(string move, bool final)
        {
            //Example: 1. a4 {+0.08/20 0.20s} Nh6 {+0.07/19 0.20s}
            //we care about the two moves that were being played
            //"1-0", "0-1", "1/2-1/2" or "*" end the game
            int c0 = move.IndexOf(' ') + 1;
            int c1 = move.IndexOf(' ', c0 + 1);
            string whiteMove = move.Substring(c0, c1 - c0);
            PlayMove(whiteMove);
            //now there could follow a comment {}
            if (move[c1 + 1] == '{')
                c0 = move.IndexOf('}') + 2;
            else
                c0 = c1 + 1;

            string blackMove = null;
            if (final && ParseLastMove(move, c0, ref blackMove))
            {
                if (blackMove != null)
                {
                    PlayMove(blackMove);
                    Console.WriteLine($"{whiteMove}|{blackMove}[END]");
                }
                else
                    Console.WriteLine($"{whiteMove}[END]");
            }
            else
            {
                if (final)
                    throw new Exception();

                c1 = move.IndexOf(' ', c0 + 1);
                blackMove = move.Substring(c0, c1 - c0);
                PlayMove(blackMove);
                Console.WriteLine($"{whiteMove}|{blackMove}");
            }
        }

        private void PlayMove(string moveNotation)
        {
            Move move = Notation.GetMove(_board, moveNotation);
            _board.Play(move);
            Console.WriteLine(Notation.GetFen(_board));
        }

        private static bool ParseLastMove(string move, int head, ref string blackMove)
        {
            return ParseLastMove(move, head, ref blackMove, "1-0") ||
            ParseLastMove(move, head, ref blackMove, "0-1") ||
            ParseLastMove(move, head, ref blackMove, "1/2-1/2") ||
            ParseLastMove(move, head, ref blackMove, "*");
        }

        private static bool ParseLastMove(string move, int head, ref string blackMove, string token)
        {
            int end = move.IndexOf(token);
            if (end == -1)
                return false;

            int comment = move.IndexOf('{', head);
            if (comment > 0)
                end = comment;

            int count = end - head - 1;
            if (count < 0)
                return true;

            blackMove = move.Substring(head, count);
            return true;
        }
    }

    static class DataUtils
    {
        public static Data ParseEntry(string line)
        {
            //Expected Format:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            //Labels: "1/2-1/2", "1-0", "0-1"

            const string WHITE = "1-0";
            const string DRAW = "1/2-1/2";
            const string BLACK = "0-1";

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

        public static void ExtractData(string pgnFile, string epdFile)
        {            
            var file = File.OpenText(pgnFile);
            PgnParser parser = new PgnParser(file);
            while (parser.NextGame())
            {

            }
        }        
    }
}
