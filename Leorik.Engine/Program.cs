using Leorik.Core;
using Leorik.Search;

namespace Leorik.Engine
{
    public static class Program
    {
        const string NAME_VERSION = "Leorik 3.0.9";
        const string AUTHOR = "Thomas Jahn";

        static readonly Engine _engine = new();

        private static async Task Main()
        {
            //GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Console.WriteLine($"{NAME_VERSION} {Bitboard.SliderMode}");
            if (!Network.LoadDefaultNetwork())
                return;

            _engine.Init();

            while (_engine.Running)
            {
                string input = await Task.Run(Console.ReadLine);
                ParseUciCommand(input);
            }
        }

        private static void ParseUciCommand(string input)
        {
            if (input == null)
                return;

            //remove leading & trailing whitecases and split using ' ' as delimiter
            string[] tokens = input.Trim().Split();
            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine($"id name {NAME_VERSION}");
                    Console.WriteLine($"id author {AUTHOR}");
                    Console.WriteLine($"option name Hash type spin default {Transpositions.DEFAULT_SIZE_MB} min 1 max 2047");//consider gcAllowVeryLargeObjects if larger TT is needed
                    Console.WriteLine($"option name Threads type spin default {SearchOptions.Default.Threads} min 1 max 8");
                    Console.WriteLine($"option name Temperature type spin default {SearchOptions.Default.Temperature} min 0 max 1000");
                    //Console.WriteLine($"option name NullMoveCutoff type spin default {SearchOptions.Default.NullMoveCutoff} min 0 max 5000");
                    Console.WriteLine("uciok");
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "position":
                    UciPosition(tokens);
                    break;
                case "go":
                    UciGo(tokens);
                    break;
                case "ucinewgame":
                    _engine.Stop();
                    _engine.Reset();
                    break;
                case "stop":
                    _engine.Stop();
                    break;
                case "quit":
                    _engine.Quit();
                    break;
                case "setoption":
                    UciSetOption(tokens);
                    break;
                //inofficial commands
                case "fen":
                    Console.WriteLine(_engine.GetFen());
                    break;
                case "eval":
                    Console.WriteLine($"{_engine.GetEval().Score}");
                    break;
                case "flip":
                    _engine.Flip();
                    break;
                default:
                    Console.WriteLine($"Unknown command: {input}");
                    return;
            }
        }

        private static void UciSetOption(string[] token)
        {
            if (token[1] == "name" && token[2] == "Hash" && token[3] == "value" && int.TryParse(token[4], out int hashSizeMBytes))
                Transpositions.Resize(hashSizeMBytes);
            else if (token[1] == "name" && token[2] == "Threads" && token[3] == "value" && int.TryParse(token[4], out int threads))
                _engine.Options.Threads = threads;
            else if (token[1] == "name" && token[2] == "Temperature" && token[3] == "value" && int.TryParse(token[4], out int temperature))
                _engine.Options.Temperature = temperature;
            else if (token[1] == "name" && token[2] == "NullMoveCutoff" && token[3] == "value" && int.TryParse(token[4], out int nullMoveCutoff))
                _engine.Options.NullMoveCutoff = nullMoveCutoff;
            else
                Console.WriteLine($"Unknown UCI option: {String.Join(' ', token[2..])}");
        }

        private static void UciPosition(string[] tokens)
        {
            //position [fen <fenstring> | startpos ]  moves <move1> .... <movei>
            if (tokens.Length > 1 && tokens[1] == "startpos")
            {
                _engine.SetupPosition(Notation.GetStartingPosition());
            }
            else if (tokens.Length > 1 && tokens[1] == "fen") //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
            {
                string fen = string.Join(' ', tokens[2..]);
                _engine.SetupPosition(Notation.GetBoardState(fen));
            }
            else
            {
                Console.WriteLine($"Unknown parameter: {tokens[1]}");
                return;
            }

            int firstMove = Array.IndexOf(tokens, "moves") + 1;
            if (firstMove > 0)
            {
                for (int i = firstMove; i < tokens.Length; i++)
                    _engine.Play(tokens[i]);
            }
        }

        private static void UciGo(string[] tokens)
        {
            //Searching on a budget that may increase at certain intervals
            //40 Moves in 5 Minutes = go wtime 300000 btime 300000 movestogo 40
            //40 Moves in 5 Minutes, 1 second increment per Move =  go wtime 300000 btime 300000 movestogo 40 winc 1000 binc 1000 movestogo 40
            //5 Minutes total, no increment (sudden death) = go wtime 300000 btime 300000

            TryParse(tokens, "depth", out int maxDepth, IterativeSearch.MAX_PLY);
            TryParse(tokens, "movetime", out int maxTime, int.MaxValue);
            TryParse(tokens, "nodes", out long maxNodes, long.MaxValue);
            TryParse(tokens, "movestogo", out int movesToGo, GuessMovesToGo());
            Move[] searchMoves = ParseSearchMoves(tokens);

            if (_engine.SideToMove == Color.White && TryParse(tokens, "wtime", out int whiteTime))
            {
                TryParse(tokens, "winc", out int whiteIncrement);
                _engine.Go(whiteTime, whiteIncrement, movesToGo, maxDepth, maxNodes, searchMoves);
            }
            else if (_engine.SideToMove == Color.Black && TryParse(tokens, "btime", out int blackTime))
            {
                TryParse(tokens, "binc", out int blackIncrement);
                _engine.Go(blackTime, blackIncrement, movesToGo, maxDepth, maxNodes, searchMoves);
            }
            else
            {
                //Searching infinite within optional constraints
                _engine.Go(maxDepth, maxTime, maxNodes, searchMoves);
            }
        }

        private static int GuessMovesToGo()
        {
            int playedMoves = _engine.HistoryPlys / 2;
            return Math.Max(50 - playedMoves, 20);
        }

        private static bool TryParse(string[] tokens, string name, out int value, int defaultValue = 0)
        {
            if (int.TryParse(Token(tokens, name), out value))
                return true;
            //token couldn't be parsed. use default value
            value = defaultValue;
            return false;
        }

        private static bool TryParse(string[] tokens, string name, out long value, long defaultValue = 0)
        {
            if (long.TryParse(Token(tokens, name), out value))
                return true;
            //token couldn't be parsed. use default value
            value = defaultValue;
            return false;
        }

        private static Move[] ParseSearchMoves(string[] tokens)
        {
            int iParam = Array.IndexOf(tokens, "searchmoves");
            if (iParam < 0) 
                return Array.Empty<Move>();

            List<Move> moves = new List<Move>();
            int iValue = iParam + 1;
            while(iValue < tokens.Length)
            {
                try
                {
                    string notation = tokens[iValue++];
                    Move move = Notation.GetMoveUci(_engine.Position, notation);
                    moves.Add(move);
                }
                catch
                {
                    return moves.ToArray();
                }
            }
            return moves.ToArray();
        }

        private static string Token(string[] tokens, string name)
        {
            int iParam = Array.IndexOf(tokens, name);
            if (iParam < 0) return null;

            int iValue = iParam + 1;
            return (iValue < tokens.Length) ? tokens[iValue] : null;
        }
    }
}