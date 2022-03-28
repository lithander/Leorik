using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct Evaluation
    {
        private short _midgameScore;
        private short _endgameScore;
        private short _phaseValue;

        public short Score { get; private set; }
        public short Phase => _phaseValue;

        public Evaluation(BoardState board) : this()
        {
            AddPieces(ref board);
            Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
        }

        private void AddPieces(ref BoardState board)
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
            double phase = (double)(phaseValue - Midgame) / (Endgame - Midgame);
            return midgameScore + Math.Clamp(phase, 0, 1) * (endgameScore - midgameScore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;


        public static readonly int Midgame = 5255;
        public static readonly int Endgame = 435;

        public static readonly short[] PhaseValues = new short[6] { 0, 155, 305, 405, 1050, 0 };

        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          166,  215,  139,  178,  162,  223,  131,   69,
           71,   86,  110,  113,  151,  141,  108,   64,
           55,   86,   83,   97,   99,   88,   96,   54,
           44,   71,   71,   87,   92,   84,   89,   52,
           45,   70,   71,   66,   80,   83,  111,   65,
           37,   73,   55,   52,   61,  102,  115,   55,
          100,  100,  100,  100,  100,  100,  100,  100,

          149,  252,  253,  280,  328,  236,  297,  210,
          250,  271,  394,  347,  342,  381,  315,  307,
          287,  381,  363,  391,  415,  439,  400,  366,
          311,  345,  345,  381,  365,  398,  348,  344,
          312,  326,  340,  340,  355,  348,  347,  318,
          304,  316,  339,  337,  346,  345,  351,  310,
          292,  266,  313,  323,  324,  343,  308,  314,
          222,  305,  266,  288,  305,  300,  308,  293,

          324,  329,  259,  255,  302,  286,  308,  323,
          328,  367,  341,  319,  379,  387,  367,  315,
          338,  379,  389,  393,  382,  401,  388,  355,
          348,  360,  372,  405,  392,  392,  363,  352,
          344,  370,  367,  376,  388,  365,  361,  352,
          350,  366,  367,  369,  366,  380,  369,  360,
          353,  367,  365,  352,  361,  370,  384,  347,
          310,  348,  339,  332,  338,  341,  309,  327,

          474,  499,  464,  516,  515,  473,  461,  475,
          468,  475,  503,  508,  522,  533,  473,  492,
          430,  454,  469,  470,  459,  491,  505,  458,
          413,  426,  443,  470,  461,  471,  442,  431,
          400,  408,  428,  435,  443,  437,  455,  423,
          394,  415,  425,  421,  443,  442,  445,  415,
          393,  424,  419,  430,  440,  450,  435,  376,
          419,  426,  440,  449,  451,  438,  416,  420,

          867,  884,  918,  887,  964,  950,  927,  934,
          888,  868,  902,  913,  905,  968,  928,  954,
          895,  894,  917,  919,  945,  982,  964,  967,
          872,  879,  894,  893,  909,  928,  907,  908,
          895,  879,  897,  895,  903,  906,  911,  908,
          885,  907,  895,  901,  900,  907,  920,  909,
          870,  893,  916,  904,  911,  917,  904,  907,
          898,  884,  893,  914,  885,  876,  879,  849,

           -8,   29,   24,    8,  -25,  -16,   14,    6,
           12,   35,   18,   40,    6,   -4,  -18,  -18,
           19,   29,   46,    6,   15,   29,   41,  -14,
           -2,   -5,   -5,  -18,  -28,  -26,  -20,  -76,
          -22,   10,  -20,  -88,  -88,  -57,  -56,  -75,
            8,   28,  -10,  -41,  -53,  -36,    7,   -5,
           40,   45,   14,  -47,  -26,    5,   48,   55,
           27,   77,   48,  -38,   34,   -5,   65,   60,
        };

        public static short[] EndgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          266,  261,  249,  225,  229,  217,  246,  264,
          190,  196,  178,  162,  148,  146,  175,  176,
          128,  120,  109,  101,   94,   99,  111,  111,
          109,  105,   93,   89,   88,   86,   96,   93,
          100,  103,   89,   96,   95,   89,   92,   86,
          110,  104,  102,  103,  105,   94,   95,   86,
          100,  100,  100,  100,  100,  100,  100,  100,

          221,  231,  264,  241,  255,  240,  222,  181,
          246,  265,  253,  274,  266,  252,  251,  225,
          245,  258,  286,  283,  268,  270,  257,  236,
          259,  275,  294,  292,  293,  284,  277,  258,
          257,  268,  289,  296,  289,  288,  277,  255,
          249,  269,  270,  286,  282,  271,  255,  248,
          236,  255,  262,  267,  270,  256,  253,  227,
          238,  219,  251,  257,  250,  251,  224,  221,

          276,  275,  281,  290,  286,  285,  281,  269,
          282,  286,  295,  283,  288,  283,  288,  277,
          290,  286,  291,  290,  290,  294,  288,  291,
          287,  298,  300,  299,  303,  298,  290,  289,
          284,  292,  303,  310,  297,  298,  284,  284,
          281,  290,  299,  300,  303,  292,  285,  278,
          277,  275,  286,  290,  293,  280,  277,  266,
          271,  284,  267,  284,  281,  272,  283,  277,

          517,  512,  524,  516,  516,  514,  513,  512,
          514,  516,  515,  514,  500,  504,  512,  508,
          512,  514,  512,  513,  509,  501,  502,  503,
          511,  511,  518,  507,  509,  508,  501,  509,
          509,  512,  513,  510,  504,  500,  496,  496,
          502,  507,  502,  506,  498,  494,  494,  492,
          502,  500,  507,  508,  498,  496,  496,  501,
          500,  509,  511,  510,  506,  501,  505,  485,

          900,  933,  926,  950,  913,  913,  893,  905,
          896,  933,  943,  951,  963,  920,  924,  902,
          894,  914,  912,  950,  955,  920,  913,  902,
          924,  939,  934,  962,  975,  952,  975,  952,
          893,  943,  935,  966,  949,  948,  941,  932,
          907,  878,  932,  921,  928,  932,  917,  915,
          892,  894,  877,  896,  899,  888,  866,  861,
          880,  881,  890,  859,  911,  877,  895,  884,

          -76,  -36,  -34,  -24,  -11,    6,    7,   -8,
          -10,    9,   11,    9,   11,   31,   26,   13,
            6,   13,   14,    7,   11,   34,   40,   11,
          -11,   14,   22,   20,   21,   28,   26,    7,
          -23,   -7,   18,   27,   29,   25,   13,   -5,
          -20,   -7,   10,   20,   25,   19,    7,   -9,
          -32,  -15,    4,   12,   13,    5,   -8,  -23,
          -57,  -43,  -26,  -11,  -30,  -13,  -31,  -54,
        };
    }
}
