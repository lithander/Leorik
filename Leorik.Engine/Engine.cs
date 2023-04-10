using Leorik.Core;
using Leorik.Search;

namespace Leorik.Engine
{
    class Engine
    {
        IterativeSearch _search = null;
        Thread _searching = null;
        Move _best = default;
        TimeControl _time = new TimeControl();
        BoardState _board = Notation.GetStartingPosition();
        List<BoardState> _history = new List<BoardState>();

        public SearchOptions Options = SearchOptions.Default;
        public bool Running { get; private set; }
        public Color SideToMove => _board.SideToMove;
        public string GetFen() => Notation.GetFen(_board);
        public Evaluation GetEval() => _board.Eval;

        public void Init()
        {
            Running = true;

            //perform warmup sequence (especially useful if JIT-compiled)
            Uci.Silent = true;
            IterativeSearch search = new IterativeSearch(Notation.GetStartingPosition(), SearchOptions.Default, null);
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
            Move move = Notation.GetMoveUci(_board, moveNotation);
            _board.Play(move);
            _history.Add(_board.Clone());
            //Console.WriteLine(moveNotation);
            //Console.WriteLine(Notation.GetFEN(_board));
        }

        //**************
        //*** Search ***
        //**************

        internal void Go(int maxDepth, int maxTime, long maxNodes)
        {
            Stop();
            _time.Go(maxDepth, maxTime);
            StartSearch(maxNodes);
        }

        internal void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes)
        {
            Stop();
            _time.Go(maxDepth, maxTime, increment, movesToGo);
            StartSearch(maxNodes);
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

        //*****************
        //*** INTERNALS ***
        //*****************

        private void StartSearch(long maxNodes)
        {
            Transpositions.IncreaseAge();

            SearchOptions options = Options;
            options.MaxNodes = maxNodes;

            _search = new IterativeSearch(_board, options, _history);
            _time.StartInterval();
            _search.SearchDeeper();
            Collect();

            //start the search thread
            _searching = new Thread(Search) { Priority = ThreadPriority.Highest };
            _searching.Start();
        }

        private void Search()
        {
            if (_search == null)
                return;

            while (CanSearchDeeper())
            {
                _time.StartInterval();
                _search.SearchDeeper(_time.CheckTimeBudget);

                //aborted?
                if (_search.Aborted)
                    break;

                //collect PV
                Collect();
            }
            //Done searching!
            Uci.BestMove(_best);
            _search = null;
        }

        private bool CanSearchDeeper()
        {
            //max depth reached or game over?
            if (_search == null)
                return false;

            //otherwise it's only time that can stop us!
            return _time.CanSearchDeeper(_search.Depth);
        }

        private void Collect()
        {
            if (_search == null)
                return;

            if (_search.Aborted)
                return;

            if (_search.PrincipalVariation.Length > 0)
                _best = _search.PrincipalVariation[0];

            Uci.Info(
                depth: _search.Depth,
                score: (int)SideToMove * _search.Score, //the score from the engine's point of view in centipawns.
                nodes: _search.NodesVisited,
                timeMs: _time.Elapsed,
                pv: GetExtendedPV()
            );
        }

        private List<Move> GetExtendedPV()
        {
            var pv = _search.PrincipalVariation.ToArray();
            List<Move> result = new(pv);

            //1.) play the PV as far as available
            BoardState position = _board.Clone();
            foreach (Move move in pv)
                position.Play(move);

            //2. try to extract the remaining depth from the TT
            while (result.Count < _search.Depth && Transpositions.GetBestMove(position, out Move move))
            {
                position.Play(move);
                result.Add(move);
            }

            return result;
        }
    }
}
