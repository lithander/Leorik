using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        private short _baseScore;
        private short _endgameScore;
        private short _phaseValue;
        private short _blackKingSafetyScore;
        private short _whiteKingSafetyScore;

        public short Score { get; private set; }

        //TODO: don't export interna for the sake of the Tuner
        public float P => (float)Phase(_phaseValue);
        public short MG => _baseScore;
        public short EG => _endgameScore;

        public Evaluation(BoardState board) : this()
        {
            AddPieces(board);
            Score = (short)CombineScores(board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPieces(BoardState board)
        {
            ulong occupied = board.Black | board.White;
            for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                Piece piece = board.GetPiece(square);
                AddPiece(piece, square);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Update(ref Move move, BoardState board)
        {
            RemovePiece(move.MovingPiece(), move.FromSquare);
            AddPiece(move.NewPiece(), move.ToSquare);

            if (move.CapturedPiece() != Piece.None)
                RemovePiece(move.CapturedPiece(), move.ToSquare);

            switch (move.Flags)
            {
                case Piece.EnPassant | Piece.BlackPawn:
                    RemovePiece(Piece.WhitePawn, move.ToSquare + 8);
                    break;
                case Piece.EnPassant | Piece.WhitePawn:
                    RemovePiece(Piece.BlackPawn, move.ToSquare - 8);
                    break;
                case Piece.CastleShort | Piece.Black:
                    RemovePiece(Piece.BlackRook, 63);
                    AddPiece(Piece.BlackRook, 61);
                    break;
                case Piece.CastleLong | Piece.Black:
                    RemovePiece(Piece.BlackRook, 56);
                    AddPiece(Piece.BlackRook, 59);
                    break;
                case Piece.CastleShort | Piece.White:
                    RemovePiece(Piece.WhiteRook, 7);
                    AddPiece(Piece.WhiteRook, 5);
                    break;
                case Piece.CastleLong | Piece.White:
                    RemovePiece(Piece.WhiteRook, 0);
                    AddPiece(Piece.WhiteRook, 3);
                    break;
            }

            Score = (short)CombineScores(board);
            //int refScore = new Evaluation(board).Score;
            //int error = Math.Abs(Score - refScore);
            //if (error > 0)
            //    throw new Exception(error.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CombineScores()
        {
            return _baseScore + Phase(_phaseValue) * _endgameScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CombineScores(BoardState board)
        {
            int blackKingSquare = Bitboard.LSB(board.Kings & board.Black);
            int whiteKingSquare = Bitboard.LSB(board.Kings & board.White) ^ 56;
            double wk = KingPhaseTable[whiteKingSquare];
            double bk = KingPhaseTable[blackKingSquare];
            return _baseScore + Phase(_phaseValue) * _endgameScore + bk * _blackKingSafetyScore + wk * _whiteKingSafetyScore;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue += PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                AddWhiteScore(pieceIndex, squareIndex ^ 56);
            else
                SubtractBlackScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue -= PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                SubtractWhiteScore(pieceIndex, squareIndex ^ 56);
            else
                AddBlackScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddBlackScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _baseScore += MidgameTables[tableIndex];
            _endgameScore += EndgameTables[tableIndex];
            _blackKingSafetyScore += KingSafetyTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubtractBlackScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _baseScore -= MidgameTables[tableIndex];
            _endgameScore -= EndgameTables[tableIndex];
            _blackKingSafetyScore -= KingSafetyTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddWhiteScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _baseScore += MidgameTables[tableIndex];
            _endgameScore += EndgameTables[tableIndex];
            _whiteKingSafetyScore += KingSafetyTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubtractWhiteScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _baseScore -= MidgameTables[tableIndex];
            _endgameScore -= EndgameTables[tableIndex];
            _whiteKingSafetyScore -= KingSafetyTables[tableIndex];
        }

        public const int CheckmateBase = 9000;
        public const int CheckmateScore = 9999;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMateDistance(int score)
        {
            int plies = CheckmateScore - Math.Abs(score);
            int moves = (plies + 1) / 2;
            return moves;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmate(int score) => Math.Abs(score) > CheckmateBase;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Checkmate(Color color, int ply) => (int)color * (ply - CheckmateScore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Checkmate(int ply) => (ply - CheckmateScore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Phase(short phaseValue)
        {
            return Math.Clamp((double)(PhaseSum - phaseValue) / PhaseSum, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        public static readonly int PhaseSum = 5000;
        public static readonly short[] PhaseValues = new short[6] { 0, 77, 317, 382, 946, 0 };

        //Tuned on 'data/quiet-labeled.epd'
        //500 Iterations. Material MSE = 0,245998296203
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24599673230870314
        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          154,  205,  132,  172,  159,  222,  121,   51,
           63,   80,  105,  108,  147,  137,  102,   57,
           51,   84,   81,   95,   97,   86,   93,   51,
           40,   69,   69,   86,   91,   82,   87,   50,
           42,   67,   69,   64,   78,   81,  110,   63,
           33,   71,   53,   50,   59,  101,  114,   53,
          100,  100,  100,  100,  100,  100,  100,  100,

          140,  229,  246,  281,  350,  233,  298,  204,
          252,  272,  396,  351,  346,  385,  319,  313,
          290,  383,  363,  392,  418,  444,  402,  374,
          311,  345,  344,  381,  365,  399,  348,  345,
          312,  326,  340,  339,  355,  348,  346,  319,
          304,  316,  339,  336,  346,  345,  351,  311,
          293,  267,  313,  323,  324,  343,  308,  314,
          208,  306,  266,  288,  305,  300,  309,  299,

          329,  337,  256,  251,  309,  288,  317,  335,
          330,  369,  342,  323,  384,  391,  368,  315,
          339,  381,  390,  394,  383,  404,  390,  356,
          349,  360,  373,  405,  393,  393,  363,  353,
          345,  372,  367,  376,  388,  365,  361,  353,
          351,  367,  367,  369,  366,  381,  369,  360,
          356,  368,  366,  353,  361,  371,  385,  349,
          309,  348,  340,  333,  339,  342,  311,  327,

          464,  491,  455,  509,  505,  454,  439,  461,
          459,  467,  494,  501,  515,  526,  465,  484,
          421,  446,  460,  462,  450,  483,  496,  449,
          403,  417,  434,  461,  453,  463,  434,  421,
          390,  399,  418,  426,  434,  427,  446,  413,
          385,  406,  417,  412,  435,  433,  436,  406,
          383,  416,  410,  421,  431,  442,  426,  366,
          410,  417,  431,  440,  443,  429,  406,  412,

          859,  871,  914,  867,  970,  965,  935,  933,
          878,  860,  893,  902,  888,  964,  920,  950,
          887,  886,  910,  910,  935,  978,  958,  962,
          863,  870,  885,  884,  898,  919,  896,  898,
          889,  870,  889,  887,  894,  897,  902,  899,
          876,  901,  886,  893,  891,  899,  912,  899,
          862,  886,  910,  897,  904,  911,  898,  905,
          891,  876,  886,  908,  878,  869,  870,  839,

          -39,   63,   52,   12,  -50,  -38,   30,   16,
           31,   68,   37,   83,   16,    0,  -28,  -53,
           31,   46,   75,   12,   33,   63,   76,  -23,
            4,  -10,    2,  -26,  -30,  -35,  -19,  -84,
          -42,   11,  -29,  -94,  -86,  -54,  -60,  -75,
           10,   19,  -16,  -46,  -56,  -43,   -1,  -13,
           31,   34,    4,  -57,  -35,   -4,   39,   46,
           20,   70,   39,  -48,   26,  -15,   57,   53,
        };

        public static short[] EndgameTables = new short[6 * 64]{
            0,    0,    0,    0,    0,    0,    0,    0,
          119,   57,  124,   55,   72,   -9,  132,  227,
          135,  123,   76,   56,   -1,    9,   76,  126,
           82,   37,   29,    4,   -5,   13,   17,   63,
           72,   37,   24,    2,   -4,    3,    8,   45,
           61,   37,   20,   33,   18,    8,  -21,   24,
           81,   34,   52,   55,   48,   -8,  -21,   35,
            0,    0,    0,    0,    0,    0,    0,    0,

           83,    1,   15,  -48, -109,    3,  -84,  -26,
          -13,  -13, -157,  -88,  -91, -146,  -77,  -99,
          -54, -138,  -88, -122, -165, -191, -159, -153,
          -61,  -79,  -59, -100,  -82, -128,  -80,  -98,
          -65,  -67,  -60,  -51,  -76,  -69,  -78,  -72,
          -64,  -54,  -79,  -59,  -73,  -84, -106,  -72,
          -67,  -19,  -61,  -65,  -63,  -97,  -64,  -96,
           31,  -98,  -21,  -38,  -63,  -56,  -97,  -90,

          -63,  -74,   23,   39,  -30,   -7,  -45,  -78,
          -57,  -95,  -57,  -49, -111, -124,  -93,  -46,
          -58, -109, -114, -119, -107, -127, -116,  -75,
          -73,  -72,  -85, -121, -102, -108,  -83,  -75,
          -72,  -92,  -74,  -78, -104,  -78,  -88,  -80,
          -81,  -88,  -79,  -80,  -74, -101,  -96,  -94,
          -91, -106,  -92,  -73,  -80, -104, -122,  -95,
          -45,  -75,  -85,  -58,  -68,  -82,  -36,  -58,

           50,   16,   68,    0,    6,   60,   75,   50,
           52,   45,   15,    7,  -24,  -32,   44,   19,
           91,   66,   50,   48,   57,   12,   -1,   51,
          110,   94,   84,   43,   53,   42,   65,   88,
          122,  115,   96,   85,   69,   72,   48,   83,
          121,  103,   85,   96,   62,   59,   57,   87,
          122,   85,   98,   87,   67,   53,   68,  140,
           91,   93,   79,   69,   61,   71,  101,   72,

           33,   60,   -2,   87,  -81,  -80,  -64,  -44,
           13,   66,   43,   43,   77,  -59,   -4,  -64,
           -3,   19,  -10,   32,   12,  -77,  -57,  -73,
           56,   63,   41,   70,   72,   25,   76,   51,
           -6,   66,   38,   72,   47,   44,   33,   26,
           26,  -36,   39,   18,   30,   23,   -3,   13,
           22,   -1,  -46,  -10,  -16,  -34,  -45,  -64,
          -21,   -4,   -7,  -65,   25,    0,   20,   44,

          -37, -108,  -95,  -38,   44,   49,  -26,  -27,
          -46,  -67,  -31,  -85,   -7,   32,   58,   75,
          -29,  -38,  -69,   -6,  -27,  -35,  -44,   37,
          -17,   27,   20,   51,   54,   68,   48,   99,
           23,  -19,   51,  131,  123,   84,   79,   74,
          -33,  -27,   28,   71,   87,   68,    9,    5,
          -65,  -51,    1,   75,   53,   12,  -49,  -73,
          -81, -119,  -67,   41,  -59,    3,  -93, -113,
        };

        public static float[] KingPhaseTable = new float[64]
{
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
        -0.4f,    0,    0,    0,    0,    0,    0, 0.4f,
        -0.6f,    0,    0,    0,    0,    0,    0, 0.6f,
           -1,-0.4f,    0,    0,    0,    0, 0.4f,    1,
           -1,   -1,-0.5f,    0,    0,  0.5f,   1,    1,
        };

        public static short[] KingSafetyTables = new short[6 * 64]
        {
            0,    0,    0,    0,    0,    0,    0,    0,
            9,   -5,   -7,  -10,   -7,  -11,  -21,   -3,
           -4,   -8,   -9,   -7,  -13,  -19,   -9,    1,
           -4,   -7,   -7,   -9,   -9,   -7,   -9,   -2,
           -3,   -7,   -8,   -9,   -7,   -6,   -4,    3,
           -5,   -9,  -10,   -3,   -4,   -5,    1,    3,
           -3,  -12,  -10,   -7,    1,    0,    8,    0,
            0,    0,    0,    0,    0,    0,    0,    0,

            3,    4,    0,    2,    9,    8,    8,    5,
           15,    5,   -1,    5,   -1,   -5,   -3,   -3,
            8,    1,   12,    6,    9,    7,    3,   -2,
            0,    1,    5,   10,    6,    5,   10,    0,
            5,    9,    2,    3,    6,    5,   -2,    1,
            2,   -1,   -7,    3,    5,    3,    4,    5,
            5,   -6,    0,    0,    4,   -6,    3,    5,
           -3,   -4,    1,    2,    1,    6,   31,   -3,

            6,    6,    3,    6,   -7,   -9,    3,   -3,
           11,   -3,    3,   -3,   -1,   -6,   -4,   -8,
            4,   -2,   -2,    0,    1,    3,   -2,   -1,
            7,    6,    3,    4,    6,   -5,    6,   -2,
           -4,    2,    4,    3,    4,    2,   -1,    1,
           -2,   -5,   -1,    1,    4,    0,    3,    3,
            1,   -8,   -2,   -4,    1,    6,    5,    1,
           -8,   -2,   -5,   -3,    3,   10,    7,    8,

            4,    5,    0,    1,    4,    2,    0,   -8,
            4,    6,    5,    2,    3,   -2,    0,  -11,
            2,    2,    9,    8,    2,   -3,   -5,  -10,
            5,    5,    3,    3,    3,    0,   -7,   -1,
            2,    8,    3,    1,    3,    1,   -2,   -6,
            4,    0,    4,    2,    5,    9,    6,    3,
            5,   -1,    5,    5,    3,    4,    9,   -3,
            7,    7,    6,    4,    5,    8,    1,  -23,

           21,   11,    7,    6,    9,   14,    8,   21,
           11,   13,    5,    5,   11,   13,    8,   15,
           11,    5,    9,    9,   11,   23,   -6,   14,
            9,   10,    1,   10,   12,   18,   14,   19,
            4,    7,    2,    2,   14,    9,   14,   13,
           11,    5,    6,   10,    9,   13,   18,   11,
           13,   11,    7,   10,   13,    8,    6,   -1,
           11,   10,   10,    9,   11,   16,   -6,  -15,

            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            2,    0,    0,    0,    0,    0,    0,   10,
            6,    0,    0,    0,    0,    0,    0,    8,
           10,    8,    0,    0,    0,    0,   10,    3,
            8,   16,   17,    0,    0,    9,    3,    3,
        };
    }
}
