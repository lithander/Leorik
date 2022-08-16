using Leorik.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Search
{
    public class Statistics
    {
        int[] _moveCounts = new int[100 << 4];
        int _maxPly = 0;

        public void LogMove(int ply, IterativeSearch.Stage stage, Move move)
        {
            _maxPly = Math.Max(ply, _maxPly);
            _moveCounts[Key(ply, stage)]++;
        }

        public void Clear()
        {
            int last = (_maxPly + 1) << 4;
            for(int i = 0; i <= last; i++)
                _moveCounts[i] = 0;
        }

        public void PrintLog()
        {
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));
            for (int ply = 0; ply <= _maxPly; ply++)
            {
                Console.Write($"Ply: {ply, 1} || ");
                for (int i = 0; i < stageNames.Length; i++)
                {
                    int count = _moveCounts[Key(ply, (IterativeSearch.Stage)i)];
                    Console.Write($"{stageNames[i]}:{count, 9} | ");
                }
                int sq = _moveCounts[Key(ply, IterativeSearch.Stage.SortedQuiets)];
                int q = _moveCounts[Key(ply, IterativeSearch.Stage.Quiets)];
                float ratio = sq / (float)(sq + q);
                Console.Write($"SQ/Q:{ratio*100}%");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private int Key(int ply, IterativeSearch.Stage stage)
        {
            return (ply << 4) + (int)stage;
        }
    }
}
