using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Tablebase
{
    public static class Encoding
    {
        static byte[,] PawnTwist = new byte[,]
        {
            {
                0,  0,  0,  0,  0,  0,  0,  0,
                47, 35, 23, 11, 10, 22, 34, 46,
                45, 33, 21,  9,  8, 20, 32, 44,
                43, 31, 19,  7,  6, 18, 30, 42,
                41, 29, 17,  5,  4, 16, 28, 40,
                39, 27, 15,  3,  2, 14, 26, 38,
                37, 25, 13,  1,  0, 12, 24, 36,
                0,  0,  0,  0,  0,  0,  0,  0
            },
            {
                0,  0,  0,  0,  0,  0,  0,  0,
                47, 45, 43, 41, 40, 42, 44, 46,
                39, 37, 35, 33, 32, 34, 36, 38,
                31, 29, 27, 25, 24, 26, 28, 30,
                23, 21, 19, 17, 16, 18, 20, 22,
                15, 13, 11,  9,  8, 10, 12, 14,
                7,  5,  3,  1,  0,  2,  4,  6,
                0,  0,  0,  0,  0,  0,  0,  0
            }
        };

        public static ulong[,] Binomial = new ulong[7, 64];
        public static ulong[,,] PawnIdx = new ulong[2, 6, 24];
        public static ulong[,] PawnFactorFile = new ulong[6, 4];
        public static ulong[,] PawnFactorRank = new ulong[6, 6];

        static Encoding()
        {
            InitIndices();
        }

        private static void InitIndices()
        {
            ulong i, j, k;

            // Binomial[k][n] = Bin(n, k)
            for (i = 0; i < 7; i++)
                for (j = 0; j < 64; j++)
                {
                    ulong f = 1;
                    ulong l = 1;
                    for (k = 0; k < i; k++)
                    {
                        f *= (j - k);
                        l *= (k + 1);
                    }
                    Binomial[i, j] = f / l;
                }

            for (i = 0; i < 6; i++)
            {
                ulong s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[0, i, j] = s;
                    int twist = PawnTwist[0, (1 + (j % 6)) * 8 + (j / 6)];
                    s += Binomial[i, twist];
                    if ((j + 1) % 6 == 0)
                    {
                        PawnFactorFile[i, j / 6] = s;
                        s = 0;
                    }
                }
            }

            for (i = 0; i < 6; i++)
            {
                ulong s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[1, i, j] = s;
                    int twist = PawnTwist[1, (1 + (j / 4)) * 8 + (j % 4)];
                    s += Binomial[i, twist];
                    if ((j + 1) % 4 == 0)
                    {
                        PawnFactorRank[i, j / 4] = s;
                        s = 0;
                    }
                }
            }
        }

        // Count number of placements of k like pieces on n squares
        public static ulong Subfactor(ulong k, ulong n)
        {
            ulong f = n;
            ulong l = 1;
            for (ulong i = 1; i < k; i++)
            {
                f *= n - i;
                l *= i + 1;
            }

            return f / l;
        }
    }
}
