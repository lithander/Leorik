using Leorik.Core;

namespace Leorik.Search
{
    public struct SearchOptions
    {
        public Variant Variant;
        public long MaxNodes;
        public int MultiPV;
        public int Threads;
        public int Temperature;
        public int Seed;
        public readonly static SearchOptions Default = new();

        public SearchOptions()
        {
            Variant = Variant.Standard;
            Threads = 1;
            MultiPV = 1;
            MaxNodes = long.MaxValue;
            Seed = -1;
        }
    }

    public interface ISearch
    {
        bool Aborted { get; }
        int Depth { get; }
        Span<Move> SearchMoves { get; }
        float Stability { get; }
        int Score { get; }
        long NodesVisited { get; }
        Span<Move> PrincipalVariation { get; }
        void SearchDeeper(Func<bool> checkTimeBudget, int firstMove);
        void Search(int depth);
    }
}
