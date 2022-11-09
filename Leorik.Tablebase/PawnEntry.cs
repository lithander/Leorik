using Leorik.Core;
using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace Leorik.Tablebase
{
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
            if (NumOtherPawns > 0)
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

            Console.WriteLine($"<-- init_enc_info({(NumOtherPawns > 0 ? "MorePawns" : "Pawns")}) = {f}");
            //position += be->num + 1 + (be->hasPawns && be->pawns[1]);
            return position;
        }
    }
}
