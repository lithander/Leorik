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
        public IReadOnlyList<BoardState> Positions => _positions;

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
            return !_file.EndOfStream;
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
                _index = _line.IndexOf('}', _index) + 2;

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
            //Console.WriteLine(Notation.GetFen(_board));
        }
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

        public static void ExtractData(StreamReader input, StreamWriter output, int posPerGame, int skipMargin, int skipOutliers, int maxCaptures)
        {
            //Output Format Example:
            //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
            Quiesce quiesce = new();
            PgnParser parser = new PgnParser(input);
            int games = 0;
            int positions = 0;
            while (parser.NextGame())
            {
                games++;
                if (parser.Result == "*")
                    continue;

                int numPos = posPerGame;
                if (parser.Result == DRAW)
                    numPos = 5;
                    //continue;

                //if (parser.Positions.Count > 200)
                //    continue;

                int p0 = skipMargin;
                int p1 = parser.Positions.Count - skipMargin;
                //Console.WriteLine($"{parser.Positions.Count} -> {parser.Result}");
                for (int i = 0; i < numPos; i++)
                {
                    int pi = p0 + (p1 - p0) * i / (numPos - 1);
                    var pos = parser.Positions[pi];

                    var quiet = quiesce.GetQuiet(pos);
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

                if (games % 1000 == 0)
                    Console.WriteLine($"{games} games, {positions} positions");
            }
        }
    }

    class Quiesce
    {
        private BoardState[] Positions;
        private Move[] Moves;
        private BoardState[] Results;

        public Quiesce()
        {
            const int MAX_PLY = 50;
            const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058

            Moves = new Move[MAX_PLY * MAX_MOVES];
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();

            Results = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Results[i] = new BoardState();
        }

        public BoardState GetQuiet(BoardState position)
        {
            const int MIN_ALPHA = -Evaluation.CheckmateScore;
            const int MAX_BETA = Evaluation.CheckmateScore;
            Positions[0].Copy(position);
            Results[0].Copy(position);
            MoveGen moveGen = new MoveGen(Moves, 0);
            int score = EvaluateQuiet(0, MIN_ALPHA, MAX_BETA, moveGen);
            int score2 = (int)position.SideToMove * Results[0].Eval.Score;
            if (score != score2)
                return null; //This typically means a checkmate or stalemate - we ignore those!

            return Results[0];
        }

        private int EvaluateQuiet(int ply, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[ply];
            bool inCheck = current.InCheck();
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = current.RelativeScore();

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                {
                    Results[ply].Copy(current);
                    alpha = standPatScore;
                }
            }

            //To quiesce a position play all the Captures!
            BoardState next = Positions[ply + 1];
            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestCapture(i, moveGen.Next);
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                    {
                        //int score2 = (int)current.SideToMove * Results[ply + 1].Eval.Score;
                        //Debug.Assert(score == score2);
                        Results[ply].Copy(Results[ply+1]);
                        alpha = score;
                    }
                }
            }

            if (!inCheck)
                return alpha;

            //Play Quiets only when in check!
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                    {
                        //int score2 = (int)current.SideToMove * Results[ply + 1].Eval.Score;
                        //Debug.Assert(score == score2);
                        Results[ply].Copy(Results[ply + 1]);
                        alpha = score;
                    }
                }
            }

            return movesPlayed ? alpha : Evaluation.Checkmate(ply);
        }

        private void PickBestCapture(int first, int end)
        {
            //find the best move...
            int best = first;
            int bestScore = Moves[first].MvvLvaScore();
            for (int i = first + 1; i < end; i++)
            {
                int score = Moves[i].MvvLvaScore();
                if (score >= bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            //...swap best with first
            if (best != first)
            {
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
        }
    }
}
