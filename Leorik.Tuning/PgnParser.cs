using Leorik.Core;
using System.Diagnostics;

namespace Leorik.Tuning
{
    class PgnParser
    {
        public bool EndOfStream => _file.EndOfStream;
        public string Result { get; private set; }
        public IReadOnlyList<BoardState> Positions => _positions;
        public IReadOnlyList<Move> Moves => _moves;

        enum PGNParserState { Header, MoveNumber, WhiteMove, BlackMove, Stop };

        private BoardState _board;
        private PGNParserState _state = PGNParserState.Header;
        private int _moveNumber;
        private string _line;
        private int _index;
        private StreamReader _file;
        private List<BoardState> _positions = new();
        private List<Move> _moves = new();

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
            _moves.Clear();
            _moveNumber = 0;
            _line = "";
        }

        public bool NextGame()
        {
            if (_file.EndOfStream)
                return false;

            Reset();
            while (_state != PGNParserState.Stop)
            {
                switch (_state)
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
            return true;
        }

        private void ParseHeader()
        {
            //reader header blocks [...] skip empty lines, parse the result
            while (!_file.EndOfStream)
            {
                _line = _file.ReadLine();
                if (string.IsNullOrEmpty(_line) || _line == "\u001a")
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
            string token = ParseToken();
            Debug.Assert(token == $"{_moveNumber}.");
            _state = PGNParserState.WhiteMove;
        }
        private void ParseWhiteMove()
        {
            string moveStr = ParseToken();
            PlayMove(moveStr);
            _state = SkipComment() ? PGNParserState.BlackMove : PGNParserState.Stop;
        }

        private void ParseBlackMove()
        {
            string moveStr = ParseToken();
            PlayMove(moveStr);
            _state = SkipComment() ? PGNParserState.MoveNumber : PGNParserState.Stop;
        }

        private string ParseToken()
        {
            //skip whitespaces
            while (_line[_index] == ' ')
                _index++;

            int end = _line.IndexOf(' ', _index);
            if (end == -1)
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
            if (_line[_index] == ' ')
                _index++;
            if (_line[_index] == '{')
            {
                int end = _line.IndexOf('}', _index);
                while(end == -1)
                {
                    _index = 0;
                    _line = _file.ReadLine();
                    end = _line.IndexOf('}', _index);
                }
                _index = end + 2;
            }

            if (_line.Length <= _index)
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
            _moves.Add(move);
        }
    }
}
