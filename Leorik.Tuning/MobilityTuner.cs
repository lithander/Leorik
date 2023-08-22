
using Leorik.Core;
using static Leorik.Core.Bitboard;

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

        public const int Dimensions = 2;
        public const int MobilityWeights = Dimensions * 88;

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

        const int Pawn = 0;
        //const int Knight = 13;
        const int Bishop = 22;
        const int Rook = 36;
        const int Queen = 51;
        const int King = 79;

        internal static void AddFeatures(float[] features, BoardState board, float phase, int offset)
        {
            void Add(int value, int index, float phase)
            {
                index = offset + 2 * index;
                features[index] += value;
                features[index + 1] += value * phase;
            }

            ulong occupied = board.Black | board.White;

            //Kings
            int square = LSB(board.Kings & board.Black);
            int moves = PopCount(KingTargets[square] & ~occupied);
            Add(-1, King + moves, phase);

            square = LSB(board.Kings & board.White);
            moves = PopCount(KingTargets[square] & ~occupied);
            Add(1, King + moves, phase);

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square));
                Add(-1, Bishop + moves, phase);
            }
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = ClearLSB(bishops))
            {
                square = LSB(bishops);
                moves = PopCount(GetBishopTargets(occupied, square));
                Add(1, Bishop + moves, phase);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square));
                Add(-1, Rook + moves, phase);
            }
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = ClearLSB(rooks))
            {
                square = LSB(rooks);
                moves = PopCount(GetRookTargets(occupied, square));
                Add(1, Rook + moves, phase);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square));
                Add(-1, Queen + moves, phase);
            }
            for (ulong queens = board.Queens & board.White; queens != 0; queens = ClearLSB(queens))
            {
                square = LSB(queens);
                moves = PopCount(GetQueenTargets(occupied, square));
                Add(1, Queen + moves, phase);
            }

            //Black Pawns
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //not able to move one square down
            int blocked = PopCount(blackPawns) - PopCount(oneStep);
            Add(-blocked, Pawn, phase);
            //promotion square not blocked?
            int promo = PopCount(oneStep & 0x00000000000000FFUL);
            Add(-promo, Pawn + 3, phase);


            //White Pawns
            ulong whitePawns = board.Pawns & board.White;
            oneStep = (whitePawns << 8) & ~occupied;
            //not able to move one square up
            blocked = PopCount(whitePawns) - PopCount(oneStep);
            Add(blocked, Pawn, phase);
            //promotion square not blocked?
            promo = PopCount(oneStep & 0xFF00000000000000UL);
            Add(promo, Pawn + 3, phase);
        }

        internal static void DescribeFeaturePairs(int[] featurePairs, int offset)
        {
            for (int order = Move.Order(Piece.Pawn); order < Move.Order(Piece.Queen); order++)
            {
                int zeroMoves = PieceMobilityIndices[order];
                int maxMoves = PieceMobilityIndices[order - 1];
                for (int tuple = zeroMoves; tuple < maxMoves; tuple++)
                {
                    int index = offset + 2 * tuple;
                    //mobility features are tuples of two elements
                    featurePairs[index + 1] = index;
                }
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
