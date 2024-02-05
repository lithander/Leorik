using Leorik.Core;

namespace Leorik.Tuning
{
    class Quiesce
    {
        private BoardState[] Positions;
        private Move[] Moves;
        private BoardState[] Results;
        private int DeepestPly;
        private int MaxDepth;

        public Quiesce()
        {
            const int MAX_PLY = 50;
            const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058

            Moves = new Move[MAX_PLY * MAX_MOVES];
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();

            Results = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Results[i] = new BoardState();
        }

        public BoardState QuiescePosition(BoardState position, int maxQDepth)
        {
            const int MIN_ALPHA = -Evaluation.CheckmateScore;
            const int MAX_BETA = Evaluation.CheckmateScore;
            Positions[0].Copy(position);
            Results[0].Copy(position);
            DeepestPly = 0;
            MaxDepth = maxQDepth;
            MoveGen moveGen = new MoveGen(Moves, 0);
            int score = EvaluateQuiet(0, MIN_ALPHA, MAX_BETA, moveGen);
            if (DeepestPly > MaxDepth)
                return null; //We couldn't quiesce the position within the allowed depth

            int score2 = position.SideToMoveScore();
            if (score != score2)
                return null; //This typically means a checkmate or stalemate - we ignore those!

            return Results[0];
        }

        private int EvaluateQuiet(int ply, int alpha, int beta, MoveGen moveGen)
        {
            BoardState current = Positions[ply];

            DeepestPly = Math.Max(DeepestPly, ply);
            if(DeepestPly > MaxDepth)
                return current.SideToMoveScore();

            bool inCheck = current.InCheck();
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = current.SideToMoveScore();

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                {
                    Results[ply].Copy(current);
                    alpha = standPatScore;
                }
            }

            //To quiesce a position play all the Captures!
            BoardState next = Positions[ply + 1];
            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestCapture(i, moveGen.Next);
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                    {
                        //int score2 = (int)current.SideToMove * Results[ply + 1].Eval.Score;
                        //Debug.Assert(score == score2);
                        Results[ply].Copy(Results[ply + 1]);
                        alpha = score;
                    }
                }
            }

            if (!inCheck)
                return alpha;

            //Play Quiets only when in check!
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.QuickPlay(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(ply + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                    {
                        //int score2 = (int)current.SideToMove * Results[ply + 1].Eval.Score;
                        //Debug.Assert(score == score2);
                        Results[ply].Copy(Results[ply + 1]);
                        alpha = score;
                    }
                }
            }

            return movesPlayed ? alpha : Evaluation.MatedScore(ply);
        }

        private void PickBestCapture(int first, int end)
        {
            //find the best move...
            int best = first;
            int bestScore = Moves[first].MvvLvaScore();
            for (int i = first + 1; i < end; i++)
            {
                int score = Moves[i].MvvLvaScore();
                if (score >= bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            //...swap best with first
            if (best != first)
            {
                Move temp = Moves[best];
                Moves[best] = Moves[first];
                Moves[first] = temp;
            }
        }
    }
}
