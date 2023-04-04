using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core.Slider
{
    public static class DenseSubsets
    {
        static readonly ulong[] Subset = new ulong[1664 + 5120]; //6784

        static readonly int[] DiagonalOffset = new int[64];
        static readonly int[] AntiDiagonalOffset = new int[64];
        static readonly int[] HorizontalOffset = new int[64];
        static readonly int[] VerticalOffset = new int[64];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            ulong hSub = Subset[HorizontalOffset[square] + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.HorizontalMask[square])];
            ulong vSub = Subset[VerticalOffset[square] + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.VerticalMask[square])];
            return hSub | vSub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            ulong dSub = Subset[DiagonalOffset[square] + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.DiagonalMask[square])];
            ulong aSub = Subset[AntiDiagonalOffset[square] + (int)Bmi2.X64.ParallelBitExtract(occupation, Blocker.AntiDiagonalMask[square])];
            return dSub | aSub;
        }

        //Table initialization

        static DenseSubsets()
        {
            //Init Subsets
            int index = 0;

            //Dense Bishop-Subsets
            for (int square = 0; square < 64; square++)
            {
                DiagonalOffset[square] = index;
                ulong mask = Blocker.DiagonalMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[index++] = Classic.DiagonalSubset(occupation, square);
                }

                AntiDiagonalOffset[square] = index;
                mask = Blocker.AntiDiagonalMask[square];
                patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[index++] = Classic.AntiDiagonalSubset(occupation, square);
                }
            }

            //Dense Rook-Subsets
            for (int square = 0; square < 64; square++)
            {
                HorizontalOffset[square] = index;
                ulong mask = Blocker.HorizontalMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[index++] = Classic.HorizontalSubset(occupation, square);
                }

                VerticalOffset[square] = index;
                mask = Blocker.VerticalMask[square];
                patterns = 1UL << BitOperations.PopCount(mask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, mask);
                    Subset[index++] = Classic.VerticalSubset(occupation, square);
                }
            }
        }
    }
}
