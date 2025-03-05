using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Leorik.Core.Slider
{
    //This is a C# port of GeneticObstructionDiffV2.hpp which is an improvement over ObstructionDifference
    //found by Daniel Inführ in 2022 using C++ Abstract Sytax Tree Sifting
    public static class ObstructionDifference
    {
        // Shift a bitmask by a given number of ranks (each rank is 8 bits)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MaskShift(ulong bb, int ranks) => ranks > 0 ? bb >> (ranks << 3) : bb << (-(ranks << 3));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Horizontal(int sq) => 0xFFUL << (sq & 56);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Vertical(int sq) => 0x0101010101010101UL << (sq & 7);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Diagonal(int sq) => MaskShift(0x8040201008040201UL, (sq & 7) - (sq >> 3));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AntiDiagonal(int sq) => MaskShift(0x0102040810204080UL, 7 - (sq & 7) - (sq >> 3));

        // Struct holding precomputed ray information for each square
        private readonly struct Masks
        {
            public readonly ulong Horizontal, Vertical, Diagonal, AntiDiagonal;
            public readonly ulong HorzLo, HorzUp;
            public readonly ulong VertLo, VertUp;
            public readonly ulong DiagLo, DiagUp;
            public readonly ulong AntiLo, AntiUp;

            public Masks(int square)
            {
                // 'low' are all bits with index < sq; 'up' are all bits with index >= sq (except the square itself)
                ulong low = (1UL << square) - 1;
                ulong up = 0xFFFFFFFFFFFFFFFEUL << square; // 0xFFFFFFFFFFFFFFFE has a zero in the LSB

                // Compute basic ray masks
                Horizontal   = Horizontal(square);
                Vertical = Vertical(square);
                Diagonal = Diagonal(square);
                AntiDiagonal = AntiDiagonal(square);

                // Precompute upper bits of each ray.
                HorzUp = Horizontal & up;
                VertUp = Vertical & up;
                DiagUp = Diagonal & up;
                AntiUp = AntiDiagonal & up;

                // Precompute lower bits and OR with 1 to ensure the mask is never zero.
                HorzLo = (Horizontal & low) | 1UL;
                VertLo = (Vertical & low) | 1UL;
                DiagLo = (Diagonal & low) | 1UL;
                AntiLo = (AntiDiagonal & low) | 1UL;

                // Remove the piece’s own bit from the main ray masks.
                Horizontal   ^= (1UL << square);
                Vertical     ^= (1UL << square);
                Diagonal     ^= (1UL << square);
                AntiDiagonal ^= (1UL << square);
            }
        }

        // Precompute masks for all 64 squares.
        private static readonly Masks[] AttackMasks = new Masks[64];
        static ObstructionDifference()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                AttackMasks[sq] = new Masks(sq);
            }
        }

        // Compute the attacked squares along a given ray.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong LineAttack(ulong lower, ulong upper, ulong mask)
        {
            // msb is the most significant blocker found in 'lower'
            ulong msb = 0x8000000000000000UL >> BitOperations.LeadingZeroCount(lower);
            return mask & (upper ^ (upper - msb));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RookAttacks(ulong occ, int square)
        {
            // Ensure occ is nonzero in its LSB (this avoids underflow in subtraction)
            occ |= 1UL;
            ref Masks r = ref AttackMasks[square];
            return LineAttack(occ & r.HorzLo, occ & r.HorzUp, r.Horizontal) |
                   LineAttack(occ & r.VertLo, occ & r.VertUp, r.Vertical);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BishopAttacks(ulong occ, int square)
        {
            occ |= 1UL;
            ref Masks r = ref AttackMasks[square];
            return LineAttack(occ & r.DiagLo, occ & r.DiagUp, r.Diagonal) |
                   LineAttack(occ & r.AntiLo, occ & r.AntiUp, r.AntiDiagonal);
        }
    }
}