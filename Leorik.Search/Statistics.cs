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
        ulong[] _moveCounts = new ulong[100 << 4];
        ulong[] _betaCutoffs = new ulong[100 << 4];
        int _maxPly = 0;

        public void LogMove(int ply, IterativeSearch.Stage stage)
        {
            _maxPly = Math.Max(ply, _maxPly);
            _moveCounts[Key(ply, stage)]++;
        }

        private int Key(int ply, IterativeSearch.Stage stage)
        {
            return (ply << 4) + (int)stage;
        }

        internal void LogBetaCutoff(int ply, IterativeSearch.Stage stage)
        {
            _maxPly = Math.Max(ply, _maxPly);
            _betaCutoffs[Key(ply, stage)]++;
        }


        public void Clear()
        {
            int last = (_maxPly + 1) << 4;
            for(int i = 0; i <= last; i++)
                _moveCounts[i] = 0;
        }

        public void PrintLog()
        {
            PrintBetaCuttoffs();
            PrintCutoffSummary();
            PrintMoveCounts();
            PrintMoveSummary();
            PrintBetaCuttoffRatios();
            Console.WriteLine();
        }

        private void PrintBetaCuttoffs()
        {
            Console.WriteLine("[BETA CUTOFFS]");
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));
            for (int ply = 0; ply <= _maxPly; ply++)
            {
                Console.Write($"Ply: {ply,2} || ");
                for (int i = 0; i < stageNames.Length; i++)
                {
                    ulong count = _betaCutoffs[Key(ply, (IterativeSearch.Stage)i)];
                    Console.Write($"{stageNames[i]}:{count,9} | ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("=============================================================");
        }

        private void PrintBetaCuttoffRatios()
        {
            Console.WriteLine("[BETA CUTOFF RATIOS]");
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));
            for (int ply = 0; ply <= _maxPly; ply++)
            {
                Console.Write($"Ply: {ply,2} || ");
                for (int i = 0; i < stageNames.Length; i++)
                {
                    ulong cutoffs = _betaCutoffs[Key(ply, (IterativeSearch.Stage)i)];
                    ulong count = _moveCounts[Key(ply, (IterativeSearch.Stage)i)];
                    float percentage = 100 * cutoffs / (float)(count);
                    Console.Write($"{stageNames[i]}:{percentage,8:0.00}% | ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("=============================================================");
        }

        private void PrintMoveCounts()
        {
            Console.WriteLine("[MOVE COUNTS]");
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));
            for (int ply = 0; ply <= _maxPly; ply++)
            {
                Console.Write($"Ply: {ply,2} || ");
                for (int i = 0; i < stageNames.Length; i++)
                {
                    ulong count = _moveCounts[Key(ply, (IterativeSearch.Stage)i)];
                    Console.Write($"{stageNames[i]}:{count,9} | ");
                }
                ulong sq = _moveCounts[Key(ply, IterativeSearch.Stage.SortedQuiets)];
                ulong q = _moveCounts[Key(ply, IterativeSearch.Stage.Quiets)];
                float percentage = 100 * sq / (float)(sq + q);
                Console.Write($"Sorted:{percentage:0.00}%");
                Console.WriteLine();
            }
            Console.WriteLine("=============================================================");
        }

        private void PrintMoveSummary()
        {
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));

            Console.Write($"Total   || ");
            ulong total = 0;
            for (int i = 0; i < stageNames.Length; i++)
            {
                ulong count = 0;
                for (int ply = 0; ply <= _maxPly; ply++)
                    count += _moveCounts[Key(ply, (IterativeSearch.Stage)i)];

                total += count;
                Console.Write($"{stageNames[i]}:{count,9} | ");
            }
            float percentage;
            ulong sq = 0;
            ulong q = 0;
            for (int ply = 0; ply <= _maxPly; ply++)
            {
                sq += _moveCounts[Key(ply, IterativeSearch.Stage.SortedQuiets)];
                q += _moveCounts[Key(ply, IterativeSearch.Stage.Quiets)];
            }
            percentage = 100 * sq / (float)(sq + q);
            Console.Write($"Sorted:{percentage:0.00}%");
            Console.WriteLine();

            Console.Write($"        || ");
            for (int i = 0; i < stageNames.Length; i++)
            {
                ulong count = 0;
                for (int ply = 0; ply <= _maxPly; ply++)
                    count += _moveCounts[Key(ply, (IterativeSearch.Stage)i)];

                percentage = 100 * count / (float)(total);
                Console.Write($"{stageNames[i]}:{percentage,8:0.00}% | ");
            }
            Console.WriteLine();
        }

        private void PrintCutoffSummary()
        {
            string[] stageNames = Enum.GetNames(typeof(IterativeSearch.Stage));

            Console.Write($"Cutoffs || ");
            ulong total = 0;
            for (int i = 0; i < stageNames.Length; i++)
            {
                ulong count = 0;
                for (int ply = 0; ply <= _maxPly; ply++)
                    count += _betaCutoffs[Key(ply, (IterativeSearch.Stage)i)];

                total += count;
                Console.Write($"{stageNames[i]}:{count,9} | ");
            }
            Console.WriteLine();

            float percentage;
            Console.Write($"        || ");
            for (int i = 0; i < stageNames.Length; i++)
            {
                ulong count = 0;
                for (int ply = 0; ply <= _maxPly; ply++)
                    count += _betaCutoffs[Key(ply, (IterativeSearch.Stage)i)];

                percentage = 100 * count / (float)(total);
                Console.Write($"{stageNames[i]}:{percentage,8:0.00}% | ");
            }
            Console.WriteLine();
        }
    }
}
