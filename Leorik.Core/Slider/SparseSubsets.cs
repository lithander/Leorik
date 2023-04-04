using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core.Slider
{
    public static class SparseSubsets
    {
        static readonly ulong[] Subset = new ulong[4 * 64 * 64];//16.384

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            int offset = square * 128;
            ulong dSub = Subset[offset + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.DiagonalMask[square])];
            ulong aSub = Subset[offset + 64 + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.AntiDiagonalMask[square])];
            return dSub | aSub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            int offset = 8192 + square * 128;
            ulong hSub = Subset[offset + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.HorizontalMask[square])];
            ulong vSub = Subset[offset + 64 + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.VerticalMask[square])];
            return hSub | vSub;
        }

        //Table initialization

        static SparseSubsets()
        {
            //Sparse Bishop-Subsets
            for (int square = 0; square < 64; square++)
            {
                ulong offset = (ulong)square * 128;
                ulong mask = Blocker.DiagonalMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[offset + i] = Classic.DiagonalSubset(occupation, square);
                }

                mask = Blocker.AntiDiagonalMask[square];
                patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[offset + 64 + i] = Classic.AntiDiagonalSubset(occupation, square);
                }
            }

            //Sparse Rook-Subsets
            for (int square = 0; square < 64; square++)
            {
                ulong offset = 8192 + (ulong)square * 128;
                ulong mask = Blocker.HorizontalMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[offset + i] = Classic.HorizontalSubset(occupation, square);
                }

                mask = Blocker.VerticalMask[square];
                patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[offset + 64 + i] = Classic.VerticalSubset(occupation, square);
                }
            }
        }
    }
}