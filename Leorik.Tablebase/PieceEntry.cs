using Leorik.Core;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using static Leorik.Core.Bitboard;

namespace Leorik.Tablebase
{
    class PieceEntry : BaseEntry
    {
        public bool KKEncoding;

        override public WinDrawLoss GetWDL(BoardState pos, bool blackSide, bool flip)
        {
            //struct EncInfo *ei = first_ei(be, type);
            int[] squares = new int[NumPieces];
            //size_t idx;
            //uint8_t flags = 0; // initialize to fix GCC warning
            //ei = &ei[bside];
            ref EncInfo ei = ref EncInfos[blackSide ? 1 : 0];
            FillSquares(pos, ei.Pieces, flip, 0, squares);
            long idx = Encode(squares, ref ei);
            Console.WriteLine($"idx = {idx}");
            //uint8_t* w = decompress_pairs(ei->precomp, idx);
            byte w0 = DecompressPairs(idx, ref ei);
            return (WinDrawLoss)(w0 - 2);
        }

        private byte DecompressPairs(long idx, ref EncInfo ei)
        {
            //TODO: could this early out happen earlier?
            if (ei.IndexBits == 0)
                return ei.ConstValue;

            Console.WriteLine($"idx = {idx}");
            Console.WriteLine($"idxBits = {ei.IndexBits}");
            long mainIdx = idx >> ei.IndexBits;
            Console.WriteLine($"mainIdx = {mainIdx}");
            long mask = (1L << ei.IndexBits) - 1;
            long sub = 1L << (ei.IndexBits - 1);
            long litIdx = (idx & mask) - sub;
            Console.WriteLine($"litIdx = {litIdx}");
            long block = File.ReadUInt32(ei.IndexTable + 6 * mainIdx);
            Console.WriteLine($"block = {block}");
            int idxOffset = File.ReadUInt16(ei.IndexTable + 6 * mainIdx + 4);
            Console.WriteLine($"idxOffset = {idxOffset}");

            litIdx += idxOffset;
            if (litIdx < 0)
            {
                while (litIdx < 0)
                {
                    long pos = ei.SizeTable + 2 * --block;
                    litIdx += File.ReadUInt16(pos) + 1;
                }
            }
            else
            {
                ushort size = File.ReadUInt16(ei.SizeTable + 2 * block);
                while (litIdx > size)
                {
                    litIdx -= size + 1;
                    size = File.ReadUInt16(ei.SizeTable + 2 * ++block);
                }
            }

            Console.WriteLine($"new litIdx = {litIdx}");
            Console.WriteLine($"new block = {block}");

            long blockPos = ei.Data + (block << ei.BlockSize);
            ulong code = BinaryPrimitives.ReverseEndianness(File.ReadUInt64(blockPos));
            long tmpPos = blockPos + sizeof(ulong);
            Console.WriteLine($"code = {code}");

            int m = ei.MinLen;
            int bitCnt = 0;
            uint sym;
            while (true)
            {
                int l = m;
                while (ei.Base[l - m] > code)
                    l++;
                Console.WriteLine($"IF {code} >= {ei.Base[l - m]}");
                sym = File.ReadUInt16(ei.Offset + (l - m) * 2);
                Console.WriteLine($"sym = {sym}, m = {m}, l = {l}");
                ulong shift = (code - ei.Base[l - m]) >> (64 - l);
                sym += (uint)shift;
                Console.WriteLine($"sym += (uint32_t)((code - base[l]) >> (64 - l)) = {sym}");
                if (litIdx < ei.SymLen[sym] + 1)
                    break;
                litIdx -= ei.SymLen[sym] + 1;
                code <<= l;
                bitCnt += l;
                if (bitCnt >= 32)
                {
                    bitCnt -= 32;
                    ulong tmp = BinaryPrimitives.ReverseEndianness(File.ReadUInt32(tmpPos));
                    Console.WriteLine($"!!! tmp = {tmp}");
                    tmpPos += sizeof(uint);
                    code |= (tmp << bitCnt);
                }
                Console.WriteLine($"code = {code}");
                Console.WriteLine($"sym = {sym}");
                Console.WriteLine($"bitCnt = {bitCnt}");
            }

            while (ei.SymLen[sym] != 0)
            {
                Console.WriteLine($"WHILE {ei.SymLen[sym]} != 0");
                long symPos = ei.SymPat + 3 * sym;
                uint w0 = File.ReadByte(symPos);
                uint w1 = File.ReadByte(symPos + 1);
                uint w2 = File.ReadByte(symPos + 2);
                Console.WriteLine($"w[0] = {w0}");
                Console.WriteLine($"w[1] = {w1}");
                Console.WriteLine($"w[2] = {w2}");
                uint s1 = ((w1 & 0xf) << 8) | w0;
                int slen = ei.SymLen[s1] + 1;
                if (litIdx >= slen)
                {
                    litIdx -= slen;
                    sym = (w2 << 4) | (w1 >> 4);
                }
                else
                    sym = s1;
                Console.WriteLine($"sym = {sym}");
                Console.WriteLine($"litIdx = {litIdx}");
            }
            long resultPos = ei.SymPat + 3 * sym;
            byte result = File.ReadByte(resultPos);
            Console.WriteLine($"1. return &symPat[3 * sym] = {result}");
            return result;
        }        

