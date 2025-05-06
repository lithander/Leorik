using static Leorik.Core.Bitboard;
using System.Text;

namespace Leorik.Core
{
    public enum Variant
    {
        Standard,
        Chess960
    }

    public static class Notation
    {       
        const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public readonly static Move BlackCastlingShort = new(Piece.BlackKing | Piece.CastleShort, 60, 63, Piece.BlackRook);//e8g8
        public readonly static Move BlackCastlingLong = new(Piece.BlackKing | Piece.CastleLong, 60, 56, Piece.BlackRook);//e8c8
        public readonly static Move WhiteCastlingShort = new(Piece.WhiteKing | Piece.CastleShort, 4, 7, Piece.WhiteRook);//e1g1
        public readonly static Move WhiteCastlingLong = new(Piece.WhiteKing | Piece.CastleLong, 4, 0, Piece.WhiteRook);//e1c1

        public static string GetHex(ulong bitboard)
        {
            return $"0x{Convert.ToString((long)bitboard, 16).PadLeft(16, '0').ToUpperInvariant()}UL";
        }
        public static char GetChar(Piece piece)
        {
            return piece switch
            {
                Piece.WhitePawn => 'P',
                Piece.WhiteKnight => 'N',
                Piece.WhiteBishop => 'B',
                Piece.WhiteRook => 'R',
                Piece.WhiteQueen => 'Q',
                Piece.WhiteKing => 'K',
                Piece.BlackPawn => 'p',
                Piece.BlackKnight => 'n',
                Piece.BlackBishop => 'b',
                Piece.BlackRook => 'r',
                Piece.BlackQueen => 'q',
                Piece.BlackKing => 'k',
                _ => ' ',
            };
        }

        public static Piece GetPiece(char ascii)
        {
            return ascii switch
            {
                'P' => Piece.WhitePawn,
                'N' => Piece.WhiteKnight,
                'B' => Piece.WhiteBishop,
                'R' => Piece.WhiteRook,
                'Q' => Piece.WhiteQueen,
                'K' => Piece.WhiteKing,
                'p' => Piece.BlackPawn,
                'n' => Piece.BlackKnight,
                'b' => Piece.BlackBishop,
                'r' => Piece.BlackRook,
                'q' => Piece.BlackQueen,
                'k' => Piece.BlackKing,
                _ => throw new ArgumentException($"Piece character {ascii} not supported."),
            };
        }

        public static Piece GetPiece(char ascii, Color color) => GetPiece(ascii).OfColor(color);

        public static Piece OfColor(this Piece piece, Color color) => (piece & Piece.TypeMask) | (Piece)(color + 2);

        public static BoardState GetStartingPosition() => GetBoardState(STARTING_POS_FEN);

        public static BoardState GetBoardState(string fen)
        {
            BoardState result = new BoardState();
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            string[] fields = fen.Split();
            if (fields.Length < 4)
                throw new ArgumentException($"FEN needs at least 4 fields. Has only {fields.Length} fields.");

            //Place pieces on board
            string[] fenPosition = fields[0].Split('/');
            int rank = 7;
            foreach (string row in fenPosition)
            {
                int file = 0;
                foreach (char piece in row)
                {
                    if (char.IsNumber(piece))
                    {
                        int emptySquares = (int)char.GetNumericValue(piece);
                        file += emptySquares;
                    }
                    else
                    {
                        result.SetBit(rank * 8 + file, Notation.GetPiece(piece));
                        file++;
                    }
                }
                rank--;
            }

            //Set side to move
            result.SideToMove = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase) ? Color.White : Color.Black;

            //Set castling rights
            result.CastleFlags = ParseCastlingRights(fields[2], result.Rooks);
            if ((result.Rooks & result.CastleFlags) != result.CastleFlags)
                throw new Exception("Invalid CastleFlags, Rooks not found!");

            //Set en-passant square
            result.EnPassant = fields[3] == "-" ? 0 : 1UL << GetSquare(fields[3]);

            //Optional: Halfmove clock
            if (fields.Length >= 5 && byte.TryParse(fields[4], out byte halfmoves))
                result.HalfmoveClock = halfmoves;

