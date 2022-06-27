using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Search
{
    public static class Transpositions
    {
        public enum ScoreType : byte
        {
            GreaterOrEqual,
            LessOrEqual,
            Exact
        }

        public struct HashEntry
        {
            public ulong Hash;       //8 Bytes
            public short Score;      //2 Bytes
            public byte Depth;       //1 Byte
            public byte Age;         //1 Byte
            public int MoveAndType;  //4 Byte
            //=================================
            //                        16 Bytes 

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ScoreType GetScoreType() => (ScoreType)(MoveAndType >> 29);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Move GetMove() => (Move)(MoveAndType & 0x1FFFFFFF);

        }

        public const short HISTORY_DEPTH = 255;
        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;
        static byte _age = 0;

        static bool Find(in ulong hash, out int index)
        {
            index = (int)(hash % (ulong)_table.Length);
            if (_table[index].Hash != hash)
                index ^= 1; //try other slot

            if (_table[index].Hash != hash)
                return false; //both slots missed

            //a table hit resets the age
            _table[index].Age = _age;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Index(in ulong hash, int newDepth)
        {
            int index = (int)(hash % (ulong)_table.Length);
            ref HashEntry e0 = ref _table[index];
            ref HashEntry e1 = ref _table[index ^ 1];

            if (e0.Hash == hash)
                return index;

            if (e1.Hash == hash)
                return index ^ 1;

            if (e0.Depth < e1.Depth)
                return index;

            //undercut replacement prevents deep entries from sticking around forever.
            if (newDepth >= e0.Depth - 1 - (byte)(_age - e0.Age))
                return index;

            return index ^ 1;
        }

        static Transpositions()
        {
            Resize(DEFAULT_SIZE_MB);
        }

        public static void Resize(int hashSizeMBytes)
        {
            int length = (hashSizeMBytes * 1024 * 1024) / ENTRY_SIZE;
            _table = new HashEntry[length];
        }

        public static void Clear()
        {
            _age = 0;
            Array.Clear(_table, 0, _table.Length);
        }

        public static void IncreaseAge()
        {
            _age++;
        }

        public static void Store(ulong zobristHash, int depth, int ply, int alpha, int beta, int score, Move bestMove)
        {
            ref HashEntry entry = ref _table[Index(zobristHash, depth)];

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (entry.Hash == zobristHash && bestMove == default)
            {
                bestMove = entry.GetMove();
            }

            entry.Hash = zobristHash;
            entry.Depth = depth < 0 ? default : (byte)depth;
            entry.Age = _age;

            if (score >= beta)
            {
                entry.MoveAndType = Encode(bestMove, ScoreType.GreaterOrEqual);
                entry.Score = AdjustMateDistance(beta, ply);
            }
            else if (score <= alpha)
            {
                entry.MoveAndType = Encode(bestMove, ScoreType.LessOrEqual);
                entry.Score = AdjustMateDistance(alpha, ply);
            }
            else
            {
                entry.MoveAndType = Encode(bestMove, ScoreType.Exact);
                entry.Score = AdjustMateDistance(score, ply);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Encode(Move move, ScoreType scoreType)
        {
            //move can be fully can be represented by a 32bit integer. But only 29bits are used. Enough room to also store the scoreType
            return (int)move | (int)scoreType << 29;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short AdjustMateDistance(int score, int ply)
        {
            //a checkmate score is reduced by the number of plies from the root so that shorter mates are preferred
            //but when we talk about a position being 'mate in X' then X is independent of the root distance. So we store
            //the score relative to the position by adding the current ply to the encoded mate distance (from the root).

            if (Evaluation.IsCheckmate(score))
                return (short)(score + Math.Sign(score) * ply);
            else
                return (short)score;
        }

        public static void StoreHistory(BoardState position)
        {
            Store(position.ZobristHash, HISTORY_DEPTH, 0, -Evaluation.CheckmateScore, +Evaluation.CheckmateScore, 0, default);
        }

        public static bool GetBestMove(BoardState position, out Move bestMove)
        {
            bestMove = Find(position.ZobristHash, out int index) ? _table[index].GetMove() : default;
            return bestMove != default;
        }

        public static bool GetScore(ulong zobristHash, int depth, int ply, int alpha, int beta, out Move bestMove, out int score)
        {
            //init out paramters to allow early out
            bestMove = default;
            score = 0;

            //does an entry exist?
            if (!Find(zobristHash, out int index))
                return false;

            ref HashEntry entry = ref _table[index];
            //yes! we can at least use the best move
            bestMove = entry.GetMove();

            //is the quality of the entry good enough?
            if (entry.Depth < depth)
                return false;

            //is what we know enough to give a definitive answer within the alpha/beta window?
            score = AdjustMateDistance(entry.Score, -ply);
            switch (entry.GetScoreType())
            {
                //2.) we don't know the score but that's okay if it is >= beta
                case ScoreType.GreaterOrEqual:
                    return score >= beta;
                //3.) we don't know the score but that's okay if it is <= alpha
                case ScoreType.LessOrEqual:
                    return score <= alpha;
                //1.) score is exact and within window
                default: //ScoreType.Exact
                    return true;
            }
        }
    }
}
