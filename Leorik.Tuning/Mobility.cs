
using Leorik.Core;

namespace Leorik.Tuning
{
    static class Mobility
    {
        //Max possible moves:
        //Pawn      = 12 + zero = 13  [00..12]
        //Knight    =  8 + zero =  9  [13..21]
        //Bishop    = 13 + zero = 14  [22..35]
        //Rook      = 14 + zero = 15  [36..50]
        //Queen     = 27 + zero = 28  [51..78]
        //King      =  8 + zero =  9  [79..87]
        //--------------
        //TOTAL     = 82        = 88

        static Move[] _moveBuffer = new Move[225];
        static MoveGen _moveGen = new MoveGen(_moveBuffer, 0);
        static short[] PieceMobilityIndices = new short[8] { 0, 0, 13, 22, 36, 51, 79, 88 };

        private static int GetIndex(Piece piece, int moves)
        {
            return PieceMobilityIndices[Move.Order(piece)] + moves;
        }

        public static Move[] GetMoves(BoardState board)
        {
            _moveGen.Next = 0;
            _moveGen.CollectQuiets(board);
            //collect ohter players move's
            board.SideToMove = (Color)(-(int)board.SideToMove);
            _moveGen.CollectQuiets(board);
            //undo changes to STM
            board.SideToMove = (Color)(-(int)board.SideToMove);
            Move[] result = new Move[_moveGen.Next];
            Array.Copy(_moveBuffer, result, result.Length);
            return result;
        }

        private static int[] _moveCounts = new int[64];

        internal static Feature[] GetFeatures(BoardState position, float phase)
        {
            Array.Clear(_moveCounts);
            Move[] moves = GetMoves(position);
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                //TODO: count captures? Count promotions as one?
                _moveCounts[move.FromSquare]++;
            }

            List<Feature> features = new List<Feature>();
            for (int i = 0; i < 64; i++)
            {
                Piece piece = position.GetPiece(i);
                if (piece == Piece.None)
                    continue;

                int value = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;
                int index = (short)GetIndex(piece, _moveCounts[i]);
                features.Add(new Feature
                {
                    Index = (short)(2 * index),
                    Value = value
                });
                if (phase == 0)
                    continue;
                features.Add(new Feature
                {
                    Index = (short)(2 * index + 1),
                    Value = value * phase
                });
            }
            return features.ToArray();
        }

        //*** DEBUG VERSION ***

        static Piece[] Pieces = new Piece[88];
        static ulong[] Sum = new ulong[88];

        internal static Feature[] _GetFeatures(BoardState position, float phase)
        {
            Array.Clear(_moveCounts);
            Move[] moves = GetMoves(position);
            for(int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                //TODO: count captures? Count promotions as one?
                _moveCounts[move.FromSquare]++;
            }
                        
            Console.WriteLine(Notation.GetFEN(position));
            List<Feature> features = new List<Feature>();
            for(int i = 0; i < 64; i++)
            {
                if (position.GetPiece(i) == Piece.None)
                    continue;
                features.Add(GetFeature(position.GetPiece(i), _moveCounts[i], phase));
            }
            Report();
            Console.WriteLine();

            //...make sure the debug version creates the same features
            var others = GetFeatures(position, phase);
            for (int i = 0; i < features.Count; i++)
                if (!features[i].Equals(others[i]))
                    throw new Exception();

            return features.ToArray();
        }

        public static void Report()
        {
            Report(Piece.Pawn);
            Report(Piece.Knight);
            Report(Piece.Bishop);
            Report(Piece.Rook);
            Report(Piece.Queen);
            Report(Piece.King);
        }

        private static void Report(Piece piece)
        {
            Console.Write($"{piece}: ");
            int i0 = Move.Order(piece);
            for(int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0+1]; i++)
            {
                Pieces[i] = piece;
                Console.Write(Sum[i]);
                Console.Write(" ");
            }
            Console.WriteLine();
        }

        internal static void Report(Piece piece, int offset, int step, float[] coefficients)
        {
            Console.Write($"{piece}: ");
            int i0 = Move.Order(piece);
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int c = (int)Math.Round(coefficients[offset + i * step]);
                Console.Write(c);
                Console.Write(" ");
            }
            Console.WriteLine();
        }

        private static Feature GetFeature(Piece piece, int moves, float phase)
        {
            int value = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;

            Console.WriteLine($"{piece} has {moves} in phase {phase}");
            short index = (short)GetIndex(piece, moves);

            if (Pieces[index] != (piece & Piece.TypeMask))
                throw new Exception();

            Sum[index]++;

            return new Feature
            {
                Index = index,
                Value = value
            };
        }
    }
}
