using Leorik.Core;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Leorik.Tuning
{
    [StructLayout(LayoutKind.Explicit)]
    struct PackedBoard
    {
        //Marlinflow Format: https://github.com/jnlt3/marlinflow/blob/main/marlinformat/src/lib.rs
        [FieldOffset(0)]
        public ulong Occupancy;
        [FieldOffset(8)]
        public byte[] Pieces;
        [FieldOffset(16)]
        public byte StmEpSquare;
        [FieldOffset(17)]
        public byte HalfmoveClock;
        [FieldOffset(18)]
        public short FullmoveNumber;
        [FieldOffset(20)]
        public short Eval;
        [FieldOffset(22)]
        public byte Wdl;
        [FieldOffset(23)]
        public byte Extra;
        //32 4bit values indicating piece types stored in byte[16]
        //public int GetPiece(int i) => (Pieces[i / 2] >> (i % 2) * 4) & 0xF;
        public int GetPiece(int i) => (Pieces[i >> 1] >> (i & 1) * 4) & 0xF;

        [FieldOffset(16)]
        public ulong Data;

        public PackedBoard()
        {
            Pieces = new byte[16];
        }

        public static void Read(BinaryReader reader, ref PackedBoard packed)
        {
            packed.Occupancy = reader.ReadUInt64();
            reader.Read(packed.Pieces);
            packed.Data = reader.ReadUInt64();
        }

        public static void Unpack(ref PackedBoard packed, BoardState board, out int eval, out int wdl)
        {
            //Setup BoardState
            int i = 0;
            for (ulong bits = packed.Occupancy; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                int data = packed.GetPiece(i++);
                //4th bit set == Black, else White
                Piece color = (Piece)(((data & 8) >> 2) ^ 3);
                //0 = Pawn, 1 = Knight .. 5 = King, 6 = Unmoved Rook
                Piece type = (Piece)(((data & 7) + 1) << 2);
                if (type > Piece.King)
                {
                    board.CastleFlags |= 1UL << square;
                    board.SetBit(square, Piece.Rook | color);
                }
                else
                {
                    board.SetBit(square, type | color);
                }
            }
            //STM
            board.SideToMove = packed.StmEpSquare >= 128 ? Color.Black : Color.White;
            int epSquare = packed.StmEpSquare & 63;
            if (epSquare != 0)
                board.EnPassant = 1UL << epSquare;
            board.HalfmoveClock = packed.HalfmoveClock;
            //WDL & EVAL
            eval = packed.Eval;
            wdl = packed.Wdl;
        }
    }
}
