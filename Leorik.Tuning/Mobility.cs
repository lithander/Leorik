
using Leorik.Core;

namespace Leorik.Tuning
{
    static class Mobility
    {
        //Max possible moves:
        //Pawn = 12 (3x4 promotions)
        //Knight = 8
        //Bishop = 13
        //Rook = 14
        //Queen = 27
        //King = 8

        static Move[] _moveBuffer = new Move[225];
        static MoveGen _moveGen = new MoveGen(_moveBuffer, 0);

        public static Move[] GetMoves(BoardState board)
        {
            _moveGen.Next = 0;
            _moveGen.Collect(board);
            Move[] result = new Move[_moveGen.Next];
            Array.Copy(_moveBuffer, result, result.Length);
            return result;
        }

        static Dictionary<Piece, int> MaxMoves = new Dictionary<Piece, int>();

        public static void LogStatistics(BoardState board, Move[] moves)
        {
            //Console.WriteLine(moves.Length + " moves:");
            Dictionary<int, int> stats = new Dictionary<int, int>();
            foreach(var move in moves)
            {
                if (stats.TryGetValue(move.FromSquare, out int count))
                    stats[move.FromSquare] = count + 1;
                else
                    stats[move.FromSquare] = 1;
            }
            foreach (var kv in stats)
            {
                Piece piece = board.GetPiece(kv.Key);
                //Console.WriteLine($"  {piece} on {Notation.GetSquareName(kv.Key)} has {kv.Value} moves!");
                if (!MaxMoves.TryGetValue(piece, out int count) || kv.Value > count)
                    MaxMoves[piece] = kv.Value;
            }
        }

        public static void LogMaxMoves()
        {
            foreach(var kv in MaxMoves)
            {
                Console.WriteLine(kv.Key + " MaxMoves= " + kv.Value);
            }
        }
    }
}