        private long Encode(int[] p, ref EncInfo ei)
        {
            //enc == PIECE_ENC
            long idx = 0;
            int k;

            for (int i = 0; i < NumPieces; i++)
                Console.WriteLine($" p[{i}] = {p[i]}");

            if ((p[0] & 0x04) > 0)
                for (int i = 0; i < NumPieces; i++)
                    p[i] ^= 0x07;

            if ((p[0] & 0x20) > 0)
                for (int i = 0; i < NumPieces; i++)
                    p[i] ^= 0x38;

            for (int i = 0; i < NumPieces; i++)
                if (OffDiag[p[i]] != 0)
                {
                    if (OffDiag[p[i]] > 0 && i < (KKEncoding ? 2 : 3))
                        for (int j = 0; j < NumPieces; j++)
                            p[j] = FlipDiag[p[j]];
                    break;
                }

            if (KKEncoding)
            {
                idx = KKIdx[Triangle[p[0]], p[1]];
                k = 2;
            }
            else
            {
                int s1 = (p[1] > p[0]) ? 1 : 0;
                int s2 = (p[2] > p[0] ? 1 : 0) + (p[2] > p[1] ? 1 : 0);
                Console.WriteLine($"s1={s1} s2={s2}");
                for (int i = 0; i < NumPieces; i++)
                    Console.WriteLine($" p[{i}] = {p[i]}");

                if (OffDiag[p[0]] != 0)
                    idx = Triangle[p[0]] * 63 * 62 + (p[1] - s1) * 62 + (p[2] - s2);
                else if (OffDiag[p[1]] != 0)
                    idx = 6 * 63 * 62 + Diag[p[0]] * 28 * 62 + Lower[p[1]] * 62 + p[2] - s2;
                else if (OffDiag[p[2]] != 0)
                    idx = 6 * 63 * 62 + 4 * 28 * 62 + Diag[p[0]] * 7 * 28 + (Diag[p[1]] - s1) * 28 + Lower[p[2]];
                else
                    idx = 6 * 63 * 62 + 4 * 28 * 62 + 4 * 7 * 28 + Diag[p[0]] * 7 * 6 + (Diag[p[1]] - s1) * 6 + (Diag[p[2]] - s2);
                k = 3;
            }
            Console.WriteLine($"{idx} *= {ei.Factor[0]} = {(ulong)idx * ei.Factor[0]}");
            idx *= (long)ei.Factor[0];
            Console.WriteLine($"idx={idx}");

            for (; k < NumPieces;)
            {
                int t = k + ei.Norm[k];
                for (int i = k; i < t; i++)
                    for (int j = i + 1; j < t; j++)
                        if (p[i] > p[j])
                            (p[i], p[j]) = (p[j], p[i]); //Swap
                long s = 0;
                for (int i = k; i < t; i++)
                {
                    int sq = p[i];
                    int skips = 0;
                    for (int j = 0; j < k; j++)
                        skips += (sq > p[j]) ? 1 : 0;
                    s += (long)Encoding.Binomial[i - k + 1, sq - skips];
                }
                idx += s * (long)ei.Factor[k];
                k = t;
            }

            return idx;
        }

