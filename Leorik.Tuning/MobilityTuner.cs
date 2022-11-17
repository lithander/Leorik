
using Leorik.Core;

namespace Leorik.Tuning
{
    static class MobilityTuner
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
                if ((piece & Piece.TypeMask) == Piece.Knight)
                    continue;
                //only blocked or promoting pawns are interesting
                if ((piece & Piece.TypeMask) == Piece.Pawn && _moveCounts[i] > 0 && _moveCounts[i] < 4)
                    continue;

                int value = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;
                int index = (short)GetIndex(piece, _moveCounts[i]);
                features.AddFeature(index, value, phase);
            }
            return features.ToArray();
        }
                
        internal static void Report(Piece piece, int table, bool endgame, float[] coefficients)
        {
            Console.WriteLine($"//{piece}: ");
            const int step = 2;
            int offset = table * 128 + (endgame ? 1 : 0);
            int i0 = Move.Order(piece);
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int c = (int)Math.Round(coefficients[offset + i * step]);
                Console.Write(c);
                Console.Write(", ");
            }
            Console.WriteLine();
        }
    }
}
