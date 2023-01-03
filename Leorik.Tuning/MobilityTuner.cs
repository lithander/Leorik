
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

        internal static void Report(Piece piece, int offset, float[] coefficients)
        {
            Console.WriteLine($"//{piece}: ");
            const int step = 2;
            int i0 = Move.Order(piece);
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int k = offset + i * step;
                float mg = (int)Math.Round(coefficients[k]);
                float eg = (int)Math.Round(coefficients[k + 1]);
                Console.Write($"({mg},{eg}), ");
            }
            Console.WriteLine();
        }

        //Score of Leorik-2.3X vs Leorik-2.3: 1236 - 1456 - 2020  [0.477] 4712
        //Elo difference: -16.2 +/- 7.5, LOS: 0.0 %, DrawRatio: 42.9 %
        private static (int a, int b) Average(Piece piece, int offset, float[] coefficients)
        {
            const int step = 2;
            int i0 = Move.Order(piece);
            float mgSum = 0; float egSum = 0;
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int k = offset + i * step;
                mgSum += coefficients[k];
                egSum += coefficients[k + 1];
            }
            int divisor = PieceMobilityIndices[i0 + 1] - PieceMobilityIndices[i0];
            return ((int)(mgSum / divisor), (int)(egSum / divisor));
        }

        //Score of Leorik-2.3X vs Leorik-2.3: 1798 - 1986 - 2651  [0.485] 6435
        //Elo difference: -10.2 +/- 6.5, LOS: 0.1 %, DrawRatio: 41.2 %
        private static (int a, int b) Baseline(Piece piece, int offset, float[] coefficients)
        {
            const int step = 2;
            int i0 = Move.Order(piece);
            int mgMin = int.MaxValue; int egMin = int.MaxValue;
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int k = offset + i * step;
                mgMin = (int)Math.Min(mgMin, coefficients[k]);
                egMin = (int)Math.Min(egMin, coefficients[k + 1]);
            }
            return (mgMin, egMin);
        }

        internal static (int mg, int eg) Rebalance(Piece piece, int offset, float[] coefficients)
        {
            (int mg, int eg) = Baseline(piece, offset, coefficients);
            const int step = 2;
            int i0 = Move.Order(piece);
            for (int i = PieceMobilityIndices[i0]; i < PieceMobilityIndices[i0 + 1]; i++)
            {
                int k = offset + i * step;
                coefficients[k] -= mg;
                coefficients[k + 1] -= eg;
            }
            return (mg, eg);
        }

        //Score of Leorik-2.3X vs Leorik-2.3: 2673 - 2431 - 3902  [0.513] 9006
        //Elo difference: 9.3 +/- 5.4, LOS: 100.0 %, DrawRatio: 43.3 %
        internal static (int mg, int eg) Rebalance2(Piece piece, int offset, float[] coefficients)
        {
            const int step = 2;
            int order = Move.Order(piece);
            int i0 = PieceMobilityIndices[order];
            int iNext = PieceMobilityIndices[order + 1];
            int iBase = i0 + (iNext - i0) / 3;
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
