using Leorik.Core;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Reflection.PortableExecutable;
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

        public PackedBoard(PackedBoard other)
        {
            Occupancy = other.Occupancy;
            Data = other.Data;
            Pieces = (byte[])other.Pieces.Clone();
        }

        public bool Equals(PackedBoard other)
        {
            if (Occupancy != other.Occupancy || Data != other.Data)
                return false;

            for(int i = 0; i < 16; i++)
                if (Pieces[i] != other.Pieces[i]) 
                    return false;

            return true;
        }

        public static void Read(BinaryReader reader, ref PackedBoard packed)
        {
            packed.Occupancy = reader.ReadUInt64();
            reader.Read(packed.Pieces);
            packed.Data = reader.ReadUInt64();
        }

        public static void Unpack(ref PackedBoard packed, BoardState board, out short fullMoveNumber, out short eval, out byte wdl, out byte extra)
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
            board.SideToMove = packed.StmEpSquare >= 128 ? Color.Black : Color.White;
            int epSquare = packed.StmEpSquare & 63;
            if (epSquare != 0)
                board.EnPassant = 1UL << epSquare;
            board.HalfmoveClock = packed.HalfmoveClock;

            //EVAL, WDL etc
            fullMoveNumber = packed.FullmoveNumber;
            eval = packed.Eval;
            wdl = packed.Wdl;
            extra = packed.Extra;
        }

        internal static void Pack(ref PackedBoard packed, BoardState board, short fullMoveNumber, short eval, byte wdl, byte extra)
        {
            //DATA
            packed.Extra = 0;
            packed.Wdl = wdl;
            packed.Eval = eval;
            packed.FullmoveNumber = fullMoveNumber;
            packed.HalfmoveClock = board.HalfmoveClock;
            packed.StmEpSquare = (byte)Bitboard.LSB(board.EnPassant);
            if (board.SideToMove == Color.Black)
                packed.StmEpSquare |= 128;
            //OCCUPANCY
            packed.Occupancy = board.Black | board.White;
            //PIECES
            Array.Clear(packed.Pieces);
            int nextPiece = 0;
            for (ulong bits = packed.Occupancy; bits != 0; bits = Bitboard.ClearLSB(bits), nextPiece++)
            {
                int square = Bitboard.LSB(bits);
                byte piece = (byte)board.GetPiece(square);
                //4th bit set == Black, else White
                int pieceBits = (~piece & 2) << 2;
                //0 = Pawn, 1 = Knight .. 5 = King, 6 = Unmoved Rook
                if ((board.CastleFlags & (1UL << square)) > 0)
                    pieceBits |= 6;
                else
                    pieceBits |= (piece >> 2) - 1;
                //put two pieces into one byte
                int offset = 4 * (nextPiece & 1);
                packed.Pieces[nextPiece>>1] |= (byte)(pieceBits << offset);
            }
        }
    }
}
