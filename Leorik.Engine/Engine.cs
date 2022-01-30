using Leorik.Core;
using Leorik.Search;

namespace Leorik.Engine
{
    class Engine
    {
        IterativeSearch? _search = null;
        Thread? _searching = null;
        Move _best = default;
        int _maxSearchDepth;
        TimeControl _time = new TimeControl();
        BoardState _board = Notation.GetStartingPosition();
        List<BoardState> _history = new List<BoardState>();

        public bool Running { get; private set; }
        public Color SideToMove => _board.SideToMove;

        public void Start()
        {
            Stop();
            Running = true;
        }

        internal void Quit()
        {
            Stop();
            Running = false;
        }

        //*************
        //*** SETUP ***
        //*************

        internal void SetupPosition(BoardState board)
        {
            Stop();
            _board = new BoardState(board);//make a copy
            _history.Clear();
            _history.Add(new BoardState(_board));
        }

        internal void Play(string moveNotation)
        {
            Stop();
            Move move = Notation.GetMoveUci(_board, moveNotation);
            _board.Play(ref move);
            _history.Add(new BoardState(_board));
            Console.WriteLine(moveNotation);
            Console.WriteLine(Notation.GetFEN(_board));
        }

        //**************
        //*** Search ***
        //**************

        internal void Go(int maxDepth, int maxTime, long maxNodes)
        {
            Stop();
            _time.Go(maxTime);
            StartSearch(maxDepth, maxNodes);
        }

        internal void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes)
        {
            Stop();
            _time.Go(maxTime, increment, movesToGo);
            StartSearch(maxDepth, maxNodes);
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

        private void StartSearch(int maxDepth, long maxNodes)
        {
            //do the first iteration. it's cheap, no time check, no thread
            Uci.Log($"Search scheduled to take {_time.TimePerMoveWithMargin}ms!");

            //add all history positions with a score of 0 (Draw through 3-fold repetition) and freeze them by setting a depth that is never going to be overwritten
            //TODO:
            //foreach (var position in _history)
            //    Transpositions.Store(position.ZobristHash, Transpositions.HISTORY, 0, SearchWindow.Infinite, 0, default);
            
            _search = new IterativeSearch(_board, maxNodes);
            _time.StartInterval();
            _search.SearchDeeper();
            Collect();

            //start the search thread
            _maxSearchDepth = maxDepth;
            _searching = new Thread(Search) {Priority = ThreadPriority.Highest};
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
            if (_search == null || _search.Depth >= _maxSearchDepth)
                return false;

            //otherwise it's only time that can stop us!
            return _time.CanSearchDeeper();
        }

        private void Collect()
        {
            if (_search == null)
                return;

            if(_search.PrincipalVariation.Length > 0)
                _best = _search.PrincipalVariation[0];

            Uci.Info(
                depth:  _search.Depth, 
                score:  (int)SideToMove * _search.Score, 
                nodes:  _search.NodesVisited, 
                timeMs: _time.Elapsed, 
                pv:     GetPrintablePV(_search.PrincipalVariation, _search.Depth)
            );
        }

        private Move[] GetPrintablePV(Move[] pv, int depth)
        {
            return pv;
            //TODO:
            //List<Move> result = new(pv);
            ////Try to extend from TT to reach the desired depth?
            //if (result.Count < depth)
            //{
            //    Board position = new Board(_board);
            //    foreach (Move move in pv)
            //        position.Play(move);
            //
            //    while (result.Count < depth && Transpositions.GetBestMove(position, out Move move))
            //    {
            //        position.Play(move);
            //        result.Add(move);
            //    }
            //}
            //return result.ToArray();
        }
    }
}
