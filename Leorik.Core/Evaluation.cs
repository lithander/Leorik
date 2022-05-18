using System.Runtime.CompilerServices;

namespace Leorik.Core
{
    public struct MaterialEval
    {
        public short Base;
        public short Endgame;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Base += MidgameTables[tableIndex];
            Endgame += EndgameTables[tableIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubtractScore(int pieceIndex, int squareIndex)
        {
            int tableIndex = (pieceIndex << 6) | squareIndex;
            Base -= MidgameTables[tableIndex];
            Endgame -= EndgameTables[tableIndex];
        }

        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          154,  195,  130,  165,  152,  191,  115,   68,
           74,   79,  109,  110,  147,  150,  105,   75,
           60,   79,   84,   99,  100,   93,   90,   65,
           48,   61,   71,   88,   92,   85,   81,   61,
           51,   59,   71,   66,   78,   84,  103,   74,
           40,   61,   54,   51,   59,  103,  106,   63,
          100,  100,  100,  100,  100,  100,  100,  100,

          147,  242,  256,  267,  306,  232,  269,  196,
          245,  264,  386,  336,  329,  371,  301,  289,
          278,  376,  360,  389,  411,  427,  397,  348,
          305,  342,  341,  379,  363,  396,  345,  340,
          310,  322,  338,  337,  352,  345,  341,  315,
          301,  313,  336,  334,  343,  342,  349,  308,
          289,  258,  311,  320,  322,  340,  300,  312,
          225,  303,  259,  282,  303,  296,  306,  279,

          298,  316,  261,  257,  295,  279,  302,  298,
          320,  362,  337,  314,  365,  375,  358,  307,
          338,  378,  389,  392,  379,  397,  386,  353,
          348,  361,  374,  404,  392,  389,  363,  351,
          342,  371,  368,  377,  390,  366,  360,  349,
          350,  367,  367,  369,  367,  381,  369,  359,
          348,  368,  366,  354,  363,  370,  386,  349,
          303,  345,  340,  331,  337,  342,  301,  321,

          480,  505,  472,  520,  516,  486,  483,  485,
          480,  481,  510,  518,  532,  542,  486,  507,
          441,  464,  476,  481,  469,  503,  518,  471,
          418,  431,  449,  475,  468,  480,  454,  440,
          401,  413,  430,  437,  446,  440,  465,  425,
          394,  418,  427,  422,  446,  445,  450,  417,
          392,  426,  419,  430,  441,  451,  440,  377,
          418,  427,  441,  449,  453,  438,  417,  422,

          861,  895,  922,  899,  952,  941,  920,  928,
          893,  870,  905,  918,  910,  960,  927,  953,
          902,  901,  921,  922,  945,  976,  963,  970,
          878,  884,  899,  898,  912,  931,  912,  912,
          902,  885,  902,  901,  908,  911,  916,  912,
          891,  916,  900,  908,  905,  913,  926,  912,
          875,  900,  924,  911,  919,  926,  909,  911,
          905,  891,  900,  922,  892,  884,  887,  863,

          -41,    3,    1,   -5,  -16,   -8,    9,   -2,
            6,   26,   17,   33,   12,   12,    2,  -11,
           13,   30,   38,    9,   18,   43,   53,   -5,
            0,    4,   11,   -6,  -14,  -14,   -2,  -47,
          -26,   14,  -16,  -68,  -71,  -42,  -49,  -66,
           -1,   19,   -8,  -39,  -49,  -36,    3,  -12,
           22,   37,    4,  -56,  -34,   -2,   41,   50,
           15,   72,   41,  -46,   27,  -13,   61,   56,
        };

