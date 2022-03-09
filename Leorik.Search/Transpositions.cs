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
            public ScoreType Type;   //1 Byte
            public Move BestMove;    //3 Bytes    4 Bytes?
            //=================================?==========
            //                        16 Bytes   24 Bytes!
        }

        public const short HISTORY_DEPTH = 255;
        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;

        static bool Find(in ulong hash, out int index)
        {
            index = (int)(hash % (ulong)_table.Length);
            if (_table[index].Hash != hash)
                index ^= 1; //try other slot

            if (_table[index].Hash != hash)
                return false; //both slots missed

            //a table hit resets the age
            _table[index].Age = 0;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Index(in ulong hash)
        {
            int index = (int)(hash % (ulong)_table.Length);
            ref HashEntry e0 = ref _table[index];
            ref HashEntry e1 = ref _table[index ^ 1];

            if (e0.Hash == hash)
                return index;

            if (e1.Hash == hash)
                return index ^ 1;

            //raise age of both and choose the older, shallower entry!
            return (++e0.Age - e0.Depth) > (++e1.Age - e1.Depth) ? index : index ^ 1;
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
            Array.Clear(_table, 0, _table.Length);
        }

        public static void Store(ulong zobristHash, int depth, int ply, int alpha, int beta, int score, Move bestMove)
        {
            ref HashEntry entry = ref _table[Index(zobristHash)];

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (entry.Hash != zobristHash || bestMove != default)
                entry.BestMove = bestMove;

            entry.Hash = zobristHash;
            entry.Depth = depth < 0 ? default : (byte)depth;
            entry.Age = 0;

            if (score >= beta)
            {
                entry.Type = ScoreType.GreaterOrEqual;
                entry.Score = AdjustMateDistance(beta, ply);
            }
            else if (score <= alpha)
            {
                entry.Type = ScoreType.LessOrEqual;
                entry.Score = AdjustMateDistance(alpha, ply);
            }
            else
            {
                entry.Type = ScoreType.Exact;
                entry.Score = AdjustMateDistance(score, ply);
            }
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
            bestMove = Find(position.ZobristHash, out int index) ? _table[index].BestMove : default;
            return bestMove != default;
        }

        public static bool GetScore(ulong zobristHash, int depth, int ply, int alpha, int beta, out Move bestMove, out int score)
        {
            bestMove = default;
            score = 0;
            if (!Find(zobristHash, out int index))
                return false;

            ref HashEntry entry = ref _table[index];
            bestMove = entry.BestMove;

            if (entry.Depth < depth)
                return false;

            score = AdjustMateDistance(entry.Score, -ply);

            //1.) score is exact and within window
            if (entry.Type == ScoreType.Exact)
                return true;
            //2.) score is below floor
            if (entry.Type == ScoreType.LessOrEqual && score <= alpha)
                return true; //failLow
            //3.) score is above ceiling
            if (entry.Type == ScoreType.GreaterOrEqual && score >= beta)
                return true; //failHigh

            return false;
        }
    }
}
