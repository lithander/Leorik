using Leorik.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public class IterativeSearch
    {
        private const int R_NULL_MOVE = 2; //how much do we reduce the search depth after passing a move? (null move pruning)
        private const int MAX_GAIN_PER_PLY = 70; //upper bound on the amount of cp you can hope to make good in a ply
        private const int FUTILITY_RANGE = 4;


        private const int MIN_ALPHA = -Evaluation.CheckmateScore;
        private const int MAX_BETA = Evaluation.CheckmateScore;
        private const int MAX_PLY = 99;
        private const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058

        private BoardState[] Positions;
        private Move[] Moves;
        private Move[] PrincipalVariations;
        private KillSwitch _killSwitch;
        private long _maxNodes;
        private KillerMoves _killers;

        public static int MaxDepth => MAX_PLY;
        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move BestMove => PrincipalVariations[0];
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get();
        public Span<Move> PrincipalVariation => GetFirstPVfromBuffer(PrincipalVariations, Depth);


        public IterativeSearch(BoardState board, long maxNodes = long.MaxValue)
        {
            _maxNodes = maxNodes;
            _killers = new KillerMoves(2);

            Moves = new Move[MAX_PLY * MAX_MOVES];

            //PV-length = depth + (depth - 1) + (depth - 2) + ... + 1
            const int d = MAX_PLY + 1;
            PrincipalVariations = new Move[(d * d + d) / 2];

            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Positions[0].Copy(board);
        }

        public void Search(int maxDepth)
        {
            while (Depth < maxDepth)
                SearchDeeper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ForcedCut(int depth)
        {
            return depth >= MAX_PLY - 1 || NodesVisited >= _maxNodes || _killSwitch.Get();
        }

        private static Span<Move> GetFirstPVfromBuffer(Move[] pv, int depth)
        {
            //return moves until the first is 'default' move but not more than 'depth' number of moves
            int end = Array.IndexOf(pv, default, 0, depth);
            return new Span<Move>(pv, 0, end >= 0 ? end : depth);
        }

        internal void StorePVinTT()
        {
            var position = Positions[0].Clone();
            var pv = PrincipalVariation;
            for (int ply = 0; ply < pv.Length; ply++)
            {
                int sideToMoveScore = (int)position.SideToMove * Score;
                if(!Transpositions.GetScore(position.ZobristHash, Depth - ply, ply, MIN_ALPHA, MAX_BETA, out _, out _))
                    Transpositions.Store(position.ZobristHash, Depth - ply, ply, MIN_ALPHA, MAX_BETA, sideToMoveScore, pv[ply]);
                position.Play(pv[ply]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexPV(int ply)
        {
            //Detailed Description:
            //https://github.com/lithander/Leorik/blob/b3236087fbc87e1915725c23ff349e46dfedd0f2/Leorik.Search/IterativeSearchNext.cs
            return Depth * ply - (ply * ply - ply) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtendPV(int ply, Move move)
        {
            int index = IndexPV(ply);
            PrincipalVariations[index] = move;
            int stride = Depth - ply;
            int from = index + stride - 1;
            for (int i = 1; i < stride; i++)
                PrincipalVariations[index + i] = PrincipalVariations[from + i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TruncatePV(int ply)
        {
            PrincipalVariations[IndexPV(ply)] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SearchDeeper(Func<bool>? killSwitch = null)
        {
            StorePVinTT();

            Depth++;
            _killers.Expand(Depth);
            _killSwitch = new KillSwitch(killSwitch);
            Move bestMove = PrincipalVariations[0];
            MoveGen moveGen = new MoveGen(Moves, 0);
            int score = Evaluate(0, Depth, MIN_ALPHA, MAX_BETA, moveGen, ref bestMove);

            Score = (int)Positions[0].SideToMove * score;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickBestMove(int first, int end)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FailLow(int ply, int remaining, int alpha, MoveGen moveGen)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -alpha - 1, -alpha, moveGen) <= alpha;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FailHigh(int ply, int remaining, int beta, MoveGen moveGen)
        {
            return -EvaluateTT(ply + 1, remaining - 1, -beta, -beta + 1, moveGen) >= beta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateTT(int ply, int remaining, int alpha, int beta, MoveGen moveGen)
        {
            if (remaining <= 0)
                return EvaluateQuiet(ply, alpha, beta, moveGen);

            if (ForcedCut(ply))
                return Positions[ply].RelativeScore();

            TruncatePV(ply);
            ulong hash = Positions[ply].ZobristHash;
            if (Transpositions.GetScore(hash, remaining, ply, alpha, beta, out Move bm, out int ttScore))
                return ttScore;

            int score = Evaluate(ply, remaining, alpha, beta, moveGen, ref bm);
            Transpositions.Store(hash, remaining, ply, alpha, beta, score, bm);
            return score;
        }

        enum Stage { New, Captures, Killers, Quiets }

        struct PlayState
        {
            public Stage Stage;
            public int Next;
            public byte PlayedMoves;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PlayState InitPlay(ref MoveGen moveGen, ref Move pvMove)
        {
            return new PlayState { Stage = Stage.New, Next = moveGen.Collect(pvMove) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Play(int ply, ref PlayState state, ref MoveGen moveGen)
        {
            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            
            while(true)
            {
                if (state.Next == moveGen.Next)
                {
                    switch (state.Stage)
                    {
                        case Stage.New:
                            state.Next = moveGen.CollectCaptures(current);
                            state.Stage = Stage.Captures;
                            continue;
                        case Stage.Captures:
                            state.Next = moveGen.CollectPlayableKillers(current, _killers.GetSpan(ply));
                            state.Stage = Stage.Killers;
                            continue;
                        case Stage.Killers:
                            state.Next = moveGen.CollectQuiets(current);
                            state.Stage = Stage.Quiets;
                            continue;
                        case Stage.Quiets:
                            return false;
                    }
                }

                if (state.Stage == Stage.Captures)
                    PickBestMove(state.Next, moveGen.Next);

                if (next.Play(current, ref Moves[state.Next++]))
                {
                    state.PlayedMoves++;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayNullMove(int ply)
        {
            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            next.PlayNullMove(current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AllowNullMove(int ply)
        {
            //if the previous iteration found a mate we check the first few plys without null move to try and find the shortest mate or escape
            return Evaluation.IsCheckmate(Score) ? (ply > Depth / 4) : true;
        }

        private int Evaluate(int ply, int remaining, int alpha, int beta, MoveGen moveGen, ref Move bm)
        {
            NodesVisited++;

            BoardState current = Positions[ply];
            BoardState next = Positions[ply + 1];
            bool inCheck = current.InCheck();
            
            //consider null move pruning first           
            if (remaining >= 2 && !inCheck && beta < MAX_BETA && AllowNullMove(ply))
            {
                //if stm can skip a move and the position is still "too good" we can assume that this position, after making a move, would also fail high
                PlayNullMove(ply);
                if (FailHigh(ply, remaining - R_NULL_MOVE, beta, moveGen))
                    return beta;
            }

            //init staged move generation and play all moves
            PlayState playState = InitPlay(ref moveGen, ref bm);
            while (Play(ply, ref playState, ref moveGen))
            {
                //some nodes near the leaves that appear hopeless can be skipped without evaluation
                if (remaining <= FUTILITY_RANGE && !inCheck && playState.Stage >= Stage.Captures)
                {
                    //if the static eval looks much worse than alpha also skip it
                    float futilityMargin = alpha - remaining * MAX_GAIN_PER_PLY;
                    if (next.RelativeScore(current.SideToMove) < futilityMargin && !next.InCheck())
                        continue;
                }

                //moves after the PV move are unlikely to raise alpha! searching with a null-sized window around alpha first...
                if (playState.PlayedMoves > 1 && remaining >= 2 && FailLow(ply, remaining, alpha, moveGen))
                    continue;

                //...but if it does not we have to research it!
                int score = -EvaluateTT(ply + 1, remaining - 1, -beta, -alpha, moveGen);

                if (score > alpha)
                {
                    bm = Moves[playState.Next-1];
                    ExtendPV(ply, bm);

                    if(playState.Stage >= Stage.Killers)
                        _killers.Add(ply, bm);
                    
                    alpha = score;
                }

                //beta cutoff?
                if (score >= beta)
                    return beta;
            }

            //checkmate or draw?
            if (playState.PlayedMoves == 0)
                return inCheck ? Evaluation.Checkmate(ply) : 0;

            return alpha;
        }

        private int EvaluateQuiet(int depth, int alpha, int beta, MoveGen moveGen)
        {
            NodesVisited++;

            BoardState current = Positions[depth];
            bool inCheck = current.InCheck();
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = current.RelativeScore();

                if (standPatScore >= beta)
                    return beta;

                if (standPatScore > alpha)
                    alpha = standPatScore;
            }

            if (ForcedCut(depth))
                return current.RelativeScore();

            //To quiesce a position play all the Captures!
            BoardState next = Positions[depth + 1];
            bool movesPlayed = false;
            for (int i = moveGen.CollectCaptures(current); i < moveGen.Next; i++)
            {
                PickBestMove(i, moveGen.Next);
                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            //TODO: if (!inCheck || movesPlayed)
            if (!inCheck)
                return alpha;

            //Play Quiets only when in check!
            for (int i = moveGen.CollectQuiets(current); i < moveGen.Next; i++)
            {
                if (next.PlayWithoutHash(current, ref Moves[i]))
                {
                    movesPlayed = true;
                    int score = -EvaluateQuiet(depth + 1, -beta, -alpha, moveGen);

                    if (score >= beta)
                        return beta;

                    if (score > alpha)
                        alpha = score;
                }
            }

            return movesPlayed ? alpha : Evaluation.Checkmate(depth);

            //NOTE: this kind of stale-mate detection was a loss for Leorik 1.0!
            //if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
            //    return 0;
        }
    }
}