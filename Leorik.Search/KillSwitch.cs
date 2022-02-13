using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    struct KillSwitch
    {
        private const int QUERY_INTERVAL = 50;

        Func<bool>? _killSwitch;
        bool _aborted;
        ulong _query;

        public KillSwitch(Func<bool>? killSwitch = null)
        {
            _killSwitch = killSwitch;
            _aborted = _killSwitch != null && _killSwitch();
            _query = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get()
        {
            if (!_aborted && ++_query >= QUERY_INTERVAL && _killSwitch != null)
            {
                _query = 0;
                _aborted = _killSwitch();
            }
            return _aborted;
        }
    }
}
