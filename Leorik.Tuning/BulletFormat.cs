using Leorik.Core;
using System.Runtime.InteropServices;

namespace Leorik.Tuning
{
    class BulletFormat
    {
        //Bullet Format: https://github.com/jw1912/bulletformat/blob/main/src/chess.rs#L82

        [StructLayout(LayoutKind.Explicit)]
        public struct DataStruct
        {
            [FieldOffset(0)]
            public short Score; //STM relative, in Centipawns.
            [FieldOffset(2)]
            public byte Result; //Result is 0 for STM Loss, 1 for Draw, 2 for STM Win
            [FieldOffset(3)]
            public byte KingSquare; //STM Pov
            [FieldOffset(4)]
            public byte OppKingSquare; //Opp Pov
            [FieldOffset(0)]
            public ulong Encoded;
        }

        public DataStruct Data;
        public ulong Occupancy;
        public readonly byte[] Pieces = new byte[16];

        //32 4bit values indicating piece types stored in byte[16]
        //public int GetPiece(int i) => (Pieces[i / 2] >> (i % 2) * 4) & 0xF;
        public int GetPiece(int i) => (Pieces[i >> 1] >> (i & 1) * 4) & 0xF;

        public BulletFormat()
        {
        }

        public BulletFormat(BulletFormat other)
        {
            Occupancy = other.Occupancy;
            other.Pieces.CopyTo(Pieces, 0);
            Data = other.Data;
        }

        public void Read(BinaryReader reader)
        {
            Occupancy = reader.ReadUInt64();
            reader.Read(Pieces);
            Data.Encoded = reader.ReadUInt64();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Occupancy); //8 Byte
            writer.Write(Pieces); //16 Byte
            writer.Write(Data.Encoded); //8 Byte
        }

        internal void PackDestructive(BoardState board, short score, byte wdl)
        {
            short stm = (short)board.SideToMove;
            if(board.SideToMove == Color.Black)
                board.Flip();
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
                //0 = Pawn, 1 = Knight .. 5 = King
                pieceBits |= (piece >> 2) - 1;
                //put two pieces into one byte
                int offset = 4 * (nextPiece & 1);
                Pieces[nextPiece>>1] |= (byte)(pieceBits << offset);
            }
            //DATA
            Data.Score = (short)(stm * score); //STM relative in Centipawns.
            Data.Result = (byte)(stm * (wdl - 1) + 1); //Result is 0 for Opp Win, 1 for Draw, 2 for STM Win
            //stores the king squares and each from its "own" pov
            Data.KingSquare = (byte)(Bitboard.LSB(board.White & board.Kings));
            Data.OppKingSquare = (byte)(Bitboard.LSB(board.Black & board.Kings) ^ 56);
        }

        public BoardState Unpack(out short score, out byte wdl, out int whiteKingSquare, out int blackKingSquareBlackPov)
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
            score = Data.Score;
            wdl = Data.Result;
            whiteKingSquare = Data.KingSquare;
            blackKingSquareBlackPov = Data.OppKingSquare;
            return board;
        }
    }
}
