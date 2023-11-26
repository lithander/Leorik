using Leorik.Core;
using System.Runtime.InteropServices;

namespace Leorik.Tuning
{
    class PackedBoard
    {
        //Marlinflow Format: https://github.com/jnlt3/marlinflow/blob/main/marlinformat/src/lib.rs

        [StructLayout(LayoutKind.Explicit)]
        public struct DataStruct
        {
            [FieldOffset(0)]
            public byte StmEpSquare;
            [FieldOffset(1)]
            public byte HalfmoveClock;
            [FieldOffset(2)]
            public short FullmoveNumber;
            [FieldOffset(4)]
            public short Eval;
            [FieldOffset(6)]
            public byte Wdl; //2 = White, 1 = Draw, 0 = Black
            [FieldOffset(7)]
            public byte Extra;
            [FieldOffset(0)]
            public ulong Encoded;

            public static bool operator ==(DataStruct s1, DataStruct s2) => s1.Encoded == s2.Encoded;
            public static bool operator !=(DataStruct s1, DataStruct s2) => s1.Encoded != s2.Encoded;
        }

        public DataStruct Data;
        public ulong Occupancy;
        public readonly byte[] Pieces = new byte[16];

        //32 4bit values indicating piece types stored in byte[16]
        //public int GetPiece(int i) => (Pieces[i / 2] >> (i % 2) * 4) & 0xF;
        public int GetPiece(int i) => (Pieces[i >> 1] >> (i & 1) * 4) & 0xF;

        public PackedBoard()
        {
        }

        public PackedBoard(PackedBoard other)
        {
            Occupancy = other.Occupancy;
            Data = other.Data;
            other.Pieces.CopyTo(Pieces, 0);
        }

        public bool Equals(PackedBoard other)
        {
            if (Occupancy != other.Occupancy || Data.Encoded != other.Data.Encoded)
                return false;

            for(int i = 0; i < 16; i++)
                if (Pieces[i] != other.Pieces[i]) 
                    return false;

            return true;
        }

        public void Read(BinaryReader reader)
        {
            Occupancy = reader.ReadUInt64();
            reader.Read(Pieces);
            Data.Encoded = reader.ReadUInt64();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Occupancy);
            writer.Write(Pieces);
            writer.Write(Data.Encoded);
        }

        public BoardState Unpack(out short fullMoveNumber, out short eval, out byte wdl, out byte extra)
        {
            BoardState board = new BoardState();
            //Setup BoardState
            int i = 0;
            for (ulong bits = Occupancy; bits != 0; bits = Bitboard.ClearLSB(bits))
            {
                int square = Bitboard.LSB(bits);
                int data = GetPiece(i++);
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
            board.SideToMove = Data.StmEpSquare >= 128 ? Color.Black : Color.White;
            int epSquare = Data.StmEpSquare & 63;
            if (epSquare != 0)
                board.EnPassant = 1UL << epSquare;
            board.HalfmoveClock = Data.HalfmoveClock;

            //EVAL, WDL etc
            fullMoveNumber = Data.FullmoveNumber;
            eval = Data.Eval;
            wdl = Data.Wdl;
            extra = Data.Extra;

            return board;
        }

        internal void Pack(BoardState board, short fullMoveNumber, short eval, byte wdl, byte extra)
        {
            //DATA
            Data.Extra = extra;
            Data.Wdl = wdl;
            Data.Eval = eval;
            Data.FullmoveNumber = fullMoveNumber;
            Data.HalfmoveClock = board.HalfmoveClock;
            Data.StmEpSquare = (byte)Bitboard.LSB(board.EnPassant);
            if (board.SideToMove == Color.Black)
                Data.StmEpSquare |= 128;
            //OCCUPANCY
            Occupancy = board.Black | board.White;
            //PIECES
            Array.Clear(Pieces);
            int nextPiece = 0;
            for (ulong bits = Occupancy; bits != 0; bits = Bitboard.ClearLSB(bits), nextPiece++)
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
                Pieces[nextPiece>>1] |= (byte)(pieceBits << offset);
            }
        }
    }
}
