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
            Score = (short)Math.Round(CombineScores(board));
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
        public static readonly short[] PhaseValues = new short[6] { 0, 88, 324, 400, 883, 0 };

        //Tuned on 'data/quiet-labeled.epd'
        //500 Iterations. Material MSE = 0,245998296203
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24599673230870314
        public static readonly short[] MidgameTables = new short[6 * 64]{
           100,  100,  100,  100,  100,  100,  100,  100,
          152,  201,  145,  181,  169,  201,  133,   86,
           64,   83,  115,  118,  158,  155,  106,   53,
           54,   95,   91,  112,  115,  103,  108,   56,
           45,   79,   80,  100,  101,   97,   97,   51,
           48,   79,   83,   70,   83,   87,  112,   62,
           37,   87,   65,   61,   62,   99,  108,   56,
          100,  100,  100,  100,  100,  100,  100,  100,

          150,  243,  255,  266,  299,  232,  266,  195,
          238,  262,  379,  326,  328,  367,  300,  288,
          272,  365,  345,  379,  391,  413,  385,  337,
          303,  335,  329,  363,  349,  386,  332,  334,
          300,  310,  332,  328,  341,  333,  337,  309,
          296,  310,  341,  328,  332,  335,  339,  300,
          281,  262,  308,  318,  314,  341,  295,  299,
          225,  304,  256,  278,  296,  282,  290,  274,

          294,  309,  261,  258,  292,  276,  297,  296,
          309,  363,  329,  309,  356,  370,  358,  305,
          327,  368,  383,  378,  366,  383,  377,  348,
          332,  348,  361,  392,  377,  386,  351,  344,
          337,  361,  355,  367,  377,  360,  353,  344,
          345,  365,  361,  358,  356,  378,  361,  347,
          336,  377,  359,  356,  355,  360,  371,  338,
          305,  339,  342,  329,  332,  325,  296,  312,

          481,  499,  476,  520,  511,  486,  480,  481,
          477,  477,  510,  516,  526,  542,  487,  505,
          443,  466,  473,  477,  469,  505,  512,  476,
          419,  430,  452,  479,  470,  483,  457,  440,
          402,  410,  434,  442,  452,  447,  467,  433,
          393,  420,  426,  426,  447,  442,  448,  422,
          390,  435,  415,  430,  445,  462,  438,  389,
          410,  415,  432,  445,  444,  430,  424,  447,

          850,  889,  915,  895,  940,  928,  910,  911,
          881,  861,  901,  915,  904,  948,  924,  938,
          891,  895,  917,  918,  937,  956,  960,  952,
          868,  875,  896,  890,  903,  922,  903,  900,
          899,  876,  904,  898,  896,  904,  905,  903,
          876,  911,  894,  899,  897,  903,  912,  904,
          859,  877,  918,  898,  904,  921,  906,  904,
          894,  878,  889,  913,  881,  875,  882,  862,

          -40,    8,   -4,   -8,  -22,   -9,   15,    4,
            4,   29,   16,   33,   11,    6,   -4,  -10,
           14,   27,   37,   14,   19,   43,   44,   -2,
           -3,    2,    5,  -13,  -13,  -21,   -2,  -42,
          -25,    8,  -18,  -71,  -81,  -46,  -48,  -53,
           -2,   19,  -20,  -67,  -72,  -54,   10,    6,
           23,   15,    5,  -64,  -42,    8,   71,   84,
           11,   43,   47,  -43,   35,   16,   83,   91,
        };

        public static short[] EndgameTables = new short[6 * 64]{

            0,    0,    0,    0,    0,    0,    0,    0,
          120,   62,  110,   48,   62,   18,  119,  187,
          137,  123,   70,   49,   -9,   -9,   75,  134,
           80,   29,   22,   -9,  -20,   -4,    5,   58,
           69,   29,   15,   -9,  -13,   -9,    1,   45,
           57,   28,    9,   28,   13,    4,  -21,   26,
           78,   23,   43,   48,   43,   -8,  -19,   35,
            0,    0,    0,    0,    0,    0,    0,    0,

           67,  -16,    4,  -28,  -49,    4,  -46,  -16,
            3,    0, -135,  -60,  -66, -123,  -52,  -65,
          -32, -118,  -70, -107, -137, -155, -138, -104,
          -50,  -68,  -43,  -83,  -67, -114,  -64,  -84,
          -52,  -50,  -50,  -38,  -62,  -55,  -65,  -59,
          -55,  -46,  -72,  -48,  -58,  -72,  -95,  -59,
          -50,   -8,  -52,  -56,  -51,  -90,  -47,  -81,
           10,  -91,   -6,  -24,  -50,  -38,  -83,  -51,

          -16,  -39,   17,   30,   -7,    9,  -19,  -25,
          -32,  -84,  -39,  -30,  -74,  -94,  -78,  -30,
          -43,  -92, -104, -100,  -86, -101,  -98,  -63,
          -53,  -60,  -70, -105,  -87,  -98,  -72,  -63,
          -60,  -79,  -61,  -67,  -92,  -71,  -78,  -66,
          -74,  -82,  -71,  -67,  -63,  -95,  -86,  -78,
          -64, -106,  -82,  -70,  -71,  -91, -109,  -81,
          -35,  -59,  -78,  -49,  -58,  -72,  -15,  -38,

           30,    4,   43,  -13,   -5,   20,   23,   25,
           31,   32,   -5,  -14,  -40,  -51,   16,   -5,
           66,   43,   31,   27,   34,  -15,  -21,   20,
           91,   78,   62,   21,   31,   15,   38,   64,
          110,  101,   77,   65,   48,   48,   22,   60,
          110,   87,   72,   79,   47,   46,   40,   66,
          115,   66,   90,   76,   50,   30,   51,  111,
           87,   90,   76,   62,   56,   65,   78,   38,

           39,   26,    2,   39,  -23,  -17,  -21,  -13,
            1,   57,   30,   24,   43,  -29,  -12,  -43,
          -10,    8,  -15,   21,    9,  -41,  -46,  -63,
           40,   50,   28,   61,   60,   14,   55,   31,
          -17,   52,   26,   62,   40,   31,   21,   14,
           13,  -44,   26,   11,   21,   14,  -14,    3,
           13,   -5,  -53,  -20,  -26,  -44,  -46,  -45,
          -31,  -16,  -18,  -73,   14,  -19,    2,    3,

          -36,  -32,  -38,  -20,   -4,    8,    3,   -3,
          -17,  -16,  -10,  -23,   -3,   10,   17,   24,
           -8,  -19,  -27,   -1,  -10,   -7,  -15,   20,
          -12,    9,    8,   22,   30,   39,   29,   52,
            1,  -19,   33,   98,  103,   71,   63,   54,
          -22,  -24,   29,   83,   99,   75,    4,   -6,
          -54,  -34,    5,   87,   66,    7,  -70, -102,
          -67,  -96,  -65,   45,  -57,  -16, -123, -138,
        };

        public static double[] KingPhaseTable = new double[64]
        {
         -0.008,   0.73,     -1, -0.483,     -1, -0.351,      1,  0.705,
         -0.188,  0.256, -0.277,  0.101, -0.115, -0.933, -0.721,  0.109,
          0.029, -0.112, -0.111,  0.356, -0.084,   0.22, -0.508,  0.373,
         -0.073, -0.046, -0.326, -0.589, -0.214, -0.562,  0.092,  0.209,
         -0.019, -0.101, -0.174, -0.194, -0.493, -0.087, -0.027,  0.227,
         -0.035,  0.143, -0.054, -0.373, -0.195, -0.142,  0.291,  0.426,
          0.109, -0.132,  0.235,  0.134,   0.16,  0.389,  0.707,      1,
          0.074, -0.114,  0.412,  0.332,  0.426,  0.693,      1,      1,
        };

        public static short[] KingSafetyTables = new short[6 * 64]
        {
            0,    0,    0,    0,    0,    0,    0,    0,
            3,    2,   -9,  -11,   -6,   -1,   -8,   -3,
           -6,   -9,  -14,  -14,  -18,  -24,   -9,   -2,
           -5,  -14,  -14,  -21,  -21,  -18,  -20,   -8,
           -6,  -11,  -14,  -17,  -13,  -18,  -12,   -4,
           -7,  -13,  -16,   -6,   -5,  -10,   -4,   -2,
           -5,  -20,  -13,  -14,    1,    2,    8,   -8,
            0,    0,    0,    0,    0,    0,    0,    0,

           -2,    0,    0,    0,    8,   -1,    3,    0,
            8,    2,    3,   10,    1,    1,    1,   -1,
            5,    7,   14,    8,   17,   12,    6,    4,
            1,    4,    9,   16,   11,    6,   13,    3,
            8,   10,    2,    5,   10,   11,   -1,    3,
            4,   -1,  -13,    2,    9,    4,    7,    6,
            4,   -5,   -2,   -1,    6,   -7,    4,    9,
           -5,   -6,    1,    3,    2,   11,   25,   -1,

            4,    7,    0,    3,   -2,   -2,    3,    1,
            9,   -4,    1,    1,    5,   -1,    1,   -1,
            5,    2,    1,    6,    3,    7,    2,    1,
           10,    8,    5,    6,   11,   -3,   11,    3,
           -1,    5,    8,    4,    7,    0,    1,    0,
            0,   -5,    0,    5,    7,   -3,    3,    7,
            2,  -19,    0,  -11,    0,    7,    9,    2,
           -7,   -1,  -13,   -3,    1,   22,    4,    7,

           -2,    2,   -6,   -3,    3,   -4,   -3,   -8,
           -5,    1,   -2,   -1,    1,   -5,   -7,  -13,
           -9,   -6,    2,    0,   -4,   -8,   -7,  -13,
           -3,   -1,   -5,   -4,   -3,   -4,   -9,   -7,
           -6,    1,   -4,   -5,   -6,   -7,   -6,  -11,
           -1,   -6,    0,   -4,   -2,    3,   -1,   -6,
           -1,  -11,    2,    0,   -4,   -8,    2,  -12,
            6,    8,    5,    2,    6,    8,   -9,  -44,

           14,    3,    1,    2,    5,    9,    6,   14,
            7,    8,    1,    1,    5,    8,    5,   10,
            5,    0,    1,    2,    5,   18,   -4,   15,
            6,    5,   -3,    4,    7,    8,    7,   15,
           -5,    4,   -9,   -7,    9,    2,    9,    7,
           12,   -3,    2,    5,    3,    7,   12,    5,
           13,   17,    1,   10,   13,    0,   -1,   -3,
            8,    9,    8,    5,    7,    9,   -4,   -8,

            0,    0,    0,    0,    1,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    1,    0,    0,
            0,    0,    0,    0,    1,    0,    0,    0,
            0,    0,    0,    1,    1,    1,    0,    0,
            0,    0,    0,    0,    0,    0,    2,    6,
            0,    4,    0,    0,    0,    1,   18,    3,
        };
    }
}
