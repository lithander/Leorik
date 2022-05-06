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
        //float MSE_SCALING = 100;
        //int ITERATIONS = 125;
        //int MATERIAL_ALPHA = 1000;
        //int PHASE_ALPHA = 1000;
        //int KS_ALPHA = 1000;
        //float KS_PHASE_ALPHA = 5;
        //int MATERIAL_BATCH = 100;
        //int PHASE_BATCH = 5;
        //int KS_BATCH = 5;
        //int KS_PHASE_BATCH = 5;
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24434034051333522
        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          140,  201,  141,  189,  168,  219,  130,   56,
           65,   84,  118,  120,  168,  171,  114,   57,
           55,   95,   90,  111,  115,  105,  109,   57,
           45,   80,   81,  100,  101,   97,   97,   51,
           48,   79,   83,   71,   83,   87,  113,   62,
           37,   87,   66,   61,   63,   99,  109,   57,
          100,  100,  100,  100,  100,  100,  100,  100,

          140,  234,  249,  273,  322,  229,  279,  193,
          239,  268,  392,  337,  348,  386,  317,  310,
          280,  379,  351,  387,  398,  431,  398,  366,
          315,  343,  337,  371,  356,  396,  338,  344,
          307,  316,  340,  336,  349,  339,  349,  317,
          303,  319,  347,  336,  339,  342,  347,  306,
          290,  277,  316,  325,  321,  351,  304,  304,
          216,  311,  264,  285,  304,  287,  291,  293,

          318,  326,  258,  248,  306,  283,  310,  324,
          320,  379,  343,  324,  380,  395,  373,  322,
          338,  388,  397,  390,  386,  402,  396,  359,
          343,  359,  373,  404,  388,  402,  361,  356,
          352,  372,  365,  379,  386,  371,  364,  358,
          357,  379,  372,  369,  366,  388,  371,  358,
          355,  387,  373,  365,  364,  370,  381,  351,
          325,  357,  352,  342,  344,  337,  307,  320,

          471,  490,  467,  518,  503,  469,  458,  468,
          468,  469,  502,  507,  519,  541,  477,  498,
          435,  456,  462,  466,  458,  502,  509,  469,
          409,  419,  444,  469,  461,  474,  446,  432,
          396,  396,  425,  433,  444,  440,  458,  426,
          384,  412,  417,  417,  438,  431,  439,  415,
          384,  427,  407,  422,  435,  456,  428,  383,
          405,  410,  427,  440,  440,  425,  417,  435,

          847,  878,  920,  879,  957,  942,  921,  917,
          875,  855,  898,  913,  890,  956,  923,  945,
          890,  896,  915,  917,  936,  958,  974,  954,
          862,  871,  897,  887,  899,  916,  896,  894,
          898,  872,  905,  896,  892,  901,  900,  899,
          868,  908,  892,  896,  895,  899,  907,  900,
          849,  865,  915,  895,  900,  920,  910,  914,
          887,  871,  886,  910,  879,  868,  878,  857,

          -39,   32,   23,    3,  -32,  -20,   20,    8,
           17,   50,   30,   61,   16,    3,  -15,  -33,
           24,   42,   64,   17,   32,   61,   65,  -13,
            2,   -3,    5,  -19,  -22,  -33,  -12,  -68,
          -36,   13,  -27,  -93,  -93,  -50,  -55,  -65,
            6,   17,  -21,  -64,  -70,  -53,    9,    0,
           25,   21,    0,  -65,  -40,    7,   66,   76,
           15,   28,   33,  -51,   29,   10,   76,   85,
        };

        public static short[] EndgameTables = new short[6 * 64]{
            0,    0,    0,    0,    0,    0,    0,    0,
          135,   61,  118,   41,   65,   -3,  126,  226,
          136,  122,   67,   47,  -20,  -26,   66,  130,
           80,   29,   22,   -9,  -20,   -5,    4,   57,
           69,   29,   15,   -9,  -14,  -10,    0,   44,
           57,   28,    9,   27,   12,    4,  -22,   26,
           78,   23,   42,   48,   42,   -9,  -20,   35,
            0,    0,    0,    0,    0,    0,    0,    0,

           84,   -6,    9,  -38,  -82,    7,  -65,  -13,
           -3,  -11, -155,  -77,  -93, -149,  -74,  -94,
          -47, -137,  -80, -119, -149, -181, -157, -143,
          -67,  -79,  -54,  -95,  -78, -127,  -74,  -98,
          -63,  -61,  -61,  -49,  -73,  -65,  -82,  -71,
          -66,  -58,  -83,  -59,  -69,  -83, -106,  -70,
          -65,  -27,  -63,  -67,  -62, -102,  -62,  -92,
           24, -102,  -20,  -36,  -62,  -49,  -93,  -79,

          -52,  -66,   20,   40,  -25,    0,  -37,  -62,
          -49, -105,  -58,  -51, -107, -128, -100,  -52,
          -59, -118, -124, -119, -112, -128, -123,  -79,
          -69,  -75,  -86, -122, -102, -120,  -87,  -81,
          -80,  -96,  -76,  -83, -106,  -85,  -93,  -87,
          -90, -100,  -86,  -82,  -77, -110, -101,  -93,
          -92, -121, -101,  -83,  -85, -106, -124,  -99,
          -59,  -84,  -93,  -67,  -74,  -87,  -33,  -53,

           43,   17,   56,  -10,    7,   43,   53,   44,
           43,   42,    6,   -2,  -30,  -48,   31,    6,
           78,   56,   47,   42,   50,   -8,  -13,   31,
          104,   93,   74,   35,   44,   29,   54,   78,
          120,  120,   90,   78,   60,   60,   36,   72,
          122,   98,   85,   90,   59,   60,   53,   77,
          124,   76,  102,   87,   63,   40,   66,  121,
           95,   99,   83,   69,   63,   73,   89,   52,

           38,   44,   -5,   65,  -54,  -44,  -43,  -30,
            9,   62,   37,   27,   63,  -49,  -15,  -65,
           -9,    8,  -14,   24,    9,  -57,  -68,  -72,
           47,   53,   31,   64,   66,   21,   65,   34,
          -12,   57,   29,   65,   43,   35,   26,   17,
           18,  -39,   29,   15,   25,   18,   -9,    8,
           21,    4,  -48,  -15,  -22,  -40,  -53,  -63,
          -26,  -11,  -14,  -67,   18,  -11,   14,   22,

          -37,  -63,  -62,  -27,    8,   24,   -1,   -8,
          -30,  -45,  -23,  -56,  -10,   11,   24,   46,
          -21,  -35,  -58,  -10,  -26,  -29,  -40,   32,
          -16,   17,   13,   35,   41,   57,   40,   82,
           15,  -23,   46,  125,  120,   77,   73,   65,
          -30,  -24,   31,   82,   97,   75,    6,   -2,
          -60,  -39,    8,   87,   64,   10,  -64,  -91,
          -76,  -92,  -57,   50,  -52,   -9, -112, -128,
        };

        public static double[] KingPhaseTable = new double[64]
        {
         -0.001,  0.502, -0.382,  -0.08,     -1, -0.223,      1,  0.658,
         -0.024,  0.058, -0.052,  0.083, -0.113,     -1,     -1,  -0.22,
          0.006, -0.013,  -0.06,  0.112, -0.127,  0.234, -0.436,  0.306,
         -0.007, -0.011, -0.133, -0.322, -0.194, -0.428,  0.049,  0.088,
         -0.002, -0.033, -0.076, -0.155, -0.367, -0.099, -0.027,  0.095,
         -0.006,   0.06, -0.033,  -0.29, -0.196, -0.144,  0.293,  0.311,
          0.023, -0.054,  0.134,  0.075,  0.148,  0.415,  0.728,      1,
          0.011, -0.505,  0.197,  0.178,  0.369,  0.707,      1,      1,
        };

        public static short[] KingSafetyTables = new short[6 * 64]
        {
            0,    0,    0,    0,    0,    0,    0,    0,
            9,    2,  -15,  -20,  -13,  -11,  -16,   -6,
           -6,  -10,  -18,  -16,  -27,  -37,  -18,   -8,
           -5,  -13,  -14,  -20,  -20,  -20,  -22,   -9,
           -6,  -11,  -14,  -17,  -12,  -17,  -13,   -3,
           -6,  -13,  -16,   -6,   -5,  -10,   -4,   -2,
           -5,  -20,  -15,  -14,    1,    2,    8,   -8,
            0,    0,    0,    0,    0,    0,    0,    0,

            2,    3,    2,    1,   16,    3,    8,    2,
           17,    7,    2,   15,   -1,   -1,    0,   -4,
           11,    6,   17,    9,   22,   14,    6,    2,
           -1,    5,   10,   18,   13,    6,   16,    3,
           11,   15,    3,    5,   12,   13,   -3,    3,
            6,   -1,  -11,    4,   10,    6,    8,    8,
            6,   -8,   -1,    0,    7,   -9,    7,   13,
           -7,   -6,    5,    6,    3,   15,   46,   -2,

           10,   14,    2,    9,   -4,   -2,    8,    0,
           14,   -8,    1,    2,    4,   -4,   -1,   -6,
            4,   -2,   -2,    7,   -1,    6,   -3,   -1,
            9,    7,    5,    5,    9,   -8,   11,    0,
           -4,    4,    8,    2,    7,   -2,   -1,   -2,
           -3,   -9,   -1,    3,    6,   -4,    3,    6,
            0,  -20,   -4,  -12,    1,    7,    9,    1,
          -14,   -7,  -14,   -6,    0,   20,    7,   12,

           -1,    2,   -6,   -4,    5,   -6,   -6,  -12,
           -3,    2,   -2,    0,    2,   -7,  -11,  -17,
           -8,   -4,    4,    3,   -4,  -12,  -13,  -19,
           -1,    2,   -4,   -3,   -2,   -5,  -12,   -8,
           -5,    6,   -3,   -3,   -4,   -7,   -9,  -14,
            2,   -5,    2,   -1,    1,    7,    1,   -6,
            0,  -10,    5,    3,    0,   -7,    4,  -15,
            8,    8,    6,    2,    6,    9,  -10,  -36,

           24,    9,    2,    5,    9,   17,   12,   27,
           14,   16,    3,    2,   12,   15,   10,   17,
            8,   -1,    4,    3,    9,   31,   -9,   21,
           12,   10,   -6,    7,   11,   16,   15,   25,
           -4,    8,  -11,   -4,   15,    5,   16,   13,
           21,    1,    5,    9,    6,   13,   19,   10,
           24,   30,    4,   14,   18,    1,   -1,   -6,
           16,   17,   13,    8,   11,   17,   -9,  -17,

            0,    1,    0,    0,    1,    0,    1,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    0,    0,    0,
            0,    0,    0,    0,    0,    1,    0,    0,
            0,    0,    0,    0,    0,    0,    2,    4,
            0,    4,    0,    0,    0,    0,   16,    1,
        };
    }
}
