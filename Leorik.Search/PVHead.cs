using Leorik.Core;

namespace Leorik.Search
{
    internal struct PVHead
    {
        private readonly Move[] _moves;
        private int _index;
        private int _stride;

        public PVHead(Move[] moves, int depth)
        {
            _moves = moves;
            _index = 0;
            _stride = depth;
        }

        private PVHead(Move[] moves, int index, int stride)
        {
            _moves = moves;
            _index = index;
            _stride = stride;
        }

        public PVHead NextDepth => new PVHead(_moves, _index + _stride, _stride - 1);

        public void Extend(Move move)
        {
            _moves[_index] = move;
            for (int i = 1; i < _stride; i++)
                _moves[_index + i] = _moves[_index + i + _stride - 1];
        }

        internal void Truncate()
        {
            _moves[_index] = default;
        }
    }
}