        public override bool InitTable(MemoryMappedViewAccessor file)
        {
            File = file;

            //Byte 4:
            file.Read(4, out byte byte4);
            //bit 0 is set for a non-symmetric table, i.e.separate wtm and btm.
            bool split = (byte4 & 0x01) > 0;
            Debug.Assert(Symmetric != split);
            //bit 1 is set for a pawnful table.
            bool hasPawns = (byte4 & 0x02) > 0;
            Debug.Assert(!hasPawns);
            //bits 4 - 7: number of pieces N(N = 5 for KRPvKR)
            int numPieces = byte4 >> 4;
            Debug.Assert(NumPieces == numPieces);

            Console.WriteLine($"int num = num_tables(be, type) = {1}"); //WDL
            EncInfos = new EncInfo[split ? 2 : 1];
            long position = 5;
            InitEncodingInfo(ref EncInfos[0], file, position, 0);
            if (split)
                InitEncodingInfo(ref EncInfos[1], file, position, 4);

            Console.WriteLine($"data += {NumPieces + 1}");
            //align the position within the tablebase file to a multiple of 2 bytes.
            position = Align(position + NumPieces + 1, 2);
            position = EncInfos[0].SetupPairs(file, position);
            if (!Symmetric)
                position = EncInfos[1].SetupPairs(file, position);

            Console.WriteLine($"IndexTable[0] = {file.ReadByte(position)}");
            EncInfos[0].IndexTable = position;
            position += EncInfos[0].Sizes.Indices;
            if (!Symmetric)
            {
                Console.WriteLine($"IndexTable[1] = {file.ReadByte(position)}");
                EncInfos[1].IndexTable = position;
                position += EncInfos[1].Sizes.Indices;
            }

            Console.WriteLine($"SizeTable[0] = {file.ReadByte(position)}");
            EncInfos[0].SizeTable = position;
            position += EncInfos[0].Sizes.Blocks;
            if (!Symmetric)
            {
                Console.WriteLine($"SizeTable[1] = {file.ReadByte(position)}");
                EncInfos[1].SizeTable = position;
                position += EncInfos[1].Sizes.Blocks;
            }

            position = Align(position, 64);
            Console.WriteLine($"SizeTable[0] = {file.ReadByte(position)}");
            EncInfos[0].Data = position;
            position += EncInfos[0].Sizes.Data;
            if (!Symmetric)
            {
                position = Align(position, 64);
                Console.WriteLine($"SizeTable[1] = {file.ReadByte(position)}");
                EncInfos[1].Data = position;
                position += EncInfos[1].Sizes.Data;
            }

            return true;
        }

