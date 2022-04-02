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
        public float Phase => Math.Clamp((float)(_phaseValue - Phase0) / (Phase1 - Phase0), 0, 1);
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
            double phase = (double)(phaseValue - Phase0) / (Phase1 - Phase0);
            return midgameScore + Math.Clamp(phase, 0, 1) * endgameScore;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        public static readonly int Phase0 = 5255;
        public static readonly int Phase1 = 435;

        public static readonly short[] PhaseValues = new short[6] { 0, 155, 305, 405, 1050, 0 };

        //20k epochs, fixed MG (no phase) + phase * EG
        //Leorik's MSE(data) with MSE_SCALING = 100 on the dataset: 0,24612965562919142
        public static readonly short[] MidgameTables = new short[6 * 64]{
          100,  100,  100,  100,  100,  100,  100,  100,
          167,  216,  140,  180,  167,  233,  132,   67,
           71,   86,  110,  112,  150,  141,  108,   64,
           55,   86,   82,   97,   99,   88,   95,   54,
           44,   71,   71,   87,   92,   84,   89,   52,
           45,   70,   71,   66,   79,   83,  111,   65,
           37,   73,   55,   52,   61,  102,  115,   55,
          100,  100,  100,  100,  100,  100,  100,  100,

          150,  231,  244,  280,  346,  234,  292,  206,
          253,  272,  394,  349,  345,  382,  318,  312,
          290,  381,  363,  391,  415,  441,  400,  371,
          312,  345,  345,  381,  365,  398,  349,  345,
          313,  327,  341,  340,  355,  348,  347,  319,
          304,  316,  339,  337,  346,  345,  351,  311,
          293,  268,  314,  323,  325,  343,  309,  314,
          215,  306,  267,  289,  306,  300,  309,  299,

          329,  335,  256,  250,  307,  290,  314,  334,
          329,  368,  342,  321,  382,  389,  368,  316,
          339,  379,  389,  394,  383,  403,  389,  356,
          349,  360,  373,  405,  393,  392,  363,  353,
          345,  371,  367,  376,  388,  365,  361,  352,
          350,  366,  367,  369,  366,  380,  369,  360,
          355,  367,  366,  353,  361,  370,  384,  348,
          311,  349,  339,  333,  339,  341,  312,  329,

          473,  497,  462,  514,  514,  463,  447,  470,
          467,  473,  501,  506,  520,  531,  471,  489,
          429,  453,  467,  469,  457,  488,  502,  455,
          412,  425,  442,  468,  460,  469,  441,  430,
          399,  408,  427,  434,  442,  435,  454,  422,
          394,  414,  425,  420,  443,  441,  444,  414,
          392,  423,  418,  429,  439,  449,  434,  375,
          418,  426,  439,  448,  450,  437,  415,  419,

          868,  879,  919,  879,  978,  967,  938,  938,
          886,  866,  900,  908,  898,  970,  925,  954,
          893,  892,  915,  916,  942,  982,  963,  965,
          870,  877,  892,  891,  907,  926,  905,  906,
          894,  877,  896,  893,  901,  904,  909,  906,
          883,  906,  893,  899,  898,  906,  918,  907,
          868,  892,  914,  902,  909,  915,  903,  910,
          897,  882,  891,  912,  884,  874,  877,  846,

          -34,   56,   45,   12,  -53,  -33,   30,   12,
           24,   64,   32,   71,   12,   -5,  -32,  -33,
           31,   44,   71,    9,   24,   45,   62,  -21,
           -6,  -10,   -8,  -24,  -36,  -33,  -26,  -93,
          -34,   11,  -25,  -99,  -95,  -60,  -59,  -78,
            8,   26,  -13,  -44,  -55,  -39,    5,   -8,
           38,   42,   11,  -49,  -28,    2,   45,   52,
           25,   74,   45,  -40,   31,   -7,   62,   57,
        };

        public static short[] EndgameTables = new short[6 * 64]{
            0,    0,    0,    0,    0,    0,    0,    0,
           98,   43,  108,   44,   59,  -19,  112,  195,
          118,  108,   67,   49,   -3,    5,   66,  111,
           72,   33,   26,    3,   -5,   11,   15,   56,
           64,   33,   21,    1,   -5,    2,    7,   40,
           54,   32,   18,   30,   15,    6,  -20,   20,
           72,   31,   46,   50,   43,   -8,  -20,   31,
            0,    0,    0,    0,    0,    0,    0,    0,

           70,    2,   20,  -41,  -96,    6,  -70,  -24,
           -9,   -9, -143,  -77,  -81, -131,  -69,  -89,
          -48, -125,  -79, -110, -149, -174, -145, -138,
          -55,  -72,  -53,  -91,  -74, -116,  -73,  -89,
          -57,  -61,  -53,  -46,  -68,  -63,  -72,  -66,
          -57,  -49,  -71,  -53,  -66,  -76,  -97,  -64,
          -59,  -15,  -54,  -58,  -57,  -88,  -58,  -88,
           24,  -88,  -18,  -34,  -58,  -51,  -86,  -82,

          -57,  -63,   23,   39,  -23,   -7,  -35,  -68,
          -49,  -83,  -50,  -40,  -97, -108,  -82,  -41,
          -50,  -95, -100, -106,  -95, -111, -103,  -67,
          -63,  -64,  -75, -108,  -91,  -96,  -75,  -66,
          -63,  -81,  -66,  -69,  -93,  -69,  -79,  -70,
          -72,  -78,  -70,  -71,  -66,  -90,  -86,  -84,
          -80,  -94,  -82,  -64,  -71,  -92, -109,  -84,
          -42,  -67,  -74,  -51,  -60,  -71,  -31,  -53,

           42,   13,   59,    0,    0,   50,   66,   40,
           44,   40,   11,    5,  -23,  -29,   39,   17,
           80,   59,   43,   42,   50,   12,   -2,   45,
           97,   84,   74,   36,   46,   36,   58,   76,
          107,  102,   84,   74,   60,   62,   40,   72,
          106,   90,   75,   84,   53,   50,   48,   76,
          108,   75,   86,   77,   58,   45,   59,  124,
           79,   81,   69,   60,   53,   61,   87,   64,

           29,   56,    5,   77,  -78,  -67,  -54,  -38,
           11,   67,   44,   46,   70,  -53,    0,  -54,
            1,   23,   -3,   34,   13,  -65,  -51,  -63,
           54,   63,   42,   71,   69,   27,   72,   47,
            0,   66,   40,   72,   48,   45,   33,   27,
           24,  -27,   39,   22,   31,   26,    0,   11,
           25,    3,  -36,   -5,  -10,  -26,  -38,  -55,
          -15,    0,   -1,  -52,   29,    5,   21,   42,

          -43,  -93,  -80,  -36,   45,   40,  -24,  -21,
          -36,  -57,  -23,  -66,   -2,   36,   59,   47,
          -27,  -32,  -60,   -3,  -14,  -13,  -25,   33,
           -5,   25,   30,   45,   58,   61,   52,  102,
           13,  -17,   44,  127,  124,   85,   73,   73,
          -28,  -33,   23,   64,   80,   58,    2,    0,
          -69,  -56,   -7,   61,   42,    3,  -52,  -74,
          -81, -117,  -70,   29,  -61,   -6,  -93, -110,
        };
    }
}
