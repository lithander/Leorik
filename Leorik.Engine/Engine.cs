using Leorik.Core;
using Leorik.Search;

namespace Leorik.Engine
{
    class Engine
    {
        ISearch _search = null;
        Thread _searching = null;
        TimeControl _time = new();
        BoardState _board = Notation.GetStartingPosition();
        BoardState _tempBoard = new();
        List<BoardState> _history = new();
        Perft _perft = new();

        public SearchOptions Options = SearchOptions.Default;
        public bool Running { get; private set; }
        public Color SideToMove => _board.SideToMove;
        public int HistoryPlys => _history.Count;
        public string GetFen() => Notation.GetFen(_board);
        public NeuralNetEval GetEval() => _board.Eval;
        public ulong GetZobristHash() => _board.ZobristHash;
        public void Flip() => _board.Flip();
        public Move GetMoveUci(string notation) => Notation.GetMoveUci(_board, notation, Options.Variant);
        public IEnumerable<Move> GetLegalMoves() => MoveGen.GetLegalMoves(_board);

        public void Init()
        {
            Running = true;

            //perform warmup sequence (especially useful if JIT-compiled)
            Uci.Silent = true;
            IterativeSearch search = new(Notation.GetStartingPosition(), SearchOptions.Default, null, null);
            search.Search(3);
            Reset();
            Uci.Silent = false;
        }

        internal void Quit()
        {
            Stop();
            Running = false;
        }

        //*************
        //*** SETUP ***
        //*************

        internal void Reset()
        {
            Transpositions.Clear();
        }

        internal void SetupPosition(BoardState board)
        {
            Stop();
            _board = board.Clone();
            _history.Clear();
            _history.Add(_board.Clone());
        }

        internal void Play(string moveNotation)
        {
            Stop();
            Move move = GetMoveUci(moveNotation);
            _board.Play(move);
            _history.Add(_board.Clone());
        }

        //**************
        //*** Search ***
        //**************

        internal void Go(int maxDepth, int maxTime, long maxNodes, Move[] searchMoves, bool pondering)
        {
            Stop();
            _time.Go(maxDepth, maxTime, pondering);
            StartSearch(maxNodes, searchMoves);
        }

        internal void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes, Move[] searchMoves, bool pondering)
        {
            Stop();
            _time.Go(maxDepth, maxTime, increment, movesToGo, pondering);
            StartSearch(maxNodes, searchMoves);
        }

        public void Stop()
        {
            if (_searching != null)
            {
                //this will cause the thread to terminate via CheckTimeBudget
                _time.Stop();
                _searching.Join();
                _searching = null;
            }
        }

        public void Ponderhit()
        {
            //this will start the clock!
            _time.Ponderhit();
        }

        public long Perft(int depth)
        {
            return _perft.Compute(_board, depth);
        }

        //*****************
        //*** INTERNALS ***
        //*****************

        private ulong[] SelectMoveHistory(IEnumerable<BoardState> history)
        {
            if (history == null)
                return null;

            List<ulong> reps = new();
            foreach (BoardState state in history)
            {
                if (state.HalfmoveClock == 0)
                    reps.Clear();
                reps.Add(state.ZobristHash);
            }
            return reps.ToArray();
        }

        private void StartSearch(long maxNodes, Move[] searchMoves)
        {
            Transpositions.IncreaseAge();

            SearchOptions options = Options;
            options.MaxNodes = maxNodes;
            if(options.Threads > 1)
                _search = new ParallelSearch(_board, options, SelectMoveHistory(_history), searchMoves);
            else
                _search = new IterativeSearch(_board, options, SelectMoveHistory(_history), searchMoves);

            //start the search thread
            _searching = new Thread(Search) { Priority = ThreadPriority.Highest };
            _searching.Start();
        }

        private void Search()
        {
            int multiPV = Math.Min(Options.MultiPV, _search.SearchMoves.Length);
            float bestMoveStability = 0.0f;
            Move best = default;
            Move ponder = default;

            do
            {
                for (int pvIndex = 0; pvIndex < multiPV; pvIndex++)
                {
                    _search.SearchDeeper(_time.CheckTimeBudget, pvIndex);

                    //aborted?
                    if (_search.Aborted)
                        break;


                    if (pvIndex == 0)
                    {
                        var pv = _search.PrincipalVariation;
                        best = pv.Length > 0 ? pv[0] : default;
                        ponder = pv.Length > 1 ? pv[1] : default;
                        bestMoveStability = _search.Stability;
                    }

                    LogUciInfo(pvIndex + 1);
                }
            }
            while (!_search.Aborted && _time.CanSearchDeeper(_search.Depth, multiPV, bestMoveStability));

            //wait until pondering is over
            while (_time.IsPondering)
                Thread.Sleep(1);

            //Done searching!
            Uci.BestMove(best, ponder, Options.Variant);
            _search = null;
        }

        private void LogUciInfo(int multiPV)
        {
            Uci.Info(
                depth: _search.Depth,
                score: (int)SideToMove * _search.Score, //the score from the engine's point of view in centipawns.
                nodes: _search.NodesVisited,
                timeMs: _time.Elapsed,
                multiPV: multiPV,
                pv: GetExtendedPV(),
                variant: Options.Variant
            );
        }

        private List<Move> GetExtendedPV()
        {
            List<Move> result = new(_search.Depth);
            _tempBoard.Copy(_board);

            //1.) play the PV as far as available
            var pv = _search.PrincipalVariation;
            foreach (Move move in pv)
            {
                _tempBoard.Play(move);
                result.Add(move);
            }

            //2. try to extract the remaining depth from the TT
            while (result.Count < _search.Depth && Transpositions.GetBestMove(_tempBoard, out Move move))
            {
                _tempBoard.Play(move);
                result.Add(move);
            }

            return result;
        }
    }
}
