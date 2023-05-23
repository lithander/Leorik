using Leorik.Core;
using System.ComponentModel;

namespace Leorik.Tuning
{
    struct Filter
    {
        public ulong WhiteKingMask;
        public ulong BlackKingMask;
        public string Comment;
    }

    struct FenBucket
    {
        public int CollectThreshold;
        public List<string> Positions;
    }

    class DataCollector
    {
        private readonly int _posPerGame;
        private readonly int _skipOutliers;
        private readonly Filter[] _filters;
        private readonly FenBucket[] _buckets;
        private int _numVisits;
        private int _numCollect;
        private BoardState _flipped;

        public DataCollector(Filter[] filters, int posPerGame, int skipOutliers)
        {
            _flipped = new BoardState();
            _filters = filters;
            _posPerGame = posPerGame;
            _skipOutliers = skipOutliers;
            _buckets = new FenBucket[filters.Length];
            for(int i = 0; i < _filters.Length; i++) 
            {
                _buckets[i].Positions = new List<string>();
            }
        }

        private bool Filter(int bucketIndex, BoardState board) 
        {
            ref Filter filter = ref _filters[bucketIndex];
            
            //WhiteKing matches filter?
            if ((board.White & board.Kings & filter.WhiteKingMask) == 0)
                return false;

            //BlackKing matches filter?
            if ((board.Black & board.Kings & filter.BlackKingMask) == 0)
                return false;

            //Position passed filter!
            return true;
        }

        public int Collect(BoardState board, string result, int totalPosCount)
        {
            int oldCollect = _numCollect;

            _flipped.Copy(board);
            _flipped.Flip();

            //TODO: each bucket should have it's own 'posPerGame' value
            int skip = totalPosCount / _posPerGame;
            _numVisits++;
            for (int i = 0; i < _buckets.Length; i++) 
            {
                //each bucket skips a few positions after a succesful collect
                if (_buckets[i].CollectThreshold > _numVisits)
                    continue;

                //does the position pass the filter?
                if (Filter(i, board) && !IsOutlier(board, result))
                {
                    AddToBucket(i, board, result, _numVisits + skip);
                    continue;
                }
                
                //does the flipped position pass the filter?
                if (Filter(i, _flipped) && !IsOutlier(_flipped, FlipResult(result)))
                {
                    AddToBucket(i, _flipped, FlipResult(result), _numVisits + skip);
                    continue;
                }
            }
            return _numCollect - oldCollect;
        }

        const string WHITE = "1-0";
        const string BLACK = "0-1";

        private bool IsOutlier(BoardState board, string result)
        {
            //Confirmation bias: Let's not weaken the eval by something the eval can't understand
            if (_skipOutliers > 0)
            {
                if (board.Eval.Score < -_skipOutliers && result != BLACK)
                    return true;
                if (board.Eval.Score > _skipOutliers && result != WHITE)
                    return true;
            }
            return false;
        }

        private void AddToBucket(int bucket, BoardState board, string result, int collecThreshold)
        {
            string line = $"{Notation.GetFen(board)} c9 \"{result}\";";
            _buckets[bucket].Positions.Add(line);
            _buckets[bucket].CollectThreshold = collecThreshold;
            _numCollect++;
        }

        private static string FlipResult(string result)
        {
            if (result == BLACK)
                return WHITE;
            if (result == WHITE)
                return BLACK;
            return result;
        }

        internal void WriteToStream(StreamWriter output)
        {
            for (int i = 0; i < _filters.Length; i++)
            {
                List<string> fens = _buckets[i].Positions;
                output.WriteLine($"// Count: {fens.Count} Filter: {_filters[i].Comment}");
                foreach(var fen in fens)
                    output.WriteLine(fen);
            }
        }

        internal void PrintMetrics()
        {
            for (int i = 0; i < _filters.Length; i++)
            {
                List<string> fens = _buckets[i].Positions;

                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine($"Count: {fens.Count}");
                Console.WriteLine($"Filter: { _filters[i].Comment}");
                Console.WriteLine();

                int[] black = new int[64];
                int[] white = new int[64];
                foreach (var entry in fens)
                {
                    var pos = Notation.GetBoardState(entry);
                    black[Bitboard.LSB(pos.Black & pos.Kings)]++;
                    white[Bitboard.LSB(pos.White & pos.Kings)]++;
                }

                Console.WriteLine();
                Console.WriteLine("[Black King]");
                int max = black.Max();
                BitboardUtils.PrintData(square => (int)(999 * black[square] / (float)max));
                Console.WriteLine();

                Console.WriteLine("[White King]");
                max = white.Max();
                BitboardUtils.PrintData(square => (int)(999 * white[square] / (float)max));
                Console.WriteLine();
            }
        }
    }
}
