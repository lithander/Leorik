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

        private int _moveTime;
        private int _moveTimeLimit;
        private int _maxDepth;

        private long _t0 = -1;
        private long _tN = -1;

        private long Now => Stopwatch.GetTimestamp();
        public int Elapsed => MilliSeconds(Now - _t0);
        public int ElapsedInterval => MilliSeconds(Now - _tN);

        private int MilliSeconds(long ticks)
        {
            double dt = ticks / (double)Stopwatch.Frequency;
            return (int)(1000 * dt);
        }

        private void Reset()
        {
            _moveTimeLimit = MAX_TIME;
            _t0 = Now;
            _tN = _t0;
        }

        public void StartInterval()
        {
            _tN = Now;
        }

        public void Stop()
        {
            //this will cause CanSearchDeeper() and CheckTimeBudget() to evaluate to 'false'
            _moveTimeLimit = 0;
        }

        internal void Go(int maxDepth, int timePerMove)
        {
            Reset();
            _maxDepth = maxDepth;
            _moveTimeLimit = _moveTime = Math.Min(timePerMove, MAX_TIME);
        }

        internal void Go(int maxDepth, int time, int increment, int movesToGo)
        {
            Reset();
            _maxDepth = maxDepth;
            int futureMoves = movesToGo - 1;
            int timeRemaining = Math.Min(time, MAX_TIME) + futureMoves * increment;
            _moveTime = Math.Min(time, timeRemaining / movesToGo);
            //abort as soon as the remaining moves have each less than 'reserve_ratio' of this one
            float reserveRatio = increment > 50 ? PLAY_ON_INC : PLAY_ON_RESERVE;
            int reserve = (int)(futureMoves * _moveTime * reserveRatio);
            _moveTimeLimit = Math.Min(time, timeRemaining - reserve);
        }

        public bool CanSearchDeeper(int depth, float stability)
        {
            if (depth >= _maxDepth)
                return false;

            _moveTimeLimit -= BASE_MARGIN;

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
            return Elapsed > _moveTimeLimit;
        }
    }
}
