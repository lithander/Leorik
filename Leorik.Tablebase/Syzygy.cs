/*
Leorik's Syzygy probing was implemented after studying Fathom: https://github.com/jdart1/Fathom

Without the invaluable work of Ronald de Man, Basil Falcinelli and Jon Dart 
Leorik's tablebase probing would look vastly different or not exist at all!
 */


//TODO: can we use long instead of ulong? Less casting, readable code


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
    public enum WinDrawLoss
    {
        Loss = -2,
        LateLoss = -1,
        Draw = 0,
        LateWin = 1,
        Win = 2
    } 

    public class Syzygy
    {
        //const int TB_PIECES = 7
        //const int TB_HASHBITS = 12
        //const int TB_MAX_PIECE = 650
        //const int TB_MAX_PAWN = 861

        //const int TB_PIECES = 5;
        //const int TB_HASHBITS = 11;
        //const int TB_MAX_PIECE = 254;
        //const int TB_MAX_PAWN = 256;              

        EndgameFiles _files = new EndgameFiles();

        Dictionary<ulong, BaseEntry> _baseEntries = new Dictionary<ulong, BaseEntry>();

        public Syzygy(string path)
        {
            _files.AddEndgames(path);
            InitTablebases();
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

        private void InitTablebases()
        {
            static char pc(int i)
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

        //static void prt_str(const Pos* pos, char* str, bool flip)
        private bool InitWinDrawLossTable(MemoryMappedViewAccessor file, BaseEntry be, string endgameClass)
        {
            if (file == null)
                return false;

            const uint WDL_MAGIC = 0x5d23e871;
            //Bytes 0 - 3: magic number
            file.Read(0, out uint magic);
            if (magic != WDL_MAGIC)
                throw new Exception($"'{endgameClass}.rtbw' is corrupted!");

            be.InitTable(file);
            return true;
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