        private long InitEncodingInfo(ref EncInfo ei, MemoryMappedViewAccessor file, long position, int shift)
        {
            byte data;
            ei.Pieces = new byte[NumPieces];
            ei.Norm = new byte[NumPieces];
            ei.Factor = new ulong[NumPieces];

            file.Read(position++, out data);
            int order = (data >> shift) & 0x0F;
            int k = ei.Norm[0] = KKEncoding ? (byte)2 : (byte)3;

            for (int i = 0; i < NumPieces; i++)
            {
                file.Read(position++, out data);
                ei.Pieces[i] = (byte)((data >> shift) & 0x0F);
            }

            for (int i = k; i < NumPieces; i += ei.Norm[i])
                for (int j = i; j < NumPieces && ei.Pieces[j] == ei.Pieces[i]; j++)
                    ei.Norm[i]++;

            int n = 64 - k;
            ulong f = 1;
            ulong mul = (ulong)(KKEncoding ? 462 : 31332);

            for (int i = 0; k < NumPieces || i == order; i++)
            {
                if (i == order)
                {
                    ei.Factor[0] = f;
                    f *= mul;
                }
                else
                {
                    ei.Factor[k] = f;
                    f *= Encoding.Subfactor(ei.Norm[k], (ulong)n);
                    n -= ei.Norm[k];
                    k += ei.Norm[k];
                }
            }

            ei.Sizes.TableBase = (long)f;
            Console.WriteLine($"<-- init_enc_info(Piece) = {f}");
            //position += be->num + 1 + (be->hasPawns && be->pawns[1]);
            return position;
        }

        static int[] OffDiag = {
            0,-1,-1,-1,-1,-1,-1,-1,
            1, 0,-1,-1,-1,-1,-1,-1,
            1, 1, 0,-1,-1,-1,-1,-1,
            1, 1, 1, 0,-1,-1,-1,-1,
            1, 1, 1, 1, 0,-1,-1,-1,
            1, 1, 1, 1, 1, 0,-1,-1,
            1, 1, 1, 1, 1, 1, 0,-1,
            1, 1, 1, 1, 1, 1, 1, 0
        };


        static int[] Triangle = {
            6, 0, 1, 2, 2, 1, 0, 6,
            0, 7, 3, 4, 4, 3, 7, 0,
            1, 3, 8, 5, 5, 8, 3, 1,
            2, 4, 5, 9, 9, 5, 4, 2,
            2, 4, 5, 9, 9, 5, 4, 2,
            1, 3, 8, 5, 5, 8, 3, 1,
            0, 7, 3, 4, 4, 3, 7, 0,
            6, 0, 1, 2, 2, 1, 0, 6
        };

        static int[] FlipDiag = {
            0,  8, 16, 24, 32, 40, 48, 56,
            1,  9, 17, 25, 33, 41, 49, 57,
            2, 10, 18, 26, 34, 42, 50, 58,
            3, 11, 19, 27, 35, 43, 51, 59,
            4, 12, 20, 28, 36, 44, 52, 60,
            5, 13, 21, 29, 37, 45, 53, 61,
            6, 14, 22, 30, 38, 46, 54, 62,
            7, 15, 23, 31, 39, 47, 55, 63
        };


        static int[] Lower = {
            28,  0,  1,  2,  3,  4,  5,  6,
            0, 29,  7,  8,  9, 10, 11, 12,
            1,  7, 30, 13, 14, 15, 16, 17,
            2,  8, 13, 31, 18, 19, 20, 21,
            3,  9, 14, 18, 32, 22, 23, 24,
            4, 10, 15, 19, 22, 33, 25, 26,
            5, 11, 16, 20, 23, 25, 34, 27,
            6, 12, 17, 21, 24, 26, 27, 35
        };

        static int[] Diag = {
            0,  0,  0,  0,  0,  0,  0,  8,
            0,  1,  0,  0,  0,  0,  9,  0,
            0,  0,  2,  0,  0, 10,  0,  0,
            0,  0,  0,  3, 11,  0,  0,  0,
            0,  0,  0, 12,  4,  0,  0,  0,
            0,  0, 13,  0,  0,  5,  0,  0,
            0, 14,  0,  0,  0,  0,  6,  0,
            15,  0,  0,  0,  0,  0,  0,  7
        };

