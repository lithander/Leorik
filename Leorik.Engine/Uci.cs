using Leorik.Core;
using System.Text;

namespace Leorik.Engine
{
    public static class Uci
    {
        public static bool Silent = false;

        public static void Write(string message)
        {
            if (!Silent)
                Console.WriteLine(message);
        }

        public static void BestMove(Move move, Variant variant)
        {
            Write($"bestmove {Notation.GetMoveName(move, variant)}");
        }

        public static void Info(int depth, int score, long nodes, int timeMs, int multiPV, List<Move> pv, Variant variant)
        {
            double tS = Math.Max(1, timeMs) / 1000.0;
            int nps = (int)(nodes / tS);

            Write($"info depth {depth} multipv {multiPV} score {ScoreToString(score)} nodes {nodes} nps {nps} time {timeMs} pv {Join(pv, variant)}");
        }

        private static string Join(IEnumerable<Move> moves, Variant variant)
        {
            var sb = new StringBuilder(128);
            foreach (var move in moves)
            {
                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(Notation.GetMoveName(move, variant));
            }
            return sb.ToString();
        }

        private static string ScoreToString(int score)
        {
            if (Evaluation.IsCheckmate(score))
            {
                int sign = Math.Sign(score);
                int moves = Evaluation.GetMateDistance(score);
                return $"mate {sign * moves}";
            }

            return $"cp {score}";
        }

        public static void Log(string message)
        {
            Write($"info string {message}");
        }
    }
}