            result.UpdateEval();
            result.UpdateHash();
            return result;
        }

        static ulong WhiteKingsideRookBit(ulong bbRooks) => HighestBit(bbRooks & 0xFFUL);
        static ulong WhiteQueensideRookBit(ulong bbRooks) => LowestBit(bbRooks & 0xFFUL);
        static ulong BlackKingsideRookBit(ulong bbRooks) => HighestBit(bbRooks & 0xFF00000000000000UL);
        static ulong BlackQueensideRookBit(ulong bbRooks) => LowestBit(bbRooks & 0xFF00000000000000UL);

        private static ulong ParseCastlingRights(string castlingField, ulong bbRooks)
        {
            ulong castleFlags = 0;
            if (castlingField == "-")
                return castleFlags;

            //We expect to be given a Shredder-FEN or X-FEN supporting Chess960 castling rights.
            //instead of KQkq, upper case (white) and lower case (black) file characters of the affected rooks
            int length = castlingField.Length;
            for (int i = 0; i < length; i++)
            {
                //castling rights provided by KQkq are related to the outermost rook of the affected side
                if (castlingField[i] == 'K')
                    castleFlags |= WhiteKingsideRookBit(bbRooks);
                else if (castlingField[i] == 'Q')
                    castleFlags |= WhiteQueensideRookBit(bbRooks);
                else if (castlingField[i] == 'k')
                    castleFlags |= BlackKingsideRookBit(bbRooks);
                else if (castlingField[i] == 'q')
                    castleFlags |= BlackQueensideRookBit(bbRooks);
                else
                {
                    //Map letters [a..h] to [0..7] with ASCII('a') == 97 or ASCII('A') == 65
                    int blackFile = castlingField[i] - 'a';
                    int whiteFile = castlingField[i] - 'A';
                    //only either black or white file is in range - set this rook's castling bit!
                    if (blackFile >= 0 && blackFile <= 7)
                        castleFlags |= 0x0100000000000000UL << blackFile;
                    else if (whiteFile >= 0 && whiteFile <= 7)
                        castleFlags |= 0x0000000000000001UL << whiteFile;
                }
            }
            return castleFlags;
        }

        public static string GetFen(BoardState board, int fullMoveNumber = 1)
        {
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation

            StringBuilder fen = new StringBuilder();
            //Piece placement is starting with rank 8 and ending with rank 1
            for (int rank = 7; rank >= 0; rank--)
            {
                //Scan piece placement from the A file to H file.
                int empty = 0;
                for (int file = 0; file <= 7; file++)
                {
                    int square = rank * 8 + file;
                    Piece piece = board.GetPiece(square);
                    //Consequtive empty fields are represented as an integer number [1..8]
                    if (piece == Piece.None)
                        empty++;
                    else
                    {
                        if (empty > 0)
                        {
                            fen.Append(empty);
                            empty = 0;
                        }
                        //Pieces are represtend as a single letter
                        fen.Append(GetChar(piece));
                    }
                }
                if (empty > 0)
                    fen.Append(empty);

                //Each rank is separated by the terminal symbol '/'(slash).
                if (rank > 0)
                    fen.Append('/');
            }

            //Side to move is either 'w' or 'b'
            if (board.SideToMove == Color.White)
                fen.Append(" w ");
            else
                fen.Append(" b ");

            //Castling rights
            if (board.CastleFlags == 0)
                fen.Append('-');
            else
                EncodeCastlingRights(fen, board);
            fen.Append(' ');

            if (board.EnPassant == 0)
                fen.Append('-');
            else
                fen.Append(GetSquareName(LSB(board.EnPassant)));

            //Halfmove Clock & Fullmove Counter
            fen.Append($" {board.HalfmoveClock} {fullMoveNumber}");

            return fen.ToString();
        }