        static short[,] KKIdx = {{
            -1, -1, -1,  0,  1,  2,  3,  4,
            -1, -1, -1,  5,  6,  7,  8,  9,
            10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, 23, 24, 25,
            26, 27, 28, 29, 30, 31, 32, 33,
            34, 35, 36, 37, 38, 39, 40, 41,
            42, 43, 44, 45, 46, 47, 48, 49,
            50, 51, 52, 53, 54, 55, 56, 57
        },{
            58, -1, -1, -1, 59, 60, 61, 62,
            63, -1, -1, -1, 64, 65, 66, 67,
            68, 69, 70, 71, 72, 73, 74, 75,
            76, 77, 78, 79, 80, 81, 82, 83,
            84, 85, 86, 87, 88, 89, 90, 91,
            92, 93, 94, 95, 96, 97, 98, 99,
            100,101,102,103,104,105,106,107,
            108,109,110,111,112,113,114,115
        },{
            116,117, -1, -1, -1,118,119,120,
            121,122, -1, -1, -1,123,124,125,
            126,127,128,129,130,131,132,133,
            134,135,136,137,138,139,140,141,
            142,143,144,145,146,147,148,149,
            150,151,152,153,154,155,156,157,
            158,159,160,161,162,163,164,165,
            166,167,168,169,170,171,172,173
        },{
            174, -1, -1, -1,175,176,177,178,
            179, -1, -1, -1,180,181,182,183,
            184, -1, -1, -1,185,186,187,188,
            189,190,191,192,193,194,195,196,
            197,198,199,200,201,202,203,204,
            205,206,207,208,209,210,211,212,
            213,214,215,216,217,218,219,220,
            221,222,223,224,225,226,227,228
        },{
            229,230, -1, -1, -1,231,232,233,
            234,235, -1, -1, -1,236,237,238,
            239,240, -1, -1, -1,241,242,243,
            244,245,246,247,248,249,250,251,
            252,253,254,255,256,257,258,259,
            260,261,262,263,264,265,266,267,
            268,269,270,271,272,273,274,275,
            276,277,278,279,280,281,282,283
        },{
            284,285,286,287,288,289,290,291,
            292,293, -1, -1, -1,294,295,296,
            297,298, -1, -1, -1,299,300,301,
            302,303, -1, -1, -1,304,305,306,
            307,308,309,310,311,312,313,314,
            315,316,317,318,319,320,321,322,
            323,324,325,326,327,328,329,330,
            331,332,333,334,335,336,337,338
        },{
            -1, -1,339,340,341,342,343,344,
            -1, -1,345,346,347,348,349,350,
            -1, -1,441,351,352,353,354,355,
            -1, -1, -1,442,356,357,358,359,
            -1, -1, -1, -1,443,360,361,362,
            -1, -1, -1, -1, -1,444,363,364,
            -1, -1, -1, -1, -1, -1,445,365,
            -1, -1, -1, -1, -1, -1, -1,446
        },{
            -1, -1, -1,366,367,368,369,370,
            -1, -1, -1,371,372,373,374,375,
            -1, -1, -1,376,377,378,379,380,
            -1, -1, -1,447,381,382,383,384,
            -1, -1, -1, -1,448,385,386,387,
            -1, -1, -1, -1, -1,449,388,389,
            -1, -1, -1, -1, -1, -1,450,390,
            -1, -1, -1, -1, -1, -1, -1,451
        },{
            452,391,392,393,394,395,396,397,
            -1, -1, -1, -1,398,399,400,401,
            -1, -1, -1, -1,402,403,404,405,
            -1, -1, -1, -1,406,407,408,409,
            -1, -1, -1, -1,453,410,411,412,
            -1, -1, -1, -1, -1,454,413,414,
            -1, -1, -1, -1, -1, -1,455,415,
            -1, -1, -1, -1, -1, -1, -1,456
        },{
            457,416,417,418,419,420,421,422,
            -1,458,423,424,425,426,427,428,
            -1, -1, -1, -1, -1,429,430,431,
            -1, -1, -1, -1, -1,432,433,434,
            -1, -1, -1, -1, -1,435,436,437,
            -1, -1, -1, -1, -1,459,438,439,
            -1, -1, -1, -1, -1, -1,460,440,
            -1, -1, -1, -1, -1, -1, -1,461
        }};
    }
}
