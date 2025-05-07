namespace Leorik.Core
{
    public class Perft
    {
        private const int MAX_PLY = 10;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        private BoardState[] _positions;
        private Move[] _moves;

        public Perft()
        {
            _positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                _positions[i] = new BoardState();
            _moves = new Move[MAX_PLY * MAX_MOVES];
        }

        public long Compute(BoardState position, int depth)
        {
            _positions[0].Copy(position);
            return Count(0, depth, new MoveGen(_moves, 0));
        }

        private long Count(int depth, int remaining, MoveGen moves)
        {
            BoardState current = _positions[depth];
            BoardState next = _positions[depth + 1];
            int i = moves.Next;
            moves.CollectAll(current);
            long sum = 0;
            for (; i < moves.Next; i++)
            {
                if (next.PlayWithoutHashAndEval(current, ref _moves[i]))
                {
                    if (remaining > 1)
                        sum += Count(depth + 1, remaining - 1, moves);
                    else
                        sum++;
                }
            }
            return sum;
        }
    }
}