        private static void EncodeCastlingRights(StringBuilder fen, BoardState board)
        {
            //White Kingside
            ulong bit = board.CastleFlags & board.White & ~MaskLow(board.Kings & board.White);
            if (bit > 0)
                fen.Append((bit & WhiteKingsideRookBit(board.Rooks)) > 0 ? 'K' : (char)(LSB(bit) % 8 + 'A'));
            //White Queenside
            bit = board.CastleFlags & board.White & MaskLow(board.Kings & board.White);
            if (bit > 0)
                fen.Append((bit & WhiteQueensideRookBit(board.Rooks)) > 0 ? 'Q' : (char)(LSB(bit) % 8 + 'A'));
            //Black Kingside
            bit = board.CastleFlags & board.Black & ~MaskLow(board.Kings & board.Black);
            if (bit > 0)
                fen.Append((bit & BlackKingsideRookBit(board.Rooks)) > 0 ? 'k' : (char)(LSB(bit) % 8 + 'a'));
            //Black Queenside
            bit = board.CastleFlags & board.Black & MaskLow(board.Kings & board.Black);
            if (bit > 0)
                fen.Append((bit & BlackQueensideRookBit(board.Rooks)) > 0 ? 'q' : (char)(LSB(bit) % 8 + 'a'));
        }

        public static string GetSquareName(int squareIndex)
        {
            //This is the reverse of the ToSquareIndex()
            int rank = squareIndex / 8;
            int file = squareIndex % 8;

            //Map file [0..7] to letters [a..h] and rank [0..7] to [1..8]
            string squareNotation = $"{(char)('a' + file)}{rank + 1}";
            return squareNotation;
        }

        public static int GetSquare(string squareNotation)
        {
            //Each square has a unique identification of file letter followed by rank number.
            //https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: White's king starts the game on square e1; Black's knight on b8 can move to open squares a6 or c6.

            //Map letters [a..h] to [0..7] with ASCII('a') == 97
            int file = squareNotation[0] - 'a';
            //Map numbers [1..8] to [0..7] with ASCII('1') == 49
            int rank = squareNotation[1] - '1';
            int index = rank * 8 + file;

            if (index >= 0 && index <= 63)
                return index;

            throw new ArgumentException($"The given square notation {squareNotation} does not map to a valid index between 0 and 63");
        }

        public static string GetMoveName(Move move, Variant variant)
        {
            if (variant == Variant.Standard && move.Flags >= Piece.CastleShort)
            {
                switch(move.Flags)
                {
                    case Piece.CastleShort | Piece.Black:
                        return "e8g8";
                    case Piece.CastleLong | Piece.Black:
                        return "e8c8";
                    case Piece.CastleShort | Piece.White:
                        return "e1g1";
                    case Piece.CastleLong | Piece.White:
                        return "e1c1";
                }
            }

            //result represents the move in the long algebraic notation (without piece names)
            string result = GetSquareName(move.FromSquare);
            result += GetSquareName(move.ToSquare);
            //the presence of a 5th character should mean promotion
            if (move.IsPromotion())
                result += char.ToLower(GetChar(move.NewPiece()));

            return result;
        }

