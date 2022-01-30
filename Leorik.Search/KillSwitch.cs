using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Search
{
    struct KillSwitch
    {
        Func<bool>? _killSwitch;
        bool _aborted;

        public KillSwitch(Func<bool>? killSwitch = null)
        {
            _killSwitch = killSwitch;
            _aborted = _killSwitch != null && _killSwitch();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(bool update)
        {
            if (!_aborted && update && _killSwitch != null)
                _aborted = _killSwitch();
            return _aborted;
        }
    }
}
