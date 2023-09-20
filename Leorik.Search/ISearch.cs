using Leorik.Core;

namespace Leorik.Search
{
    public struct SearchOptions
    {
        public int MidgameRandomness;
        public int EndgameRandomness;
        public long MaxNodes;
        public int NullMoveCutoff;
        public int Threads;

        internal readonly int Randomness(float phase)
        {
            return Math.Max(0, (int)(MidgameRandomness + (EndgameRandomness - MidgameRandomness) * phase));
        }

        public readonly static SearchOptions Default = new();


        public SearchOptions()
        {
            Threads = 1;
            MaxNodes = long.MaxValue;
            MidgameRandomness = 0;
            EndgameRandomness = 0;
            NullMoveCutoff = 338;
        }
    }

    public interface ISearch
    {
        bool Aborted { get; }
        int Depth { get; }
        int Score { get; }
        long NodesVisited { get; }
        Span<Move> PrincipalVariation { get; }
        void SearchDeeper(Func<bool> checkTimeBudget);
        void Search(int depth);
    }
}
