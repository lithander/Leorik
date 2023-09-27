using Leorik.Core;

namespace Leorik.Search
{
    public class ParallelSearch : ISearch
    {
        List<IterativeSearch> _worker = new List<IterativeSearch>();
        int _best = 0;

        public Span<Move> PrincipalVariation => _worker[_best].PrincipalVariation;
        public bool Aborted => _worker[_best].Aborted;
        public int Depth => _worker[_best].Depth;
        public int Score => _worker[_best].Score;

        public long NodesVisited
        {
            get
            {
                long totalNodesVisited = 0;
                foreach (var worker in _worker)
                    totalNodesVisited += worker.NodesVisited;
                return totalNodesVisited;
            }
        }


        public ParallelSearch(BoardState board, SearchOptions options, IEnumerable<BoardState> history)
        {
            for (int i = 0; i < options.Threads; i++)
            {
                var worker = new IterativeSearch(board, options, history);
                _worker.Add(worker);
            }
        }

        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            // Using a lambda expression.
            Parallel.For(0, _worker.Count, i =>
            {
                _worker[i].SearchDeeper(killSwitch);
                _best = i;
            });
        }

        public void Search(int maxDepth)
        {
            // Using a lambda expression.
            Parallel.For(0, _worker.Count, i =>
            {
                while (_worker[i].Depth < maxDepth)
                    _worker[i].SearchDeeper(null);

                _best = i;
            });
        }
    }
}
