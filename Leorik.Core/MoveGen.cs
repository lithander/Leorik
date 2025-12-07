using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct MoveGen
    {
        //*** STATIC ***

        private static readonly Move[] _moveBuffer = new Move[256];

        public static Move[] GetLegalMoves(BoardState board)
        {
            MoveGen moveGen = new MoveGen(_moveBuffer, 0);
            moveGen.CollectAll(board);
            BoardState tempBoard = new BoardState();
            int to = 0;
            for(int i = 0; i < moveGen.Next; i++)
            {
                if(tempBoard.Play(board, ref _moveBuffer[i]))
                    _moveBuffer[to++] = _moveBuffer[i];
            }
            Move[] result = new Move[to];
            Array.Copy(_moveBuffer, result, result.Length);
            return result;
        }

        //**************

        private readonly Move[] _moves;
        public int Next;

        public MoveGen(Move[] moves, int nextIndex)
        {
            _moves = moves;
            Next = nextIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CollectMove(Move move)
        {
            int oldNext = Next;
            if (move != default)
                _moves[Next++] = move;

            return oldNext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CollectAll(BoardState board)
        {
            int oldNext = Next;
            if (board.SideToMove == Color.White)
            {
                CollectWhiteCaptures(board);
                CollectWhiteQuiets(board);
            }
            else
            {
                CollectBlackCaptures(board);
                CollectBlackQuiets(board);
            }
            return oldNext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CollectCaptures(BoardState board)
        {
            int oldNext = Next;
            if (board.SideToMove == Color.White)
                CollectWhiteCaptures(board);
            else
                CollectBlackCaptures(board);

            return oldNext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CollectQuiets(BoardState board)
        {
            int oldNext = Next;
            if (board.SideToMove == Color.White)
                CollectWhiteQuiets(board);
            else
                CollectBlackQuiets(board);

            return oldNext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(Move move)
        {
            _moves[Next++] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(Piece flags, int from, int to)
        {
            _moves[Next++] = new Move(flags, from, to, Piece.None);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAll(Piece piece, int square, ulong targets)
        {
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                Add(piece, square, Bitboard.LSB(targets));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnMove(Piece flags, ulong moveTargets, int offset)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            Add(flags, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAllCaptures(Piece piece, int square, ulong targets, BoardState board)
        {
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                AddCapture(piece, square, Bitboard.LSB(targets), board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCapture(Piece flags, int from, int to, BoardState board)
        {
            _moves[Next++] = new Move(flags, from, to, board.GetPiece(to));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnCapture(Piece flags, ulong moveTargets, int offset, BoardState board)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            AddCapture(flags, from, to, board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnCapturePromotions(Piece flags, ulong moveTargets, int offset, BoardState board)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            Piece target = board.GetPiece(to);
            _moves[Next++] = new Move(flags | Piece.QueenPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.RookPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.BishopPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.KnightPromotion, from, to, target);
        }

        private void CollectBlackCaptures(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            //can't move on squares occupied by side to move
            AddAllCaptures(Piece.BlackKing, square, Bitboard.KingTargets[square] & board.White, board);

            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAllCaptures(Piece.BlackKnight, square, Bitboard.KnightTargets[square] & board.White, board);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAllCaptures(Piece.BlackBishop, square, Bitboard.GetBishopTargets(occupied, square) & board.White, board);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAllCaptures(Piece.BlackRook, square, Bitboard.GetRookTargets(occupied, square) & board.White, board);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAllCaptures(Piece.BlackQueen, square, Bitboard.GetQueenTargets(occupied, square) & board.White, board);
            }

            //Pawns & Castling
            ulong targets;
            ulong blackPawns = board.Pawns & board.Black;

            //capture left
            ulong captureLeft = ((blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9) & board.White;
            for (targets = captureLeft & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.BlackPawn, targets, +9, board);

            //capture left to first rank and promote
            for (targets = captureLeft & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.BlackPawn, targets, +9, board);

            //capture right
            ulong captureRight = ((blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7) & board.White;
            for (targets = captureRight & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.BlackPawn, targets, +7, board);

            //capture right to first rank and promote
            for (targets = captureRight & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.BlackPawn, targets, +7, board);

            //is en-passent possible?
            captureLeft = ((blackPawns & 0x00000000FE000000UL) >> 9) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureLeft, +9);

            captureRight = ((blackPawns & 0x000000007F000000UL) >> 7) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureRight, +7);

            //move up and promote to Queen
            for (targets = (blackPawns >> 8) & ~occupied & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.Black | Piece.QueenPromotion, targets, +8);
        }

        private void CollectBlackQuiets(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            ulong king = board.Kings & board.Black;
            int square = Bitboard.LSB(king);
            //can't move on squares occupied by side to move
            AddAll(Piece.BlackKing, square, Bitboard.KingTargets[square] & ~occupied);
                        
            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAll(Piece.BlackKnight, square, Bitboard.KnightTargets[square] & ~occupied);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAll(Piece.BlackBishop, square, Bitboard.GetBishopTargets(occupied, square) & ~occupied);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAll(Piece.BlackRook, square, Bitboard.GetRookTargets(occupied, square) & ~occupied);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAll(Piece.BlackQueen, square, Bitboard.GetQueenTargets(occupied, square) & ~occupied);
            }

            //Pawns & Castling
            ulong targets;
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //move one square down
            for (targets = oneStep & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +8);

            //move to first rank and under-promote
            for (targets = oneStep & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
            {
                PawnMove(Piece.Black | Piece.RookPromotion, targets, +8);
                PawnMove(Piece.Black | Piece.BishopPromotion, targets, +8);
                PawnMove(Piece.Black | Piece.KnightPromotion, targets, +8);
            }

            //move two squares down
            ulong twoStep = (oneStep >> 8) & ~occupied;
            for (targets = twoStep & 0x000000FF00000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +16);

            //Chess960 Castling Requirements:
            //1.) Neither King nor Rook has moved. (CastleFlags are set)
            //2.) All squares between the castling King's initial and final squares (inclusive), and all of the squares between the castling Rook's initial and final squares (inclusive) must be vacant except for the King and involved Rook
            //3.) No square through which the King moves including starting and final square is under enemy attack.
            square = Bitboard.LSB(king);
            ulong leftRook = Bitboard.LowerBits(square) & 0xFF00000000000000UL & board.CastleFlags;
            if (leftRook != 0)
            {
                int leftRookSquare = Bitboard.LSB(leftRook);
                ulong vacancy = Bitboard.BitsBetween(leftRookSquare, 59) | Bitboard.BitsBetween(square, 58);
                if (((occupied ^ king ^ leftRook) & vacancy) == 0 && !IsAttackedByWhite(board, Bitboard.BitsBetween(square, 58)))
                    AddCapture(Piece.BlackRook | Piece.CastleLong, square, leftRookSquare, board);
            }
            ulong rightRook = Bitboard.HigherBits(square) & board.CastleFlags;
            if (rightRook != 0)
            {
                int rightRookSquare = Bitboard.LSB(rightRook);
                ulong vacancy = Bitboard.BitsBetween(rightRookSquare, 61) | Bitboard.BitsBetween(square, 62);
                if (((occupied ^ king ^ rightRook) & vacancy) == 0 && !IsAttackedByWhite(board, Bitboard.BitsBetween(square, 62))) //Rule 2 && Rule 3
                    AddCapture(Piece.BlackRook | Piece.CastleShort, square, rightRookSquare, board); //in Chess960 castling moves are sent in the form king "takes" his own rook
            }
        }

        private void CollectWhiteCaptures(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.White);
            //can't move on squares occupied by side to move
            AddAllCaptures(Piece.WhiteKing, square, Bitboard.KingTargets[square] & board.Black, board);

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAllCaptures(Piece.WhiteKnight, square, Bitboard.KnightTargets[square] & board.Black, board);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAllCaptures(Piece.WhiteBishop, square, Bitboard.GetBishopTargets(occupied, square) & board.Black, board);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAllCaptures(Piece.WhiteRook, square, Bitboard.GetRookTargets(occupied, square) & board.Black, board);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAllCaptures(Piece.WhiteQueen, square, Bitboard.GetQueenTargets(occupied, square) & board.Black, board);
            }

            //Pawns                
            ulong targets;
            ulong whitePawns = board.Pawns & board.White;

            //capture left
            ulong captureLeft = ((whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7) & board.Black;
            for (targets = captureLeft & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.WhitePawn, targets, -7, board);

            //capture left to last rank and promote
            for (targets = captureLeft & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.WhitePawn, targets, -7, board);

            //capture right
            ulong captureRight = ((whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9) & board.Black;
            for (targets = captureRight & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.WhitePawn, targets, -9, board);

            //capture right to last rank and promote
            for (targets = captureRight & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.WhitePawn, targets, -9, board);

            //is en-passent possible?
            captureLeft = ((whitePawns & 0x000000FE00000000UL) << 7) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureLeft, -7);

            captureRight = ((whitePawns & 0x000007F00000000UL) << 9) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureRight, -9);

            //move up and promote Queen
            for (targets = (whitePawns << 8) & ~occupied & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.White | Piece.QueenPromotion, targets, -8);
        }

        private void CollectWhiteQuiets(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            ulong king = board.Kings & board.White;
            int square = Bitboard.LSB(king);
            //can't move on squares occupied by side to move
            AddAll(Piece.WhiteKing, square, Bitboard.KingTargets[square] & ~occupied);

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAll(Piece.WhiteKnight, square, Bitboard.KnightTargets[square] & ~occupied);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAll(Piece.WhiteBishop, square, Bitboard.GetBishopTargets(occupied, square) & ~occupied);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAll(Piece.WhiteRook, square, Bitboard.GetRookTargets(occupied, square) & ~occupied);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAll(Piece.WhiteQueen, square, Bitboard.GetQueenTargets(occupied, square) & ~occupied);
            }

            //Pawns                
            ulong targets;
            ulong whitePawns = board.Pawns & board.White;
            ulong oneStep = (whitePawns << 8) & ~occupied;
            //move one square up
            for (targets = oneStep & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -8);

            //move to first rank and under-promote
            for (targets = oneStep & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
            {
                PawnMove(Piece.White | Piece.RookPromotion, targets, -8);
                PawnMove(Piece.White | Piece.BishopPromotion, targets, -8);
                PawnMove(Piece.White | Piece.KnightPromotion, targets, -8);
            }

            //move two squares up
            ulong twoStep = (oneStep << 8) & ~occupied;
            for (targets = twoStep & 0x00000000FF000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -16);

            //Castling (CollectBlackQuiets has more comments)
            square = Bitboard.LSB(king);
            ulong leftRook = Bitboard.LowerBits(square) & board.CastleFlags;
            if (leftRook != 0)
            {
                int leftRookSquare = Bitboard.LSB(leftRook);
                ulong vacancy = Bitboard.BitsBetween(leftRookSquare, 3) | Bitboard.BitsBetween(square, 2);
                if (((occupied ^ king ^ leftRook) & vacancy) == 0 && !IsAttackedByBlack(board, Bitboard.BitsBetween(square, 2)))
                    AddCapture(Piece.WhiteRook | Piece.CastleLong, square, leftRookSquare, board);
            }

            ulong rightRook = Bitboard.HigherBits(square) & 0x00000000000000FF & board.CastleFlags;
            if (rightRook != 0)
            {
                int rightRookSquare = Bitboard.LSB(rightRook);
                ulong vacancy = Bitboard.BitsBetween(rightRookSquare, 5) | Bitboard.BitsBetween(square, 6);
                if (((occupied ^ king ^ rightRook) & vacancy) == 0 && !IsAttackedByBlack(board, Bitboard.BitsBetween(square, 6)))
                    AddCapture(Piece.WhiteRook | Piece.CastleShort, square, rightRookSquare, board);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAttackedByWhite(BoardState board, ulong mask)
        {
            for (; mask != 0; mask = Bitboard.ClearLSB(mask))
            {
                int square = Bitboard.LSB(mask);
                if(board.IsAttackedByWhite(square))
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAttackedByBlack(BoardState board, ulong mask)
        {
            for (; mask != 0; mask = Bitboard.ClearLSB(mask))
            {
                int square = Bitboard.LSB(mask);
                if (board.IsAttackedByBlack(square))
                    return true;
            }
            return false;
        }
    }
}