        public static Move GetMove(BoardState board, string notation)
        {
            //trim check and checkmate symbols.
            notation = notation.TrimEnd('+', '#');

            //queenside castling
            if (notation == "O-O-O" || notation == "0-0-0")
            {
                if (board.SideToMove == Color.White)
                    return WhiteCastlingLong;
                else
                    return BlackCastlingLong;
            }

            //kingside castling
            if (notation == "O-O" || notation == "0-0")
            {
                if (board.SideToMove == Color.White)
                    return WhiteCastlingShort;
                else
                    return BlackCastlingShort;
            }

            //promotion
            Piece promotion = (notation[^2] == '=') ? GetPiece(notation[^1], board.SideToMove) : default;

            //pawns?
            if (char.IsLower(notation, 0))
            {
                Piece pawn = Piece.Pawn.OfColor(board.SideToMove);
                if (notation[1] == 'x')
                {
                    //pawn capture
                    int toSquare = GetSquare(notation.Substring(2, 2));
                    return IdentifyMove(board, pawn, toSquare, promotion, notation[0]);
                }
                else
                {
                    //pawn move
                    int toSquare = GetSquare(notation);
                    return IdentifyMove(board, pawn, toSquare, promotion);
                }
            }

            Piece piece = GetPiece(notation[0], board.SideToMove);
            if (notation[1] == 'x')
            {
                //capture
                int toSquare = GetSquare(notation.Substring(2, 2));
                return IdentifyMove(board, piece, toSquare, promotion);
            }
            else if (notation[2] == 'x')
            {
                //piece capture with disambiguation
                int toSquare = GetSquare(notation.Substring(3, 2));
                return IdentifyMove(board, piece, toSquare, promotion, notation[1]);
            }
            else if (notation.Length >= 4 && notation[3] == 'x')
            {
                int fromSquare = GetSquare(notation.Substring(1, 2));
                int toSquare = GetSquare(notation.Substring(4, 2));
                return new Move(piece, fromSquare, toSquare, promotion);
            }
            else if (notation.Length == 3)
            {
                //move
                int toSquare = GetSquare(notation.Substring(1, 2));
                return IdentifyMove(board, piece, toSquare, promotion);
            }
            else if (notation.Length == 4)
            {
                //move with disambiguation
                int toSquare = GetSquare(notation.Substring(2, 2));
                return IdentifyMove(board, piece, toSquare, promotion, notation[1]);
            }
            else if(notation.Length == 5)
            {
                //move with disambiguation e.g Ra1a2
                int fromSquare = GetSquare(notation.Substring(1, 2));
                int toSquare = GetSquare(notation.Substring(3, 2));
                return new Move(piece, fromSquare, toSquare, promotion);
            }

            throw new ArgumentException($"Move notation {notation} could not be parsed!");
        }

        private static Move IdentifyMove(BoardState board, Piece moving, int toSquare, Piece promotion, char? fileOrRank = null)
        {
            Move[] moves = new Move[225];
            var moveGen = new MoveGen(moves, 0);
            moveGen.CollectAll(board);
            for (int i = 0; i < moveGen.Next; i++)
            {
                Move move = moves[i];
                if (move.ToSquare != toSquare)
                    continue;
                if (move.MovingPiece() != move.NewPiece() && move.NewPiece() != promotion)
                    continue;
                if (board.GetPiece(move.FromSquare) != moving)
                    continue;
                if (fileOrRank != null && !GetSquareName(move.FromSquare).Contains(fileOrRank.Value))
                    continue;
                //make sure the move isn't illegal
                BoardState clone = board.Clone();
                if (!clone.PlayWithoutHashAndEval(board, ref move))
                    continue;

                return move; //this is the move!
            }
            throw new ArgumentException("No move meeting all requirements could be found!");
        }

        public static Move GetMoveUci(BoardState board, string uciMoveNotation, Variant variant)
        {
            if (uciMoveNotation.Length < 4)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too short!");
            if (uciMoveNotation.Length > 5)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too long!");

            Move[] moves = new Move[225];
            var moveGen = new MoveGen(moves, 0);
            moveGen.CollectAll(board);
            for (int i = 0; i < moveGen.Next; i++)
            {
                if (GetMoveName(moves[i], variant) == uciMoveNotation)
                    return moves[i];
            }
            throw new ArgumentException("No move meeting all requirements could be found!");
        }

        public static string GetEndgameClass(BoardState board)
        {
            StringBuilder sb = new StringBuilder();
            void Add(char c, ulong bb)
            {
                int cnt = PopCount(bb);
                while (cnt-- > 0) sb.Append(c);
            }
            void AddAll(ulong color)
            {
                Add('K', color & board.Kings);
                Add('Q', color & board.Queens);
                Add('R', color & board.Rooks);
                Add('B', color & board.Bishops);
                Add('N', color & board.Knights);
                Add('P', color & board.Pawns);
            }
            AddAll(board.White);
            sb.Append("v");
            AddAll(board.Black);
            return sb.ToString();
        }

        public static string PrintBitboard(ulong bits)
        {
            StringBuilder sb = new StringBuilder(64 * 3);
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int sq = (7 - i) * 8 + j;
                    bool bit = (bits & (1UL << sq)) != 0;
                    sb.Append(bit ? "O " : "- ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
