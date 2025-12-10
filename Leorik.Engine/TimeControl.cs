using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leorik.Engine
{
    class TimeControl
    {
        const int BASE_MARGIN = 5;
        const int MAX_TIME = int.MaxValue / 3; //large but not too large to cause overflow issues
        const float PLAY_ON_INC = 0.5f;
        const float PLAY_ON_RESERVE = 0.85f;

        private bool _pondering = false;
        private int _moveTime;
        private int _moveTimeLimit;
        private int _maxDepth;
        private long _t0 = -1;

        private long Now => Stopwatch.GetTimestamp();
        public int Elapsed => MilliSeconds(Now - _t0);
        public bool IsPondering => _pondering;

        private int MilliSeconds(long ticks)
        {
            double dt = ticks / (double)Stopwatch.Frequency;
            return (int)(1000 * dt);
        }

        public void Stop()
        {
            //this will cause CanSearchDeeper() and CheckTimeBudget() to evaluate to 'false'
            _pondering = false;
            _moveTimeLimit = 0;
        }

        public void Ponderhit()
        {
            //now the clock's running!
            _pondering = false;
            _t0 = Now;
            _moveTime /= 2;
        }

        internal void Go(int maxDepth, int timePerMove, bool pondering)
        {
            _pondering = pondering;
            _t0 = Now;
            _maxDepth = maxDepth;
            _moveTimeLimit = _moveTime = Math.Min(timePerMove, MAX_TIME);
        }

        internal void Go(int maxDepth, int time, int increment, int movesToGo, bool pondering)
        {
            _pondering = pondering;
            _t0 = Now;
            _maxDepth = maxDepth;
            int futureMoves = movesToGo - 1;
            int timeRemaining = Math.Min(time, MAX_TIME) + futureMoves * increment;
            _moveTime = Math.Min(time, timeRemaining / movesToGo);
            //abort as soon as the remaining moves have each less than 'reserve_ratio' of this one
            float reserveRatio = increment > 50 ? PLAY_ON_INC : PLAY_ON_RESERVE;
            int reserve = (int)(futureMoves * _moveTime * reserveRatio);
            _moveTimeLimit = Math.Min(time, timeRemaining - reserve);
        }

        public bool CanSearchDeeper(int depth, int multiPV, float stability)
        {
            _moveTimeLimit -= BASE_MARGIN * multiPV;

            if (depth >= _maxDepth)
                return false;

            if (_pondering)
                return true; //clock's not running

            //high stability means we're confident we already have the best move
            float stopRatio = 1 / (1 + 2 * stability);
            if (Elapsed > stopRatio * _moveTime)
                return false;

            //all conditions fulfilled
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckTimeBudget()
        {
            if (_pondering)
                return false; //no need to ever abort during pondering!
            else
                return Elapsed > _moveTimeLimit;
        }
    }
}
