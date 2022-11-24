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

        enum PGNParserState { Header, MoveNumber, WhiteMove, BlackMove, Stop };

        private BoardState _board;
        private PGNParserState _state = PGNParserState.Header;
        private int _moveNumber;
        private string _line;
        private int _index;
        private StreamReader _file;
        private List<BoardState> _positions = new();

        public PgnParser(StreamReader file)
        {
            _file = file;
        }

        private void Reset()
        {
            Result = "";
            _board = Notation.GetStartingPosition();
            _state = PGNParserState.Header;
            _positions.Clear();
            _moveNumber = 0;
            _line = "";
        }

        public bool NextGame()
        {
            Reset();
            while (_state != PGNParserState.Stop)
            {
                switch(_state)
                {
                    case PGNParserState.Header:
                        ParseHeader(); break;
                    case PGNParserState.MoveNumber:
                        ParseMoveNumber(); break;
                    case PGNParserState.WhiteMove:
                        ParseWhiteMove(); break;
                    case PGNParserState.BlackMove:
                        ParseBlackMove(); break;
                }
            }
            return !_file.EndOfStream;
        }

        private void ParseHeader()
        {
            //reader header blocks [...] skip empty lines, parse the result
            while (!_file.EndOfStream)
            {
                _line = _file.ReadLine();
                if (string.IsNullOrEmpty(_line))
                    continue;

                if (_line[0] != '[')
                {
                    _index = 0;
                    _state = PGNParserState.MoveNumber;
                    return;
                }

                if (_line.StartsWith("[Result "))
                {
                    int stop = _line.IndexOf(']');
                    Result = _line.Substring(9, stop - 10);
                }
            }
            _state = PGNParserState.Stop;
        }

        private void ParseMoveNumber()
        {
            _moveNumber++;
            //Console.WriteLine($"Move#: {_moveNumber}");
            string token = $"{_moveNumber}. ";
            int i = _line.IndexOf(token, _index);
            _index = i + token.Length;
            _state = PGNParserState.WhiteMove;
        }
        private void ParseWhiteMove()
        {
            string moveStr = ParseToken();
            //Console.WriteLine($"White: {moveStr}");
            PlayMove(moveStr);
            _state = SkipComment() ? PGNParserState.BlackMove : PGNParserState.Stop;
        }

        private void ParseBlackMove()
        {
            string moveStr = ParseToken();
            //Console.WriteLine($"Black: {moveStr}");
            PlayMove(moveStr);
            _state = SkipComment() ? PGNParserState.MoveNumber : PGNParserState.Stop;
        }

        private string ParseToken()
        {
            int end = _line.IndexOf(' ', _index);
            if(end == -1)
            {
                string token = _line.Substring(_index);
                _line = _file.ReadLine();
                _index = 0;
                return token;
            }
            else
            {
                string token = _line.Substring(_index, end - _index);
                _index = end;
                return token;
            }
        }

        private bool SkipComment()
        {
            //now there could follow a comment {}
            if(_line[_index] == ' ')
                _index++;
            if (_line[_index] == '{')
                _index = _line.IndexOf('}', _index) + 2;

            if(_line.Length <= _index)
            {
                _line = _file.ReadLine();
                _index = 0;
            }

            return _line.IndexOf(Result, _index) == -1;
        }               

        private void PlayMove(string moveNotation)
        {
            Move move = Notation.GetMove(_board, moveNotation);
            _board.Play(move);
            _positions.Add(_board.Clone());
            //Console.WriteLine(Notation.GetFen(_board));
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

        public static void ExtractData(string pgnFile, string epdFile, int posCount)
        {
            //Output Format Example:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            var output = File.CreateText(epdFile);
            var input = File.OpenText(pgnFile);
            PgnParser parser = new PgnParser(input);
            int games = 0;
            int positions = 0;
            while (parser.NextGame() && positions < posCount)
            {
                games++;
                if (parser.Result == "*")
                    continue;

                foreach(var pos in parser.Positions)
                {                    
                    positions++;
                    output.WriteLine($"{Notation.GetFen(pos)} c9 \"{parser.Result}\";");
                    if (positions >= posCount)
                        break;
                }

                if (games % 100 == 0)
                    Console.WriteLine($"{games} games, {positions} positions");
            }
            output.Close();
            input.Close();
        }        
    }
}
