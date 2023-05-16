
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

        internal static void AddFeatures(float[] features, BoardState position, float phase, int offset)
        {
            int[] moveCounts = new int[64];
            Move[] moves = GetMoves(position);
            for (int i = 0; i < moves.Length; i++)
            {
                //TODO: count captures? Count promotions as one?
                moveCounts[moves[i].FromSquare]++;
            }

            for (int i = 0; i < 64; i++)
            {
                int moveCount = moveCounts[i];
                Piece piece = position.GetPiece(i);

                if (piece == Piece.None)
                    continue;
                if ((piece & Piece.TypeMask) == Piece.Knight)
                    continue;
                    //only blocked or promoting pawns are interesting
                if ((piece & Piece.TypeMask) == Piece.Pawn && moveCount > 0 && moveCount < 3)
                    continue;

                int value = (piece & Piece.ColorMask) == Piece.White ? 1 : -1;
                int order = Move.Order(piece);
                int index = offset + 2 * (PieceMobilityIndices[order] + moveCount);
                features[index] += value;
                features[index + 1] += value * phase;
            }
        }

        internal static void Report(Piece piece, int offset, float[] coefficients)
        {
            Console.WriteLine($"//{piece}: ");
            const int step = 2;
            int order = Move.Order(piece);
            for (int i = PieceMobilityIndices[order]; i < PieceMobilityIndices[order + 1]; i++)
            {
                int k = offset + i * step;
                float mg = (int)Math.Round(coefficients[k]);
                float eg = (int)Math.Round(coefficients[k + 1]);
                Console.Write($"({mg},{eg}), ");
            }
            Console.WriteLine();
        }

        internal static (int mg, int eg) Rebalance(Piece piece, int offset, float[] coefficients)
        {
            const int step = 2;
            int order = Move.Order(piece);
            int i0 = PieceMobilityIndices[order];
            int iNext = PieceMobilityIndices[order + 1];
            int iBase = i0 + (iNext - i0) / 2;
            int mg = (int)coefficients[offset + iBase * step];
            int eg = (int)coefficients[offset + iBase * step + 1];

            for (int i = i0; i < iNext; i++)
            {
                int k = offset + i * step;
                coefficients[k] -= mg;
                coefficients[k + 1] -= eg;
            }
            return (mg, eg);
        }
    }
}
