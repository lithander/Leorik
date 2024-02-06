using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Leorik.Core.Slider
{
    public static class Pext
    {
        //https://www.chessprogramming.org/BMI2#PEXTBitboards

        static readonly ulong[] Attacks = new ulong[5248 + 102400];
        static readonly ulong[] BishopOffset = new ulong[64];
        static readonly ulong[] RookOffset = new ulong[64];


        static Pext()
        {
            ulong index = 0;

            //Bishop-Attacks
            for (int square = 0; square < 64; square++)
            {
                BishopOffset[square] = index;
                ulong bishopMask = Blocker.BishopMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(bishopMask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, bishopMask);
                    Attacks[index++] = Classic.BishopAttacks(occupation, square);
                }
            }

            //Rook-Attacks
            for (int square = 0; square < 64; square++)
            {
                RookOffset[square] = index;
                ulong rookMask = Blocker.RookMask[square];
                ulong patterns = 1UL << BitOperations.PopCount(rookMask);
                for (ulong i = 0; i < patterns; i++)
                {
                    ulong occupation = Bmi2.X64.ParallelBitDeposit(i, rookMask);
                    Attacks[index++] = Classic.RookAttacks(occupation, square);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occupation, int square)
        {
            return Attacks[RookOffset[square] + Bmi2.X64.ParallelBitExtract(occupation, Blocker.RookMask[square])];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occupation, int square)
        {
            return Attacks[BishopOffset[square] + Bmi2.X64.ParallelBitExtract(occupation, Blocker.BishopMask[square])];
        }
    }
}
