namespace Leorik.Core.Slider
{
    static class Blocker
    {
        const int FILE_A = 0;
        const int FILE_H = 7;
        const int RANK_1 = 0;
        const int RANK_8 = 7;

        public static readonly ulong[] DiagonalMask = new ulong[64];
        public static readonly ulong[] AntiDiagonalMask = new ulong[64];
        public static readonly ulong[] HorizontalMask = new ulong[64];
        public static readonly ulong[] VerticalMask = new ulong[64];
        public static readonly ulong[] BishopMask = new ulong[64];
        public static readonly ulong[] RookMask = new ulong[64];

        static Blocker()
        {
            //Init Masks
            for (int square = 0; square < 64; square++)
            {
                int x = Bitboard.File(square);
                int y = Bitboard.Rank(square);

                //DiagonalMask
                for (int dx = x + 1, dy = y + 1; dx < FILE_H && dy < RANK_8; dx++, dy++)
                    DiagonalMask[square] |= 1UL << dx + dy * 8;
                for (int dx = x - 1, dy = y - 1; dx > FILE_A && dy > RANK_1; dx--, dy--)
                    DiagonalMask[square] |= 1UL << dx + dy * 8;

                //AntiDiagonalMask
                for (int dx = x - 1, dy = y + 1; dx > FILE_A && dy < RANK_8; dx--, dy++)
                    AntiDiagonalMask[square] |= 1UL << dx + dy * 8;
                for (int dx = x + 1, dy = y - 1; dx < FILE_H && dy > RANK_1; dx++, dy--)
                    AntiDiagonalMask[square] |= 1UL << dx + dy * 8;

                BishopMask[square] = DiagonalMask[square] | AntiDiagonalMask[square];

                //HorizontalMask
                for (int dx = FILE_A + 1; dx < FILE_H; dx++)
                    if (dx != x)
                        HorizontalMask[square] |= 1UL << dx + y * 8;

                //VerticalMask
                for (int dy = RANK_1 + 1; dy < RANK_8; dy++)
                    if (dy != y)
                        VerticalMask[square] |= 1UL << x + dy * 8;

                RookMask[square] = HorizontalMask[square] | VerticalMask[square];

            }
        }
    }
}
