using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Tablebase
{
    struct DataSize
    {
        public long TableBase;
        public long Indices;
        public long Blocks;
        public long Data;
    }

    struct EncInfo
    {
        const int TB_MAX_SYMS = 4096;

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
            for (int i = h - 2; i >= 0; i--)
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
}
