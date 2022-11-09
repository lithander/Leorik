/*
Leorik's Syzygy Probing was implementated after studying Fathom: https://github.com/jdart1/Fathom

Without the invaluable work of Ronald de Man, Basil Falcinelli and Jon Dart Leorik's tablebase 
probing would look vastly different or not exist at all!
 */


//TODO: can we use long instead of ulong? Less casting, readable code


using Leorik.Core;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using static Leorik.Core.Bitboard;

namespace Leorik.Tablebase
{
    public enum WinDrawLoss
    {
        Loss = -2,
        LateLoss = -1,
        Draw = 0,
        LateWin = 1,
        Win = 2
    }

    public class EndgameFiles
    {
        Dictionary<string, MemoryMappedViewAccessor> _wdlFiles = new Dictionary<string, MemoryMappedViewAccessor>();
        Dictionary<string, MemoryMappedViewAccessor> _dtzFiles = new Dictionary<string, MemoryMappedViewAccessor>();

        public MemoryMappedViewAccessor GetDTZ(string egClass) => _dtzFiles[egClass];
        public MemoryMappedViewAccessor GeWDL(string egClass) => _wdlFiles[egClass];

        public void AddEndgames(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension == null)
                    continue;

                switch(extension.ToLowerInvariant())
                {
                    case ".rtbw":
                        AddWDL(file);
                        break;
                    case ".rtbz":
                        AddDTZ(file);
                        break;
                }
            }
        }

        private void AddDTZ(string filePath)
        {
            string egClass = Path.GetFileNameWithoutExtension(filePath);
            _dtzFiles[egClass] = Open(filePath);
            Console.WriteLine($"_dtzFiles[{egClass}] = {filePath}");
        }

        private void AddWDL(string filePath)
        {
            string egClass = Path.GetFileNameWithoutExtension(filePath);
            _wdlFiles[egClass] = Open(filePath);
            Console.WriteLine($"_wdlFiles[{egClass}] = {filePath}");
        }

        public static MemoryMappedViewAccessor Open(string path)
        {
            //var file = MemoryMappedFile.CreateFromFile(path);
            var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var file = MemoryMappedFile.CreateFromFile(
                stream, //include a readonly shared stream
                null, //not mapping to a name
                0L, //use the file's actual size
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false //close the provided in stream when done
            );
            return file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        }
    }

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

        public static ulong[,] Binomial = new ulong[7,64];
        public static ulong[,,] PawnIdx = new ulong[2,6,24];
        public static ulong[,] PawnFactorFile = new ulong[6,4];
        public static ulong[,] PawnFactorRank = new ulong[6,6];

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
                    Binomial[i,j] = f / l;
                }

            for (i = 0; i < 6; i++)
            {
                ulong s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[0,i,j] = s;
                    int twist = PawnTwist[0, (1 + (j % 6)) * 8 + (j / 6)];
                    s += Binomial[i, twist];
                    if ((j + 1) % 6 == 0)
                    {
                        PawnFactorFile[i,j / 6] = s;
                        s = 0;
                    }
                }
            }

            for (i = 0; i < 6; i++)
            {
                ulong s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[1,i,j] = s;
                    int twist = PawnTwist[1, (1 + (j / 4)) * 8 + (j % 4)];
                    s += Binomial[i,twist];
                    if ((j + 1) % 4 == 0)
                    {
                        PawnFactorRank[i,j / 4] = s;
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

    public class Syzygy
    {
        //const int TB_PIECES = 7
        //const int TB_HASHBITS = 12
        //const int TB_MAX_PIECE = 650
        //const int TB_MAX_PAWN = 861

        const int TB_PIECES = 5;
        const int TB_HASHBITS = 11;
        const int TB_MAX_PIECE = 254;
        const int TB_MAX_PAWN = 256;
        
        const int TB_MAX_SYMS = 4096;

        struct DataSize
        {
            public long TableBase;
            public long Indices;
            public long Blocks;
            public long Data;
        }

        struct EncInfo
        {
            //PAIRS DATA!
            public long Offset;
            public long SymPat;
            public int MinLen;
            public byte[] SymLen;
            public ulong[] Base;
            public byte IndexBits;
            public byte BlockSize;
            public byte ConstValue;
            //---
            public DataSize Sizes;
            public ulong[] Factor;
            public byte[] Pieces;
            public byte[] Norm;
            public long IndexTable;
            public long SizeTable;
            public long Data;

            internal long SetupPairs(MemoryMappedViewAccessor file, long position)
            {
                //TODO: consider reading a normal filestream
                byte flags = file.ReadByte(position);
                if ((flags & 0x80) > 0)
                {
                    Console.WriteLine($"A. flags = {flags:X}");
                    //d = (struct PairsData*)malloc(sizeof(struct PairsData));
                    IndexBits = 0;
                    //d->constValue[0] = type == WDL? data[1] : 0;
                    ConstValue = file.ReadByte(position + 1);
                    //d->constValue[1] = 0;
                    //*ptr = data + 2;
                    //size[0] = size[1] = size[2] = 0;
                    return position + 2;
                }

                Console.WriteLine($"B. flags = {flags:X}");
                byte blockSize = file.ReadByte(position + 1);
                Console.WriteLine($"blockSize = {blockSize}");
                byte idxBits = file.ReadByte(position + 2);
                Console.WriteLine($"idxBits = {idxBits}");
                uint realNumBlocks = file.ReadUInt32(position + 4);
                uint numBlocks = realNumBlocks + file.ReadByte(position + 3);
                Console.WriteLine($"realNumBlocks = {realNumBlocks}");
                Console.WriteLine($"numBlocks = {numBlocks}");
                int maxLen = file.ReadByte(position + 8);
                int minLen = file.ReadByte(position + 9);
                int h = maxLen - minLen + 1;
                Console.WriteLine($"maxLen = {maxLen}, minLen = {minLen}, h = {h}");
                //uint32_t numSyms = (uint32_t)read_le_u16(data + 10 + 2 * h);
                uint numSyms = file.ReadUInt16(position + 10 + 2 * h);
                MinLen = minLen;
                SymLen = new byte[numSyms];
                SymPat = position + 12 + 2 * h;
                Offset = position + 10;
                Base = new ulong[h];
                IndexBits = idxBits;
                BlockSize = blockSize;
                Console.WriteLine($"*ptr = &data[12 + 2 * h + 3 * numSyms + (numSyms & 1)] = &data[{12 + 2 * h + 3 * numSyms + (numSyms & 1)}]");

                long num_indices = (Sizes.TableBase + (1 << idxBits) - 1) >> idxBits;
                Sizes.Indices = 6 * num_indices;
                Sizes.Blocks = 2 * numBlocks;
                Sizes.Data = realNumBlocks << blockSize;
                Console.WriteLine($"size[0] = {Sizes.Indices}, size[1] = {Sizes.Blocks}, size[2] = {Sizes.Data}");

                //TODO: is tmp really necessary? Can't we check SymLen[] > 0?
                byte[] tmp = new byte[TB_MAX_SYMS];
                for (int i = 0; i < numSyms; i++)
                    if (tmp[i] == 0)
                        CalcSymLen(i, tmp, file);

                Base[h - 1] = 0;
                for(int i = h - 2; i >= 0; i--)
                {
                    ushort a = file.ReadUInt16(position + 10 + 2 * i);
                    ushort b = file.ReadUInt16(position + 12 + 2 * i);
                    Base[i] = (Base[i + 1] + a - b) / 2;
                }
                //DECOMP64
                for (int i = 0; i < h; i++)
                    Base[i] <<= 64 - (minLen + i);

                for (int i = 0; i < h; i++)
                    Console.WriteLine($"base[{i}] = {Base[i]}");

                return position + 12 + 2 * h + 3 * numSyms + (numSyms & 1);
            }

            void CalcSymLen(int s, byte[] tmp, MemoryMappedViewAccessor file)
            {
                long w = SymPat + 3 * s;
                byte w0 = file.ReadByte(w);
                byte w1 = file.ReadByte(w + 1);
                byte w2 = file.ReadByte(w + 2);
                int s2 = (w2 << 4) | (w1 >> 4);
                //uint8_t* w = d->symPat + 3 * s;
                //uint32_t s2 = (w[2] << 4) | (w[1] >> 4);
                if (s2 == 0x0fff)
                {
                    SymLen[s] = 0;
                }
                else
                {
                    int s1 = ((w1 & 0xf) << 8) | w0;
                    if (tmp[s1] == 0)
                        CalcSymLen(s1, tmp, file);
                    if (tmp[s2] == 0)
                        CalcSymLen(s2, tmp, file);
                    int len = SymLen[s1] + SymLen[s2] + 1;
                    Debug.Assert(len <= 255);
                    SymLen[s] = (byte)len;
                }
                Console.WriteLine($"--> d->symLen[{s}] = {SymLen[s]}");
                tmp[s] = 1;
            }
        }

        abstract class BaseEntry
        {
            public bool Symmetric;
            public ulong Key;
            public byte NumPieces;
            public EncInfo[] EncInfos;
            protected MemoryMappedViewAccessor File;

            abstract public bool InitTable(MemoryMappedViewAccessor file);
            abstract public WinDrawLoss GetWDL(BoardState pos, bool blackSide, bool flip);

            protected long Align(long position, int multiple)
            {
                //https://stackoverflow.com/questions/11642210/computing-padding-required-for-n-byte-alignment
                int n = multiple - 1;
                return (position + n) & ~n;
            }


            // p[i] is to contain the square 0-63 (A1-H8) for a piece of type
            // pc[i] ^ flip, where 1 = white pawn, ..., 14 = black king
            // if flip == true then pc ^ flip flips between white and black 
            // Pieces of the same type are guaranteed to be consecutive.
            protected void FillSquares(BoardState pos, byte[] pieces, bool flip, int mirror, int[] p)
            {
                const int BLACK = 9;

                for (int i = 0; i < NumPieces;)
                {
                    bool isWhite = (pieces[i] < BLACK) ^ flip;
                    for (ulong bits = GetBitboard(pos, isWhite, pieces[i]); bits != 0; bits = ClearLSB(bits))
                    {
                        int square = LSB(bits);
                        p[i++] = square ^ mirror;
                    }
                }
            }

            private ulong GetBitboard(BoardState pos, bool isWhite, byte pieceIndex)
            {
                ulong mask = isWhite ? pos.White : pos.Black;
                switch(pieceIndex & 7)
                {
                    case 1:
                        return pos.Pawns & mask;
                    case 2:
                        return pos.Knights & mask;
                    case 3:
                        return pos.Bishops & mask;
                    case 4:
                        return pos.Rooks & mask;
                    case 5:
                        return pos.Queens & mask;
                    case 6:
                        return pos.Kings & mask;
                }
                throw new Exception($"PieceIndex {pieceIndex} is not supported.");
            }
        }

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
                return (WinDrawLoss)(w0-2);
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
                if(litIdx < 0)
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
                while(true)
                {
                    int l = m;
                    while (ei.Base[l-m] > code) 
                        l++;
                    Console.WriteLine($"IF {code} >= {ei.Base[l-m]}");
                    sym = File.ReadUInt16(ei.Offset + (l-m) * 2);
                    Console.WriteLine($"sym = {sym}, m = {m}, l = {l}");
                    ulong shift = (code - ei.Base[l-m]) >> (64 - l);
                    sym += (uint)shift;
                    Console.WriteLine($"sym += (uint32_t)((code - base[l]) >> (64 - l)) = {sym}");
                    if (litIdx < ei.SymLen[sym] + 1)
                        break;
                    litIdx -= ei.SymLen[sym] + 1;
                    code <<= l;
                    bitCnt += l;
                    if(bitCnt >= 32)
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

                while(ei.SymLen[sym] != 0)
                {
                    Console.WriteLine($"WHILE {ei.SymLen[sym]} != 0");
                    long symPos = ei.SymPat + 3 * sym;
                    uint w0 = File.ReadByte(symPos);
                    uint w1 = File.ReadByte(symPos+1);
                    uint w2 = File.ReadByte(symPos+2);
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

            static short[,] KKIdx = {
            { -1, -1, -1,  0,  1,  2,  3,  4,
              -1, -1, -1,  5,  6,  7,  8,  9,
              10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, 23, 24, 25,
              26, 27, 28, 29, 30, 31, 32, 33,
              34, 35, 36, 37, 38, 39, 40, 41,
              42, 43, 44, 45, 46, 47, 48, 49,
              50, 51, 52, 53, 54, 55, 56, 57 },
            { 58, -1, -1, -1, 59, 60, 61, 62,
              63, -1, -1, -1, 64, 65, 66, 67,
              68, 69, 70, 71, 72, 73, 74, 75,
              76, 77, 78, 79, 80, 81, 82, 83,
              84, 85, 86, 87, 88, 89, 90, 91,
              92, 93, 94, 95, 96, 97, 98, 99,
             100,101,102,103,104,105,106,107,
             108,109,110,111,112,113,114,115},
            {116,117, -1, -1, -1,118,119,120,
             121,122, -1, -1, -1,123,124,125,
             126,127,128,129,130,131,132,133,
             134,135,136,137,138,139,140,141,
             142,143,144,145,146,147,148,149,
             150,151,152,153,154,155,156,157,
             158,159,160,161,162,163,164,165,
             166,167,168,169,170,171,172,173 },
            {174, -1, -1, -1,175,176,177,178,
             179, -1, -1, -1,180,181,182,183,
             184, -1, -1, -1,185,186,187,188,
             189,190,191,192,193,194,195,196,
             197,198,199,200,201,202,203,204,
             205,206,207,208,209,210,211,212,
             213,214,215,216,217,218,219,220,
             221,222,223,224,225,226,227,228 },
            {229,230, -1, -1, -1,231,232,233,
             234,235, -1, -1, -1,236,237,238,
             239,240, -1, -1, -1,241,242,243,
             244,245,246,247,248,249,250,251,
             252,253,254,255,256,257,258,259,
             260,261,262,263,264,265,266,267,
             268,269,270,271,272,273,274,275,
             276,277,278,279,280,281,282,283 },
            {284,285,286,287,288,289,290,291,
             292,293, -1, -1, -1,294,295,296,
             297,298, -1, -1, -1,299,300,301,
             302,303, -1, -1, -1,304,305,306,
             307,308,309,310,311,312,313,314,
             315,316,317,318,319,320,321,322,
             323,324,325,326,327,328,329,330,
             331,332,333,334,335,336,337,338 },
            { -1, -1,339,340,341,342,343,344,
              -1, -1,345,346,347,348,349,350,
              -1, -1,441,351,352,353,354,355,
              -1, -1, -1,442,356,357,358,359,
              -1, -1, -1, -1,443,360,361,362,
              -1, -1, -1, -1, -1,444,363,364,
              -1, -1, -1, -1, -1, -1,445,365,
              -1, -1, -1, -1, -1, -1, -1,446 },
            { -1, -1, -1,366,367,368,369,370,
              -1, -1, -1,371,372,373,374,375,
              -1, -1, -1,376,377,378,379,380,
              -1, -1, -1,447,381,382,383,384,
              -1, -1, -1, -1,448,385,386,387,
              -1, -1, -1, -1, -1,449,388,389,
              -1, -1, -1, -1, -1, -1,450,390,
              -1, -1, -1, -1, -1, -1, -1,451 },
            {452,391,392,393,394,395,396,397,
              -1, -1, -1, -1,398,399,400,401,
              -1, -1, -1, -1,402,403,404,405,
              -1, -1, -1, -1,406,407,408,409,
              -1, -1, -1, -1,453,410,411,412,
              -1, -1, -1, -1, -1,454,413,414,
              -1, -1, -1, -1, -1, -1,455,415,
              -1, -1, -1, -1, -1, -1, -1,456 },
            {457,416,417,418,419,420,421,422,
              -1,458,423,424,425,426,427,428,
              -1, -1, -1, -1, -1,429,430,431,
              -1, -1, -1, -1, -1,432,433,434,
              -1, -1, -1, -1, -1,435,436,437,
              -1, -1, -1, -1, -1,459,438,439,
              -1, -1, -1, -1, -1, -1,460,440,
              -1, -1, -1, -1, -1, -1, -1,461 }
            };

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
        }

        class PawnEntry : BaseEntry
        {
            public byte NumPawns;
            public byte NumOtherPawns;

            override public WinDrawLoss GetWDL(BoardState pos, bool blackSide, bool flip)
            {
                //struct EncInfo *ei = first_ei(be, type);
                //int p[TB_PIECES];
                //size_t idx;
                //int t = 0;
                //uint8_t flags = 0; // initialize to fix GCC warning
                //
                //int i = fill_squares(pos, ei->pieces, flip, flip ? 0x38 : 0, p, 0);
                //t = leading_pawn(p, be, FILE_ENC);
                //ei = &ei[t + 4 * bside];
                //while (i < be->num)
                //    i = fill_squares(pos, ei->pieces, flip, flip ? 0x38 : 0, p, i);
                //idx = encode_pawn_f(p, ei, be);

                //int result = w[0] - 2;
                //printf("X. int result = w[0] - 2 = %i\n", result);
                //return (int)w[0] - 2;
                return WinDrawLoss.Draw;

            }

            public override bool InitTable(MemoryMappedViewAccessor file)
            {
                //Byte 4:
                file.Read(4, out byte byte4);
                //bit 0 is set for a non-symmetric table, i.e.separate wtm and btm.
                bool split = (byte4 & 0x01) > 0;
                Debug.Assert(Symmetric != split);
                //bit 1 is set for a pawnful table.
                bool hasPawns = (byte4 & 0x02) > 0;
                Debug.Assert(hasPawns);
                //bits 4 - 7: number of pieces N(N = 5 for KRPvKR)
                int numPieces = byte4 >> 4;
                Debug.Assert(NumPieces == numPieces);

                const int numTables = 4;
                EncInfos = new EncInfo[Symmetric ? 4 : 8];
                Console.WriteLine($"int num = num_tables(be, type) = {numTables}");
                long position = 5;
                for (int t = 0; t < numTables; t++)
                {
                    InitEncodingInfo(ref EncInfos[t], file, position, 0, t);
                    if (split)
                        InitEncodingInfo(ref EncInfos[t + numTables], file, position, 4, t);
                    Console.WriteLine($"data += {NumPieces + ((hasPawns && NumOtherPawns > 0) ? 2 : 1)}");
                    position += NumPieces + (NumOtherPawns > 0 ? 2 : 1);
                }

                //align the position within the tablebase file to a multiple of 2 bytes.
                position = Align(position, 2);
                for (int t = 0; t < numTables; t++)
                {
                    position = EncInfos[t].SetupPairs(file, position);
                    if (!Symmetric)
                        position = EncInfos[numTables + t].SetupPairs(file, position);
                }

                for (int t = 0; t < numTables; t++)
                {
                    Console.WriteLine($"IndexTable[{t}] = {file.ReadByte(position)}");
                    EncInfos[t].IndexTable = position;
                    position += EncInfos[t].Sizes.Indices;
                    if (!Symmetric)
                    {
                        Console.WriteLine($"IndexTable[{numTables + t}] = {file.ReadByte(position)}");
                        EncInfos[numTables + t].IndexTable = position;
                        position += EncInfos[numTables + t].Sizes.Indices;
                    }
                }

                for (int t = 0; t < numTables; t++)
                {
                    Console.WriteLine($"SizeTable[{t}] = {file.ReadByte(position)}");
                    EncInfos[t].SizeTable = position;
                    position += EncInfos[t].Sizes.Blocks;
                    if (!Symmetric)
                    {
                        Console.WriteLine($"SizeTable[{numTables + t}] = {file.ReadByte(position)}");
                        EncInfos[numTables + t].SizeTable = position;
                        position += EncInfos[numTables + t].Sizes.Blocks;
                    }
                }

                for (int t = 0; t < numTables; t++)
                {
                    position = Align(position, 64);
                    Console.WriteLine($"SizeTable[{t}] = {file.ReadByte(position)}");
                    EncInfos[t].Data = position;
                    position += EncInfos[t].Sizes.Data;
                    if (!Symmetric)
                    {
                        position = Align(position, 64);
                        Console.WriteLine($"SizeTable[{numTables + t}] = {file.ReadByte(position)}");
                        EncInfos[numTables + t].Data = position;
                        position += EncInfos[numTables + t].Sizes.Data;
                    }
                }

                return true;
            }

            private long InitEncodingInfo(ref EncInfo ei, MemoryMappedViewAccessor file, long position, int shift, int table)
            {
                byte data;
                ei.Pieces = new byte[NumPieces];
                ei.Norm = new byte[NumPieces];
                ei.Factor = new ulong[NumPieces];

                file.Read(position++, out data);
                int order = (data >> shift) & 0x0F;
                int k = ei.Norm[0] = NumPawns;

                int order2 = 0x0F;
                if(NumOtherPawns > 0)
                {
                    file.Read(position++, out data);
                    order2 = (data >> shift) & 0x0F;
                    ei.Norm[k] = NumOtherPawns;
                    k += NumOtherPawns;
                }

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

                for (int i = 0; k < NumPieces || i == order || i == order2; i++)
                {
                    if (i == order)
                    {
                        ei.Factor[0] = f;
                        //FILE_ENC
                        f *= Encoding.PawnFactorFile[ei.Norm[0] - 1, table];
                    }
                    else if (i == order2)
                    {
                        ei.Factor[ei.Norm[0]] = f;
                        f *= Encoding.Subfactor(ei.Norm[ei.Norm[0]], (ulong)(48 - ei.Norm[0]));
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

                Console.WriteLine($"<-- init_enc_info({(NumOtherPawns>0 ? "MorePawns" : "Pawns")}) = {f}");
                //position += be->num + 1 + (be->hasPawns && be->pawns[1]);
                return position;
            }
        }

        EndgameFiles _files = new EndgameFiles();

        Dictionary<ulong, BaseEntry> _baseEntries = new Dictionary<ulong, BaseEntry>();

        public Syzygy(string path)
        {
            _files.AddEndgames(path);
            InitTablebases();
        }

        private void InitTablebases()
        {
            char pc(int i)
            {
                //#define pchr(i) piece_to_char[QUEEN - (i)]
                Piece piece = Piece.White | (Piece)((5-i) << 2);
                return Notation.GetChar(piece);
            }

            for (int i = 0; i < 5; i++)
                InitTablebase($"K{pc(i)}vK");

            for (int i = 0; i < 5; i++)
                for (int j = i; j < 5; j++)
                    InitTablebase($"K{pc(i)}vK{pc(j)}");

            for (int i = 0; i < 5; i++)
                for (int j = i; j < 5; j++)
                    InitTablebase($"K{pc(i)}{pc(j)}vK");

            for (int i = 0; i < 5; i++)
                for (int j = i; j < 5; j++)
                    for (int k = 0; k < 5; k++)
                        InitTablebase($"K{pc(i)}{pc(j)}vK{pc(k)}");

            for (int i = 0; i < 5; i++)
                for (int j = i; j < 5; j++)
                    for (int k = j; k < 5; k++)
                        InitTablebase($"K{pc(i)}{pc(j)}{pc(k)}vK");

            //6- and 7-piece Tablebases are not supported atm
        }

        private void InitTablebase(string egClass)
        {
            byte[] pieces = new byte[12];
            byte totalCount = CountPieces(egClass, pieces);

            ulong key = GetMaterialSignature(pieces);
            ulong key2 = GetMaterialSignatureFlipped(pieces);

            bool hasPawns = pieces[BLACK_PAWN] > 0 || pieces[WHITE_PAWN] > 0;

            BaseEntry tb = hasPawns ? CreatePawnTable(pieces) : CreatePieceTable(pieces);
            tb.Key = key;
            tb.NumPieces = totalCount;
            tb.Symmetric = key == key2;

            _baseEntries[key] = tb;
            _baseEntries[key2] = tb;

            Console.WriteLine($"{egClass} {key} {key2} {(hasPawns ? 1 : 0)}");
        }

        private PieceEntry CreatePieceTable(byte[] pieces)
        {
            var tbPiece = new PieceEntry();
            tbPiece.KKEncoding = CountSingles(pieces) == 2;
            return tbPiece;
        }

        private static PawnEntry CreatePawnTable(byte[] pieces)
        {
            byte bPawns = pieces[BLACK_PAWN];
            byte wPawns = pieces[WHITE_PAWN];

            var tbPawn = new PawnEntry();
            if (bPawns > 0 && (wPawns == 0 || wPawns > bPawns))
                (tbPawn.NumPawns, tbPawn.NumOtherPawns) = (bPawns, wPawns);
            else
                (tbPawn.NumPawns, tbPawn.NumOtherPawns) = (wPawns, bPawns);

            return tbPawn;
        }

        private int CountSingles(byte[] pcs)
        {
            int j = 0;
            for (int i = 0; i < 12; i++)
                if (pcs[i] == 1) 
                    j++;
            
            return j;
        }

        //Pawn = 0, Knight = 1, Bishop = 2; Rook = 3, Queen = 4, King = 5
        private int Index(Piece piece) => ((int)piece >> 2) - 1;

        private byte CountPieces(string egClass, byte[] numPieces)
        {
            int colorOffset = 0;
            byte totalCount = 0;
            for(int i = 0; i < egClass.Length; i++)
            {
                if (egClass[i] == 'v')
                    colorOffset = 6;
                else
                {
                    Piece piece = Notation.GetPiece(egClass[i]);
                    numPieces[Index(piece) + colorOffset]++;
                    totalCount++;
                }
            }
            return totalCount;
        }

        //int probe_table(const Pos *pos, int s, int *success, const int type)
        public bool ProbeWinDrawLoss(BoardState pos, out WinDrawLoss result)
        {
            ulong key = GetMaterialSignature(pos);
            Console.WriteLine($"GetMaterialSignature(pos) = {key}");

            if (key == 0)
            {
                //KvK (two lone kings)
                result = WinDrawLoss.Draw;
                return true;
            }

            if (!_baseEntries.TryGetValue(key, out BaseEntry be))
            {
                //hash does not map to an entry
                result = WinDrawLoss.Draw;
                return false;
            }

            string endgameClass = GetEndgameClass(pos, be.Key != key);
            var file = _files.GeWDL(endgameClass);

            Console.WriteLine($"map_tb({endgameClass}.rtbw)");
            if (!InitWinDrawLossTable(file, be, endgameClass))
            {
                _baseEntries.Remove(key);
            }


            bool flip;
            bool bside;
            if (!be.Symmetric)
            {
                flip = key != be.Key;
                bside = (pos.SideToMove == Color.White) == flip;
            }
            else
            {
                flip = pos.SideToMove != Color.White;
                bside = false;
            }

            Console.WriteLine($"4. bside = {bside}, flip = {flip}");
            result = be.GetWDL(pos, bside, flip);

            //uint8_t* w = decompress_pairs(ei->precomp, idx);

            //int result = w[0] - 2;
            //printf("X. int result = w[0] - 2 = %i\n", result);
            //return (int)w[0] - 2;

            return true;
        }

        const uint WDL_MAGIC = 0x5d23e871;

        //static void prt_str(const Pos* pos, char* str, bool flip)
        private bool InitWinDrawLossTable(MemoryMappedViewAccessor file, BaseEntry be, string endgameClass)
        {
            if(file == null)
                return false;

            //Bytes 0 - 3: magic number
            file.Read(0, out uint magic);
            if (magic != WDL_MAGIC)
                throw new Exception($"'{endgameClass}.rtbw' is corrupted!");

            be.InitTable(file);
            return true;
        }

        public static string GetEndgameClass(BoardState board, bool flip)
        {
            StringBuilder sb = new StringBuilder();

            void Add(char c, ulong bb)
            {
                int cnt = PopCount(bb);
                while (cnt-- > 0) sb.Append(c);
            }

            void AddAll(ulong color)
            {
                Add('K', color & board.Kings);
                Add('Q', color & board.Queens);
                Add('R', color & board.Rooks);
                Add('B', color & board.Bishops);
                Add('N', color & board.Knights);
                Add('P', color & board.Pawns);
            }

            AddAll(flip ? board.Black : board.White);
            sb.Append('v');
            AddAll(flip ? board.White : board.Black);
            return sb.ToString();
        }

        const ulong PRIME_WHITE_QUEEN  = 11811845319353239651;
        const ulong PRIME_WHITE_ROOK   = 10979190538029446137;
        const ulong PRIME_WHITE_BISHOP = 12311744257139811149;
        const ulong PRIME_WHITE_KNIGHT = 15202887380319082783;
        const ulong PRIME_WHITE_PAWN   = 17008651141875982339;
        const ulong PRIME_BLACK_QUEEN  = 15484752644942473553;
        const ulong PRIME_BLACK_ROOK   = 18264461213049635989;
        const ulong PRIME_BLACK_BISHOP = 15394650811035483107;
        const ulong PRIME_BLACK_KNIGHT = 13469005675588064321;
        const ulong PRIME_BLACK_PAWN   = 11695583624105689831;

        /*
         * Computes a 64-bit material signature key based on a Position
         */
        static ulong GetMaterialSignature(BoardState pos)
        {
            ulong white = pos.White;
            ulong black = pos.Black;
            //if(mirror)
            //    (white, black) = (black, white);
            return (ulong)PopCount(white & pos.Queens) * PRIME_WHITE_QUEEN +
                   (ulong)PopCount(white & pos.Rooks) * PRIME_WHITE_ROOK +
                   (ulong)PopCount(white & pos.Bishops) * PRIME_WHITE_BISHOP +
                   (ulong)PopCount(white & pos.Knights) * PRIME_WHITE_KNIGHT +
                   (ulong)PopCount(white & pos.Pawns) * PRIME_WHITE_PAWN +
                   (ulong)PopCount(black & pos.Queens) * PRIME_BLACK_QUEEN +
                   (ulong)PopCount(black & pos.Rooks) * PRIME_BLACK_ROOK +
                   (ulong)PopCount(black & pos.Bishops) * PRIME_BLACK_BISHOP +
                   (ulong)PopCount(black & pos.Knights) * PRIME_BLACK_KNIGHT +
                   (ulong)PopCount(black & pos.Pawns) * PRIME_BLACK_PAWN;
        }

        const byte WHITE_PAWN = 0;
        const byte WHITE_KNIGHT = 1;
        const byte WHITE_BISHOP = 2;
        const byte WHITE_ROOK = 3;
        const byte WHITE_QUEEN = 4;
        const byte WHITE_KING = 5;

        const byte BLACK_PAWN = 6;
        const byte BLACK_KNIGHT = 7;
        const byte BLACK_BISHOP = 8;
        const byte BLACK_ROOK = 9;
        const byte BLACK_QUEEN = 10;
        const byte BLACK_KING = 11;

        /*
         * Computes a 64-bit material signature key based on an array of pieceCounts
         *   pieceCounts[1..6] corresponds to the number of {WhitePawn..WhiteKing}
         *   pieceCounts[9..14] corresponds to the number of {WhitePawn..WhiteKing}
         */
        static ulong GetMaterialSignature(byte[] pieceCounts)
        {
            return pieceCounts[WHITE_QUEEN] * PRIME_WHITE_QUEEN +
                   pieceCounts[WHITE_ROOK] * PRIME_WHITE_ROOK +
                   pieceCounts[WHITE_BISHOP] * PRIME_WHITE_BISHOP +
                   pieceCounts[WHITE_KNIGHT] * PRIME_WHITE_KNIGHT +
                   pieceCounts[WHITE_PAWN] * PRIME_WHITE_PAWN +
                   pieceCounts[BLACK_QUEEN]* PRIME_BLACK_QUEEN +
                   pieceCounts[BLACK_ROOK] * PRIME_BLACK_ROOK +
                   pieceCounts[BLACK_BISHOP] * PRIME_BLACK_BISHOP +
                   pieceCounts[BLACK_KNIGHT] * PRIME_BLACK_KNIGHT +
                   pieceCounts[BLACK_PAWN] * PRIME_BLACK_PAWN;
        }

        static ulong GetMaterialSignatureFlipped(byte[] pieceCounts)
        {
            return pieceCounts[BLACK_QUEEN] * PRIME_WHITE_QUEEN +
                   pieceCounts[BLACK_ROOK] * PRIME_WHITE_ROOK +
                   pieceCounts[BLACK_BISHOP] * PRIME_WHITE_BISHOP +
                   pieceCounts[BLACK_KNIGHT] * PRIME_WHITE_KNIGHT +
                   pieceCounts[BLACK_PAWN] * PRIME_WHITE_PAWN +
                   pieceCounts[WHITE_QUEEN] * PRIME_BLACK_QUEEN +
                   pieceCounts[WHITE_ROOK] * PRIME_BLACK_ROOK +
                   pieceCounts[WHITE_BISHOP] * PRIME_BLACK_BISHOP +
                   pieceCounts[WHITE_KNIGHT] * PRIME_BLACK_KNIGHT +
                   pieceCounts[WHITE_PAWN] * PRIME_BLACK_PAWN;
        }
    }
}
