using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        private short _midgameScore;
        private short _endgameScore;
        private short _phaseValue;

        public short Score { get; private set; }

        //TODO: don't export interna for the sake of the Tuner
        public float P => (float)Phase(_phaseValue);
        public short MG => _midgameScore;
        public short EG => _endgameScore;

        public Evaluation(BoardState board) : this()
        {
            AddPieces(board);
            Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
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
        internal void Update(ref Move move)
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

            Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue += PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                AddScore(pieceIndex, squareIndex ^ 56);
            else
                SubtractScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue -= PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                SubtractScore(pieceIndex, squareIndex ^ 56);
            else
                AddScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _midgameScore += MidgameTables[tableIndex];
            _endgameScore += EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubtractScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            _midgameScore -= MidgameTables[tableIndex];
            _endgameScore -= EndgameTables[tableIndex];
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
        public static double Interpolate(short midgameScore, short endgameScore, short phaseValue)
        {
            return midgameScore + Phase(phaseValue) * endgameScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Phase(short phaseValue)
        {
            return Math.Clamp((double)(Phase1 - phaseValue) / Phase0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        //N:   76 B:  325 R:  390 Q:  993 +  -260
        public static readonly int Phase0 = 5000;
        public static readonly int Phase1 = 5260;
        public static readonly short[] PhaseValues = new short[6] { 0, 76, 325, 390, 993, 0 };

        //Tables tuned with Tuner v2: 20k epochs, fixed MG (no phase) + phase * EG
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24604822213727187
        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          157,  208,  134,  174,  160,  225,  123,   55,
           65,   81,  106,  109,  148,  138,  104,   59,
           52,   84,   81,   95,   98,   87,   94,   51,
           41,   69,   69,   86,   91,   83,   88,   50,
           43,   68,   69,   64,   78,   81,  110,   63,
           34,   71,   54,   51,   59,  101,  114,   53,
          100,  100,  100,  100,  100,  100,  100,  100,

          139,  228,  242,  276,  343,  230,  290,  203,
          251,  270,  395,  349,  346,  383,  317,  312,
          289,  382,  362,  392,  417,  444,  402,  373,
          310,  344,  343,  380,  364,  399,  348,  344,
          312,  325,  339,  338,  354,  347,  346,  318,
          304,  315,  338,  336,  345,  345,  351,  310,
          293,  266,  313,  323,  324,  343,  308,  314,
          211,  306,  265,  287,  305,  300,  309,  299,

          328,  335,  256,  250,  307,  286,  314,  333,
          328,  368,  341,  322,  383,  389,  367,  315,
          338,  379,  389,  393,  382,  403,  389,  355,
          348,  360,  372,  404,  392,  392,  362,  352,
          344,  371,  366,  375,  388,  364,  360,  352,
          350,  366,  366,  368,  366,  380,  368,  360,
          355,  367,  365,  352,  361,  370,  384,  348,
          308,  347,  339,  332,  338,  341,  310,  327,

          464,  491,  455,  509,  505,  456,  439,  461,
          459,  467,  494,  500,  515,  526,  464,  483,
          421,  446,  459,  462,  450,  483,  496,  449,
          404,  417,  434,  461,  453,  463,  434,  422,
          391,  399,  419,  426,  434,  428,  447,  414,
          385,  406,  417,  412,  435,  434,  437,  406,
          384,  416,  410,  421,  431,  442,  427,  367,
          410,  417,  432,  440,  443,  430,  407,  412,

          858,  871,  914,  869,  967,  960,  932,  931,
          878,  859,  893,  903,  889,  964,  920,  950,
          887,  886,  910,  910,  935,  978,  958,  961,
          862,  870,  885,  884,  898,  919,  896,  898,
          889,  870,  889,  887,  894,  897,  902,  899,
          876,  901,  886,  893,  891,  899,  912,  899,
          862,  886,  910,  897,  904,  911,  899,  905,
          892,  877,  886,  908,  878,  869,  871,  840,

          -34,   55,   45,   12,  -48,  -31,   28,   13,
           23,   63,   31,   73,   12,   -4,  -31,  -38,
           30,   42,   70,   12,   27,   49,   64,  -24,
           -2,  -10,   -5,  -23,  -33,  -34,  -25,  -90,
          -37,   12,  -26,  -99,  -92,  -58,  -60,  -76,
           10,   25,  -13,  -46,  -57,  -41,    3,   -8,
           37,   40,    9,  -52,  -31,    1,   44,   52,
           26,   75,   44,  -44,   31,   -9,   63,   58,
        };

        public static short[] EndgameTables = new short[6 * 64]{
            0,    0,    0,    0,    0,    0,    0,    0,
          107,   50,  114,   49,   66,  -13,  122,  210,
          124,  114,   70,   51,   -3,    6,   70,  117,
           76,   34,   27,    4,   -5,   12,   16,   59,
           67,   35,   22,    2,   -4,    2,    7,   42,
           56,   34,   19,   31,   16,    7,  -20,   22,
           75,   32,   48,   51,   45,   -8,  -21,   32,
            0,    0,    0,    0,    0,    0,    0,    0,

           79,    3,   18,  -39,  -95,    8,  -70,  -24,
          -10,   -9, -147,  -80,  -85, -136,  -71,  -92,
          -49, -129,  -81, -114, -154, -180, -150, -143,
          -56,  -73,  -55,  -94,  -77, -120,  -75,  -91,
          -60,  -62,  -55,  -48,  -71,  -65,  -74,  -67,
          -60,  -51,  -73,  -55,  -68,  -79, -100,  -67,
          -62,  -16,  -57,  -61,  -59,  -92,  -60,  -90,
           26,  -92,  -19,  -35,  -60,  -53,  -91,  -84,

          -58,  -67,   22,   38,  -26,   -5,  -38,  -71,
          -52,  -87,  -52,  -44, -103, -114,  -85,  -42,
          -53, -100, -105, -110,  -99, -117, -107,  -70,
          -67,  -67,  -78, -112,  -95, -100,  -77,  -69,
          -67,  -85,  -69,  -72,  -97,  -72,  -82,  -74,
          -75,  -82,  -73,  -74,  -69,  -94,  -89,  -87,
          -84,  -98,  -85,  -67,  -74,  -96, -113,  -88,
          -42,  -69,  -79,  -54,  -63,  -76,  -33,  -54,

           47,   15,   63,    0,    4,   54,   70,   46,
           49,   43,   15,    7,  -22,  -30,   42,   19,
           86,   62,   47,   45,   53,   11,   -1,   48,
          103,   88,   78,   40,   49,   39,   61,   82,
          113,  107,   89,   79,   64,   67,   44,   77,
          112,   96,   79,   89,   57,   55,   52,   81,
          113,   79,   92,   81,   62,   49,   63,  130,
           84,   86,   74,   64,   57,   66,   93,   67,

           32,   56,    0,   78,  -73,  -69,  -58,  -40,
           12,   63,   40,   39,   71,  -56,   -5,  -61,
           -2,   18,  -10,   30,   12,  -73,  -54,  -70,
           54,   60,   39,   67,   69,   24,   72,   48,
           -6,   63,   36,   68,   45,   41,   32,   25,
           24,  -35,   37,   17,   28,   22,   -3,   11,
           21,   -1,  -44,  -10,  -15,  -33,  -43,  -61,
          -21,   -4,   -7,  -62,   24,   -1,   17,   39,

          -42,  -93,  -81,  -36,   40,   39,  -23,  -22,
          -35,  -58,  -23,  -69,   -2,   35,   58,   53,
          -25,  -32,  -60,   -5,  -18,  -18,  -27,   37,
          -10,   25,   27,   44,   54,   63,   52,  100,
           16,  -19,   45,  129,  123,   83,   74,   73,
          -31,  -32,   23,   67,   83,   61,    4,    0,
          -69,  -55,   -5,   66,   45,    5,  -52,  -75,
          -83, -119,  -69,   34,  -62,   -3,  -94, -112,
        };
    }
}
