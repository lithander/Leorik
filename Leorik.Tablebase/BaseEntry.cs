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
            switch (pieceIndex & 7)
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
}
