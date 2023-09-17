using Leorik.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        [StructLayout(LayoutKind.Explicit)]
        public struct HashEntry
        {
            [FieldOffset(0)]
            public ulong Key;        //8 Bytes
            [FieldOffset(8)]
            public short Score;      //2 Bytes
            [FieldOffset(10)]
            public byte Depth;       //1 Byte
            [FieldOffset(11)]
            public byte Age;         //1 Byte
            [FieldOffset(12)]
            public int MoveAndType;  //4 Byte
            //=================================
            //                        16 Bytes
            [FieldOffset(8)]
            public ulong Data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ScoreType GetScoreType() => (ScoreType)(MoveAndType >> 29);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Move GetMove() => (Move)(MoveAndType & 0x1FFFFFFF);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong GetHash() => Key ^ Data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetHash(ulong hash) => Key = hash ^ Data;
        }

        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;
        static byte _age = 0;

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
            int index = (int)(zobristHash % (ulong)_table.Length);
            HashEntry entry = _table[index];
            if (entry.GetHash() != zobristHash)
            {
                HashEntry other = _table[index ^ 1];
                //other slot is correct -OR- both are wrong but other is shallower -OR- undercut replacement prevents deep entries from sticking around forever.
                if (other.GetHash() == zobristHash || entry.Depth >= other.Depth && entry.Depth + entry.Age > depth + 1 + _age)
                {
                    index ^= 1;
                    entry = other;
                }
            }

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (bestMove == default && entry.GetHash() == zobristHash)
                bestMove = entry.GetMove();

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

            entry.SetHash(zobristHash);
            _table[index] = entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Encode(Move move, ScoreType scoreType)
        {
            //move can be fully can be represented by a 32bit integer. But only 29bits are used. Enough room to also store the scoreType
            return (int)move | (int)scoreType << 29;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short AdjustMateDistance(int score, int ply)
        {
            //a checkmate score is reduced by the number of plies from the root so that shorter mates are preferred
            //but when we talk about a position being 'mate in X' then X is independent of the root distance. So we store
            //the score relative to the position by adding the current ply to the encoded mate distance (from the root).

            if (Evaluation.IsCheckmate(score))
                return (short)(score + Math.Sign(score) * ply);
            else
                return (short)score;
        }

        public static bool GetBestMove(BoardState position, out Move bestMove)
        {
            ulong hash = position.ZobristHash;
            int index = (int)(hash % (ulong)_table.Length);

            HashEntry entry = _table[index];
            if (entry.GetHash() == hash)
                return (bestMove = entry.GetMove()) != default;

            entry = _table[index ^ 1]; //try other slot
            if (entry.GetHash() == hash)
                return (bestMove = entry.GetMove()) != default;

            bestMove = default;
            return false; //both slots missed
        }

        public static bool GetScore(ulong zobristHash, int depth, int ply, int alpha, int beta, out Move bestMove, out int score)
        {
            //init out paramters to allow early out
            bestMove = default;
            score = 0;

            int index = (int)(zobristHash % (ulong)_table.Length);

            HashEntry entry = _table[index];

            if (entry.GetHash() != zobristHash)
            {
                index ^= 1; //try other slot
                entry = _table[index];
            }

            if (entry.GetHash() != zobristHash)
                return false; //both slots missed

            //a table hit resets the age
            entry.Age = _age;
            entry.SetHash(zobristHash);
            _table[index] = entry;

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
