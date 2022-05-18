using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct PawnEval
    {
        const int ENTRY_COUNT = 4999; //prime!

        public short Base;
        public short Endgame;

        public PawnEval(BoardState pos) : this()
        {
            ulong isolated = PawnStructure.GetIsolatedPawns(pos);
            
            for (ulong bits = isolated & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
                AddIsolatedPawn(-1, Bitboard.LSB(bits));

            for (ulong bits = isolated & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
                AddIsolatedPawn(+1, Bitboard.LSB(bits) ^ 56);

            ulong passed = PawnStructure.GetPassedPawns(pos);

            for (ulong bits = passed & pos.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
                AddPassedPawn(-1, Bitboard.LSB(bits));

            for (ulong bits = passed & pos.White; bits != 0; bits = Bitboard.ClearLSB(bits))
                AddPassedPawn(+1, Bitboard.LSB(bits) ^ 56);
        }

        private void AddIsolatedPawn(int sign, int square)
        {
            Base += (short)(sign * -14);
            //_baseScore += (short)(sign * IsolatedPawnsMidgame[square]);
            //_endgameScore += (short)(sign * IsolatedPawnsEndgame[square]);
        }

        private void AddPassedPawn(int sign, int square)
        {
            int rank = 8 - (square >> 3);
            int file = square & 7;
            int center = Math.Min(file, 7 - file);
            //_baseScore += (short)(sign * 10);
            Endgame += (short)(sign * (rank * 15 - 12 * center));
        }

        public int GetScore(float phase)
        {
            return (int)(Base + phase * Endgame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board, ref Move move)
        {
            if ((move.MovingPiece() & Piece.TypeMask) == Piece.Pawn ||
                (move.CapturedPiece() & Piece.TypeMask) == Piece.Pawn)
                Update(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(BoardState board)
        {
            ulong index = board.Pawns % ENTRY_COUNT;
            if (!PawnHashTable[index].GetEval(board, ref this))
            {
                this = new PawnEval(board);
                PawnHashTable[index] = new PawnHashEntry(this, board);
            }
        }

        static PawnHashEntry[] PawnHashTable = new PawnHashEntry[ENTRY_COUNT];

        struct PawnHashEntry
        {
            public PawnEval Eval;
            public ulong BlackPawns;
            public ulong WhitePawns;

            public PawnHashEntry(PawnEval pawns, BoardState board) : this()
            {
                Eval = pawns;
                BlackPawns = board.Black & board.Pawns;
                WhitePawns = board.White & board.Pawns;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool GetEval(BoardState board, ref PawnEval eval)
            {
                if ((board.Black & board.Pawns) != BlackPawns)
                    return false;
                if ((board.White & board.Pawns) != WhitePawns)
                    return false;

                eval = Eval;
                return true;
            }
        }
    }

    public class PawnStructure
    {
        public static readonly ulong[] NeighbourFiles =
        {
            0x0202020202020202UL, 0x0505050505050505UL,
            0x0A0A0A0A0A0A0A0AUL, 0x1414141414141414UL,
            0x2828282828282828UL, 0x5050505050505050UL,
            0xA0A0A0A0A0A0A0A0UL, 0x4040404040404040UL
        };

        public static bool IsIsolatedPawn(ulong pawns, int square)
        {
            int file = square & 7;
            return (pawns & NeighbourFiles[file]) == 0;
        }

        public static ulong GetIsolatedPawns(BoardState board, Color color)
        {
            ulong mask = color == Color.Black ? board.Black : board.White;
            ulong pawns = mask & board.Pawns;
            ulong result = 0;
            for (ulong bits = pawns; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                if (IsIsolatedPawn(pawns, square))
                    result |= 1UL << square;
            }
            return result;
        }

        public static ulong GetIsolatedPawns(BoardState board)
        {
            return GetIsolatedPawns(board, Color.Black) | GetIsolatedPawns(board, Color.White);
        }

        public static ulong GetPassedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong whiteFront = Up(FillUp(board.White & board.Pawns));
                return board.Black & board.Pawns & ~(whiteFront | Left(whiteFront) | Right(whiteFront));
            }
            else //White
            {
                ulong blackFront = Down(FillDown(board.Black & board.Pawns));
                return board.White & board.Pawns & ~(blackFront | Left(blackFront) | Right(blackFront));
            }
        }

        public static ulong GetPassedPawns(BoardState board)
        {
            return GetPassedPawns(board, Color.Black) | GetPassedPawns(board, Color.White);
        }

        public static ulong GetDoubledPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackRear = Up(FillUp(board.Black & board.Pawns));
                return board.Black & board.Pawns & blackRear;
            }
            else //White
            {
                ulong whiteRear = Down(FillDown(board.White & board.Pawns));
                return board.White & board.Pawns & whiteRear;
            }
        }

        public static ulong GetProtectedPawns(BoardState board)
        {
            return GetProtectedPawns(board, Color.Black) | GetProtectedPawns(board, Color.White);
        }

        public static ulong GetProtectedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackPawns = board.Black & board.Pawns;
                //capture left
                ulong captureLeft = (blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9;
                ulong captureRight = (blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7;
                return (captureLeft | captureRight) & blackPawns;
            }
            else //White
            {
                ulong whitePawns = board.White & board.Pawns;
                ulong captureRight = (whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9;
                ulong captureLeft = (whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7;
                return (captureRight | captureLeft) & whitePawns;
            }
        }

        public static ulong GetConnectedOrProtected(BoardState board)
        {
            return GetConnectedPawns(board) | GetProtectedPawns(board);
        }

        public static ulong GetConnectedPassedPawns(BoardState board)
        {
            return GetConnectedPawns(board) & GetPassedPawns(board);
        }

        public static ulong GetConnectedPawns(BoardState board)
        {
            return GetConnectedPawns(board, Color.Black) | GetConnectedPawns(board, Color.White);
        }

        public static ulong GetConnectedPawns(BoardState board, Color color)
        {
            if (color == Color.Black)
            {
                ulong blackPawns = board.Black & board.Pawns;
                return (Left(blackPawns) ^ Right(blackPawns)) & blackPawns;
            }
            else //White
            {
                ulong whitePawns = board.White & board.Pawns;
                return (Left(whitePawns) ^ Right(whitePawns)) & whitePawns;
            }
        }

        public static ulong GetDoubledPawns(BoardState board)
        {
            return GetDoubledPawns(board, Color.Black) | GetDoubledPawns(board, Color.White);
        }

        private static ulong FillUp(ulong bits)
        {
            bits |= (bits << 8);
            bits |= (bits << 16);
            bits |= (bits << 32);
            return bits;
        }

        private static ulong FillDown(ulong bits)
        {
            bits |= (bits >> 8);
            bits |= (bits >> 16);
            bits |= (bits >> 32);
            return bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Up(ulong bits) => bits << 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Down(ulong bits) => bits >> 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Left(ulong bits) => (bits & 0xFEFEFEFEFEFEFEFEUL) >> 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Right(ulong bits) => (bits & 0x7F7F7F7F7F7F7F7FUL) << 1;
    }
}