        public static short[] EndgameTables = new short[6 * 64]{
            0,    0,    0,    0,    0,    0,    0,    0,
           32,  -13,   58,    7,   22,  -39,   54,  118,
           65,   67,   25,   13,  -42,  -49,   14,   45,
           30,    6,    0,  -22,  -23,  -17,  -13,   11,
           28,   15,    5,  -12,  -16,  -15,  -16,    3,
           18,   12,    2,   20,    7,  -10,  -48,  -17,
           42,   12,   39,   44,   40,  -24,  -47,   -3,
            0,    0,    0,    0,    0,    0,    0,    0,

           71,  -12,    5,  -24,  -50,    9,  -43,  -14,
            3,    1, -142,  -64,  -65, -126,  -50,  -61,
          -33, -126,  -80, -113, -152, -165, -148, -114,
          -50,  -71,  -50,  -93,  -74, -120,  -70,  -86,
          -55,  -56,  -52,  -43,  -67,  -62,  -66,  -61,
          -54,  -46,  -70,  -50,  -64,  -75,  -99,  -62,
          -52,    0,  -52,  -56,  -55,  -86,  -48,  -88,
           15,  -90,   -6,  -25,  -55,  -44,  -90,  -54,

          -14,  -38,   23,   35,   -5,   11,  -17,  -23,
          -35,  -79,  -44,  -30,  -79,  -93,  -72,  -25,
          -50,  -98, -106, -109,  -94, -111, -103,  -65,
          -66,  -68,  -79, -113,  -95,  -98,  -78,  -65,
          -61,  -85,  -68,  -71,  -99,  -72,  -80,  -66,
          -72,  -80,  -72,  -73,  -69,  -96,  -89,  -85,
          -71, -100,  -87,  -68,  -75,  -94, -117,  -88,
          -29,  -62,  -79,  -49,  -59,  -76,  -14,  -42,

           37,    5,   52,   -7,   -1,   27,   27,   27,
           33,   35,    1,  -10,  -38,  -45,   24,   -3,
           74,   50,   35,   30,   40,   -7,  -23,   29,
           99,   83,   71,   32,   40,   26,   45,   70,
          117,  107,   89,   78,   62,   63,   30,   76,
          118,   96,   80,   90,   55,   52,   46,   82,
          120,   79,   94,   83,   60,   46,   57,  134,
           88,   88,   75,   65,   56,   68,   95,   65,

           47,   36,    6,   48,  -25,  -13,  -14,  -10,
            3,   65,   39,   36,   55,  -26,   10,  -35,
          -11,   11,   -9,   30,   15,  -43,  -37,  -60,
           43,   54,   34,   63,   65,   21,   64,   46,
          -11,   58,   34,   64,   41,   35,   27,   27,
           18,  -40,   34,   12,   25,   16,   -7,   10,
           19,   -8,  -50,  -16,  -21,  -42,  -40,  -45,
          -27,  -13,  -13,  -69,   18,  -11,    5,   10,

          -35,  -35,  -32,  -17,    3,   11,   -3,   -6,
          -18,  -20,   -7,  -24,   -1,   16,   21,   21,
           -9,  -21,  -27,   -1,   -9,  -12,  -18,   13,
          -15,    8,    9,   27,   36,   43,   26,   51,
            5,  -23,   37,  102,  105,   71,   65,   64,
          -18,  -25,   20,   63,   79,   60,    5,    4,
          -52,  -52,    3,   75,   52,    9,  -50,  -76,
          -72, -122,  -69,   40,  -61,    2,  -98, -117,
        };
    }

    public struct Evaluation
    {
        private short _phaseValue;
        private PawnEval _pawns;
        private MaterialEval _material;

        public short Score { get; private set; }

        public Evaluation(BoardState board) : this()
        {
            _pawns.Update(board);
            AddPieces(board);
            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateScore()
        {
            int mg = _pawns.Base + _material.Base;
            int eg = _pawns.Endgame + _material.Endgame;
            Score = (short)(mg + Phase(_phaseValue) * eg);
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
        internal void Update(BoardState board, ref Move move)
        {
            _pawns.Update(board, ref move);

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

            UpdateScore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue += PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                _material.AddScore(pieceIndex, squareIndex ^ 56);
            else
                _material.SubtractScore(pieceIndex, squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemovePiece(Piece piece, int squareIndex)
        {
            int pieceIndex = PieceIndex(piece);
            _phaseValue -= PhaseValues[pieceIndex];
            if ((piece & Piece.ColorMask) == Piece.White)
                _material.SubtractScore(pieceIndex, squareIndex ^ 56);
            else
                _material.AddScore(pieceIndex, squareIndex);
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
        public static float Phase(short phaseValue)
        {
            return Math.Clamp((float)(PhaseSum - phaseValue) / PhaseSum, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        public static readonly int PhaseSum = 5000;
        public static readonly short[] PhaseValues = new short[6] { 0, 105, 311, 395, 879, 0 };

        //Tuned on 'data/quiet-labeled.epd'
        //MSE_SCALING = 100;
        //ITERATIONS = 80;
        //MATERIAL_ALPHA = 700;
        //PHASE_ALPHA = 200;
        //MATERIAL_BATCH = 100;
        //PHASE_BATCH = 5;
        //==================
        //MSE=0,241711475410
        //==================
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24173941526656895

    }
}
