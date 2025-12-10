using System.Runtime.CompilerServices;
using static Leorik.Core.Bitboard;

namespace Leorik.Core
{
    public struct Evaluation
    {
        public const int CheckmateBase = 9000;
        public const int CheckmateScore = 9999;
        public const int ScoreBounds = 10000;

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
        public static int MatedScore(int ply) => ply - CheckmateScore;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MateScore(int ply) => CheckmateScore - ply;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsufficientMatingMaterial(BoardState board)
        {
            //https://support.chess.com/article/128-what-does-insufficient-mating-material-mean
            if (PopCount(board.Black | board.White) > 4)
                return false;

            ulong black = board.Black & ~board.Kings;
            ulong white = board.White & ~board.Kings;

            //lone king vs two knights
            if (white == 0 && (board.Knights & black) == black && PopCount(black) == 2)
                return true;
            if (black == 0 && (board.Knights & white) == white && PopCount(white) == 2)
                return true;

            //if both sides have any one of the following, and there are no pawns on the board: 
            //    * lone king
            //    * king and bishop
            //    * king and knight
            ulong norb = board.Knights | board.Bishops;
            return (black == 0 || ((norb & black) == black && PopCount(black) == 1)) && //Black is K or K(N|B)
                   (white == 0 || ((norb & white) == white && PopCount(white) == 1));   //White is K or K(N|B)
        }
    }
}
