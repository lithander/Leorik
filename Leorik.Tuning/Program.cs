using Leorik.Core;
using Leorik.Tuning;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

string DATA_PATH = "D:/Projekte/Chess/Leorik/TD2/";
string EPD_FILE = "DATA-v27-WhiteKing.epd";
string[] PGN_FILES = {
    //"leorik2X3_selfplay_startpos_5s_200ms_50mb_12112020.pgn",
    //"leorik2X3_selfplay_startpos_5s_200ms_50mb_16112020.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay_2.pgn",
    //"leorik228a_startpos_RND25_100Hash_5s_200ms_selfplay_3.pgn",
    //"leorik228alpha_selfplay_startpos_RND25_100Hash_5s_200ms.pgn",
    //"leorik228alpha_selfplay_startpos_RND25_100Hash_5s_200ms_2.pgn",
    //"leorik228beta_vs_leorik228alpha_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228beta_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_vs_leorik228beta_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228gamma_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_vs_leorik228gamma_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_selfplay_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228delta_selfplay_varied_RND30_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_vs_leorik228delta_startpos_RND30_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_vs_leorik228delta_startpos_RND35_100Hash_5s_200ms.pgn",
    //"leorik228epsilon_selfplay_startpos_RND50-10_100Hash_5s_200ms.pgn",

    "leorik228epsilon_selfplay_startpos_RND50-10_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228epsilon_selfplay_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_startpos_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_vs_leorik228epsilon2_varied_RND40-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228zeta_selfplay_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228eta_vs_zeta_startpos_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228eta_vs_zeta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228theta-1234672_vs_eta_varied_RND50-0_100Hash_5s_200ms.pgn",
    "leorik228theta-1234672_vs_eta_startpos_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND50-0_100Hash_5s_200ms_2.pgn",
    "leorik228theta-1234672_selfplay_RND100-0_100Hash_5s_200ms.pgn",
    
    "leorik24net8pext_selfplay_human_0_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_1_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_2_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_3_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_4_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_5_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_6_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_7_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_8_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_human_9_RND100--250-5s_100ms.pgn",
    
    "leorik24net8pext_selfplay_varied_1_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_2_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_3_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_4_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_5_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_6_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_7_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_8_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_9_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_10_RND120--300-5s_100ms.pgn",
    "leorik24net8pext_selfplay_varied_11_RND120--300-5s_100ms.pgn",
    
    "leorik24net8pext_selfplay_0_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_1_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_2_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_3_titans_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_4_titans_RND100--250-5s_100ms.pgn",
    
    "leorik24_selfplay_varied_1_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_2_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_3_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_4_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_5_RND100-0_5s_200ms.pgn",
    "leorik24_selfplay_varied_6_RND100-0_5s_200ms.pgn",
    
    "leorik24net8pext_selfplay_startpos_0_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_1_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_2_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_3_RND100--250-5s_100ms.pgn",
    "leorik24net8pext_selfplay_startpos_4_RND100--250-5s_100ms.pgn",
};

Filter[] WK_FILTERS =
{
    new(){ WhiteKingMask = 0x0000000000000001UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000002UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000004UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000008UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000010UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e1 BK: *any*" },
    //new(){ WhiteKingMask = 0x0000000000000010UL, BlackKingMask = 0xAFFFFFFFFFFFFFFFUL, Comment = "WK: e1 BK: ~(e8 & g8)" },
    //new(){ WhiteKingMask = 0x0000000000000010UL, BlackKingMask = 0x1000000000000000UL, Comment = "WK: e1 (starting square) BK: e8 (starting square)" },
    //new(){ WhiteKingMask = 0x0000000000000010UL, BlackKingMask = 0x4000000000000000UL, Comment = "WK: e1 (starting square) BK: g8 (castle short)" },
    new(){ WhiteKingMask = 0x0000000000000020UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000040UL, BlackKingMask = 0xAFFFFFFFFFFFFFFFUL, Comment = "WK: g1 BK: *any*" },
    //new(){ WhiteKingMask = 0x0000000000000040UL, BlackKingMask = 0xAFFFFFFFFFFFFFFFUL, Comment = "WK: g1 BK: ~(e8 & g8)" },
    //new(){ WhiteKingMask = 0x0000000000000040UL, BlackKingMask = 0x4000000000000000UL, Comment = "WK: g1 (castle short) BK: g8 (castle short)" },
    //new(){ WhiteKingMask = 0x0000000000000040UL, BlackKingMask = 0x1000000000000000UL, Comment = "WK: g1 (castle short) BK: e8 (starting square)" },
    new(){ WhiteKingMask = 0x0000000000000080UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h1 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000100UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000200UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000400UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000000800UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000001000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000002000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000004000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000008000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h2 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000010000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000020000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000040000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000080000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000100000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000200000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000400000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000000800000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h3 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000001000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000002000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000004000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000008000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000010000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000020000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000040000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000080000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h4 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000100000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000200000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000400000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000000800000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000001000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000002000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000004000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000008000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h5 BK: *any*" },
    new(){ WhiteKingMask = 0x0000010000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000020000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000040000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000080000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000100000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000200000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000400000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g6 BK: *any*" },
    new(){ WhiteKingMask = 0x0000800000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h6 BK: *any*" },
    new(){ WhiteKingMask = 0x0001000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a7 BK: *any*" },
    new(){ WhiteKingMask = 0x0002000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b7 BK: *any*" },
    new(){ WhiteKingMask = 0x0004000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c7 BK: *any*" },
    new(){ WhiteKingMask = 0x0008000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d7 BK: *any*" },
    new(){ WhiteKingMask = 0x0010000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e7 BK: *any*" },
    new(){ WhiteKingMask = 0x0020000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f7 BK: *any*" },
    new(){ WhiteKingMask = 0x0040000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g7 BK: *any*" },
    new(){ WhiteKingMask = 0x0080000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h7 BK: *any*" },
    new(){ WhiteKingMask = 0x0100000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: a8 BK: *any*" },
    new(){ WhiteKingMask = 0x0200000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: b8 BK: *any*" },
    new(){ WhiteKingMask = 0x0400000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: c8 BK: *any*" },
    new(){ WhiteKingMask = 0x0800000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: d8 BK: *any*" },
    new(){ WhiteKingMask = 0x1000000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: e8 BK: *any*" },
    new(){ WhiteKingMask = 0x2000000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: f8 BK: *any*" },
    new(){ WhiteKingMask = 0x4000000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: g8 BK: *any*" },
    new(){ WhiteKingMask = 0x8000000000000000UL, BlackKingMask = 0xFFFFFFFFFFFFFFFFUL, Comment = "WK: h8 BK: *any*" },
};

Filter[] BK_FILTERS =
{
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000001UL, Comment = "WK: *any* BK: a1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000002UL, Comment = "WK: *any* BK: b1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000004UL, Comment = "WK: *any* BK: c1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000008UL, Comment = "WK: *any* BK: d1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000010UL, Comment = "WK: *any* BK: e1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000020UL, Comment = "WK: *any* BK: f1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000040UL, Comment = "WK: *any* BK: g1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000080UL, Comment = "WK: *any* BK: h1"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000100UL, Comment = "WK: *any* BK: a2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000200UL, Comment = "WK: *any* BK: b2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000400UL, Comment = "WK: *any* BK: c2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000000800UL, Comment = "WK: *any* BK: d2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000001000UL, Comment = "WK: *any* BK: e2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000002000UL, Comment = "WK: *any* BK: f2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000004000UL, Comment = "WK: *any* BK: g2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000008000UL, Comment = "WK: *any* BK: h2"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000010000UL, Comment = "WK: *any* BK: a3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000020000UL, Comment = "WK: *any* BK: b3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000040000UL, Comment = "WK: *any* BK: c3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000080000UL, Comment = "WK: *any* BK: d3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000100000UL, Comment = "WK: *any* BK: e3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000200000UL, Comment = "WK: *any* BK: f3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000400000UL, Comment = "WK: *any* BK: g3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000000800000UL, Comment = "WK: *any* BK: h3"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000001000000UL, Comment = "WK: *any* BK: a4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000002000000UL, Comment = "WK: *any* BK: b4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000004000000UL, Comment = "WK: *any* BK: c4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000008000000UL, Comment = "WK: *any* BK: d4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000010000000UL, Comment = "WK: *any* BK: e4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000020000000UL, Comment = "WK: *any* BK: f4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000040000000UL, Comment = "WK: *any* BK: g4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000080000000UL, Comment = "WK: *any* BK: h4"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000100000000UL, Comment = "WK: *any* BK: a5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000200000000UL, Comment = "WK: *any* BK: b5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000400000000UL, Comment = "WK: *any* BK: c5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000000800000000UL, Comment = "WK: *any* BK: d5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000001000000000UL, Comment = "WK: *any* BK: e5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000002000000000UL, Comment = "WK: *any* BK: f5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000004000000000UL, Comment = "WK: *any* BK: g5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000008000000000UL, Comment = "WK: *any* BK: h5"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000010000000000UL, Comment = "WK: *any* BK: a6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000020000000000UL, Comment = "WK: *any* BK: b6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000040000000000UL, Comment = "WK: *any* BK: c6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000080000000000UL, Comment = "WK: *any* BK: d6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000100000000000UL, Comment = "WK: *any* BK: e6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000200000000000UL, Comment = "WK: *any* BK: f6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000400000000000UL, Comment = "WK: *any* BK: g6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0000800000000000UL, Comment = "WK: *any* BK: h6"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0001000000000000UL, Comment = "WK: *any* BK: a7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0002000000000000UL, Comment = "WK: *any* BK: b7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0004000000000000UL, Comment = "WK: *any* BK: c7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0008000000000000UL, Comment = "WK: *any* BK: d7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0010000000000000UL, Comment = "WK: *any* BK: e7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0020000000000000UL, Comment = "WK: *any* BK: f7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0040000000000000UL, Comment = "WK: *any* BK: g7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0080000000000000UL, Comment = "WK: *any* BK: h7"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0100000000000000UL, Comment = "WK: *any* BK: a8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0200000000000000UL, Comment = "WK: *any* BK: b8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0400000000000000UL, Comment = "WK: *any* BK: c8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x0800000000000000UL, Comment = "WK: *any* BK: d8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x1000000000000000UL, Comment = "WK: *any* BK: e8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x2000000000000000UL, Comment = "WK: *any* BK: f8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x4000000000000000UL, Comment = "WK: *any* BK: g8"},
   new(){ WhiteKingMask = 0xFFFFFFFFFFFFFFFFUL, BlackKingMask = 0x8000000000000000UL, Comment = "WK: *any* BK: h8"}
};
/*
* 
* SHARED DATA
* 
*/

int SKIP_OPENING = 10;
int FEN_PER_GAME = 5;
int SKIP_OUTLIERS = 300;
int MAX_Q_DEPTH = 10;

float MSE_SCALING = 100;
int ITERATIONS = 120;

int MATERIAL_ALPHA = 25;
int MATERIAL_BATCHES = 2500;
int MATERIAL_BATCH_SIZE = 5000;

int PHASE_ALPHA = 10;
int PHASE_BATCHES = 500;
int PHASE_BATCH_SIZE = 5000;

int SUBSET_ITERATIONS = 20;

//https://www.desmos.com/calculator/k7qsivwcdc
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine(" Leorik Tuning v27 ");
Console.WriteLine("~~~~~~~~~~~~~~~~~~~");
Console.WriteLine();
Console.WriteLine($"SKIP_OPENING = {SKIP_OPENING}");
Console.WriteLine($"FEN_PER_GAME = {FEN_PER_GAME}");
Console.WriteLine($"SKIP_OUTLIERS = {SKIP_OUTLIERS}");
Console.WriteLine($"MAX_Q_DEPTH = {MAX_Q_DEPTH}");
Console.WriteLine();
Console.WriteLine($"MSE_SCALING = {MSE_SCALING}");
Console.WriteLine($"ITERATIONS = {ITERATIONS}");
Console.WriteLine();
Console.WriteLine($"MATERIAL_ALPHA = {MATERIAL_ALPHA}");
Console.WriteLine($"MATERIAL_BATCHES = {MATERIAL_BATCHES}");
Console.WriteLine($"MATERIAL_BATCH_SIZE = {MATERIAL_BATCH_SIZE}");
Console.WriteLine();
Console.WriteLine($"PHASE_ALPHA = {PHASE_ALPHA}");
Console.WriteLine($"PHASE_BATCHES = {PHASE_BATCHES}");
Console.WriteLine();

//BitboardUtils.GenerateFilters();
//BitboardUtils.Repl();
//PgnToUci("leorik228theta-1592568_gauntlet_30per40_7threads.pgn");

float[] cPhase;
float[] cFeatures;
TuningData[] tuningData;

//ExtractPositions();
//TuneTaperedWeights();
TuneLinearFunctions();
PlotTunedSubsets();

void PlotTunedSubsets()
{
    Console.WriteLine($"Loading WK data...");
    cFeatures = FeatureTuner.GetLeorikCoefficients();
    MultipleRegressionSolver solver = new MultipleRegressionSolver(DATA_PATH + $"DATA-v27-WhiteKing-FromZero-20Its.bin");
    Console.WriteLine($"Plotting WK");
    FeaturePlot.Plot8x8FeatureSets(solver, cFeatures, DATA_PATH + $"DATA-v27-FromZero-20Its.png", "White King");

    //Console.WriteLine($"Loading BK data...");
    //cFeatures = FeatureTuner.GetLeorikCoefficients();
    //solver = new MultipleRegressionSolver(DATA_PATH + $"DATA-v27-BlackKing-FromZero-20Its.bin");
    //Console.WriteLine($"Plotting BK");
    //FeaturePlot.Plot8x8FeatureSets(solver, cFeatures, DATA_PATH + $"DATA-v27-BlackKing-FromZero-20Its.png", "Black King");

    Console.WriteLine($"Done!");
}

void TuneLinearFunctions()
{
    List<Bucket> buckets = DataUtils.LoadDataBuckets(DATA_PATH + EPD_FILE);
    TuningData[] batch = new TuningData[MATERIAL_BATCH_SIZE];
    cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
    MultipleRegressionSolver solver = new MultipleRegressionSolver(cPhase);

    for (int iBucket = 0; iBucket < buckets.Count; iBucket++)
    {
        Console.WriteLine();
        Console.WriteLine($"Processing Subset {iBucket+1}/{buckets.Count}");
        var bucket = buckets[iBucket];
        Console.WriteLine(bucket.Comment);
        TestLeorikMSE(bucket.Data);
        
        cFeatures = FeatureTuner.GetUntrainedCoefficients();

        Console.WriteLine($"Preparing TuningData for {bucket.Data.Count} positions");
        long t0 = Stopwatch.GetTimestamp();
        tuningData = new TuningData[bucket.Data.Count];
        int tdIndex = 0;
        foreach (Data entry in bucket.Data)
        {
            tuningData[tdIndex++] = Tuner.GetTuningData(entry, cPhase, cFeatures);
        }
        Tuner.Shuffle(tuningData);
        Tuner.Localize(tuningData);
        GC.Collect();
        long t1 = Stopwatch.GetTimestamp();
        Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

        t0 = Stopwatch.GetTimestamp();
        double mse = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);

        Console.WriteLine();
        Console.WriteLine($"Tuning coefficients... ");
        double msePre = mse;
        for (int it = 0; it < SUBSET_ITERATIONS; it++)
        {
            int alpha = (int)(MATERIAL_ALPHA / (3 * msePre));
            Console.Write($"{it}/{SUBSET_ITERATIONS} Alpha={alpha} ");
            long t_0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < MATERIAL_BATCHES; i++)
            {
                Tuner.SampleRandomSlice(tuningData, batch);
                FeatureTuner.MinimizeParallel(batch, cFeatures, MSE_SCALING, alpha);
            }
            long t_1 = Stopwatch.GetTimestamp();
            double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
            Console.WriteLine($"Material MSE={msePost:N10} Delta={msePre - msePost:N8} Time={Seconds(t_1 - t_0):0.###}s");
            msePre = msePost;
        }
        RebalanceCoefficients(cFeatures);
        t1 = Stopwatch.GetTimestamp();
        double percentage = 100 * (mse - msePre) / mse;
        Console.WriteLine($"Tuning reduced MSE={mse:N9} - {mse - msePre:N9} = {msePre:N9} by {percentage:F1}% in {(t1 - t0) / (double)Stopwatch.Frequency:0.###}s");
        solver.AddSubset(bucket.Data, cFeatures);
    }
    solver.WriteToFile(DATA_PATH + $"DATA-v27-WhiteKing-FromZero-20Its.bin");
}

/*
* 
* FUNCTIONS 
* 
*/


void TuneTaperedWeights()
{
    List<Data> data = DataUtils.LoadData(DATA_PATH + EPD_FILE);
    //DataUtils.CollectMetrics(data);
    //MSE_SCALING = Tuner.Minimize((k) => Tuner.MeanSquareError(data, k), 1, 1000);
    TestLeorikMSE(data);

    //float[] cPhase = PhaseTuner.GetLeorikPhaseCoefficients();
    cPhase = PhaseTuner.GetUntrainedCoefficients();
    //float[] cFeatures = FeatureTuner.GetLeorikCoefficients();
    cFeatures = FeatureTuner.GetUntrainedCoefficients();
    //PrintCoefficients(cFeatures, cPhase);

    Console.WriteLine($"Preparing TuningData for {data.Count} positions");
    long t0 = Stopwatch.GetTimestamp();
    tuningData = new TuningData[data.Count];
    int tdIndex = 0;
    foreach (Data entry in data)
    {
        tuningData[tdIndex++] = Tuner.GetTuningData(entry, cPhase, cFeatures);
    }
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

    Console.WriteLine($"Shuffling data...");
    t0 = Stopwatch.GetTimestamp();
    Tuner.Shuffle(tuningData);
    Console.WriteLine($"...and aligning feature arrays in memory...");
    Tuner.Localize(tuningData);
    GC.Collect();
    t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

    Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
    Console.WriteLine();

    RebalanceCoefficients(cFeatures);
    PrintCoefficients(cFeatures, cPhase);
    TestPhaseMSE(cPhase);
    TestMaterialMSE(cFeatures);
    PhaseTuner.Report(cPhase);

    t0 = Stopwatch.GetTimestamp();
    for (int it = 0; it < ITERATIONS; it++)
    {
        Console.WriteLine($"{it}/{ITERATIONS} ");
        TuneMaterialMicroBatches();
        TunePhaseMicroBatches();
        Tuner.ValidateConsistency(tuningData, cPhase, cFeatures);
    }
    t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Tuning took {(t1 - t0) / (double)Stopwatch.Frequency:0.###} seconds!");

    RebalanceCoefficients(cFeatures);
    PrintCoefficients(cFeatures, cPhase);

    double mse = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.ReadKey();
}

void WriteResults(string fileName, float[] cFeatures, List<Data> data)
{
    string filePath;
    // Loop until a filename that doesn't exist is found
    int i = 0;
    do
        filePath = Path.Combine(DATA_PATH, $"{fileName}{i++}.txt");
    while (File.Exists(filePath));

    // Create the new file
    Console.WriteLine($"Writing results into {fileName}...");
    using (StreamWriter sw = File.CreateText(filePath))
    {
        sw.WriteLine(data.Count);
        foreach (var td in data)
        {
            sw.WriteLine(Notation.GetFen(td.Position));
        }
        sw.WriteLine("#Weights:");
        for(i = 0; i < cFeatures.Length; i += 2)
            sw.WriteLine($"{cFeatures[i]}, {cFeatures[i+1]}");
    }
    Console.WriteLine($"Done!");
}

double Seconds(long ticks) => ticks / (double)Stopwatch.Frequency;

void PgnToUci(string pgnFileName)
{
    Console.WriteLine($"Converting PGN to 'position startpos move ...' format..");
    var output = File.CreateText(DATA_PATH + pgnFileName + ".uci");
    var input = File.OpenText(DATA_PATH + pgnFileName);
    DataUtils.PgnToUci(input, output);
    input.Close();
}

void ExtractPositions()
{
    Console.WriteLine($"Extracting {FEN_PER_GAME} positions per game into memory.");
    Console.WriteLine($"All positions that disagree by >{SKIP_OUTLIERS}cp with the previous eval...");
    Console.WriteLine();
    
    DataCollector collector = new DataCollector(BK_FILTERS, FEN_PER_GAME, SKIP_OUTLIERS);
    foreach (string pgnFile in PGN_FILES)
    {
        var input = File.OpenText(DATA_PATH + pgnFile);
        Console.WriteLine($"Reading {pgnFile}");
        long t_0 = Stopwatch.GetTimestamp();
        (int games, int positions) = DataUtils.ExtractData(input, SKIP_OPENING, MAX_Q_DEPTH, collector);
        long t_1 = Stopwatch.GetTimestamp();
        double totalDuration = Seconds(t_1 - t_0);
        double durationPerGame = Seconds(1000000 * (t_1 - t_0) / (1 + games));
        Console.WriteLine($"Extracted {positions} positions from {games} games in {totalDuration:0.###}s. ({durationPerGame:0.#}µs/Game)");
        Console.WriteLine();
        input.Close();
    }
    collector.PrintMetrics();
    Console.WriteLine($"Writing {EPD_FILE}");
    var output = File.CreateText(DATA_PATH + EPD_FILE);
    collector.WriteToStream(output);
    output.Close();
}

void TuneMaterial()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        FeatureTuner.MinimizeParallel(tuningData, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
}

void TuneMaterialMicroBatches()
{
    double msePre = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.Write($"  Material MSE={msePre:N12} Alpha={MATERIAL_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    TuningData[] batch = new TuningData[MATERIAL_BATCH_SIZE];
    for (int i = 0; i < MATERIAL_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, batch);
        FeatureTuner.MinimizeParallel(batch, cFeatures, MSE_SCALING, MATERIAL_ALPHA);
    }
    Tuner.SyncFeaturesChanges(tuningData, cFeatures);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = FeatureTuner.MeanSquareError(tuningData, cFeatures, MSE_SCALING);
    Console.WriteLine($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s");
}

void TunePhase()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        PhaseTuner.MinimizeParallel(tuningData, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s ");
    PhaseTuner.Report(cPhase);
}

void TunePhaseMicroBatches()
{
    double msePre = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"     Phase MSE={msePre:N12} Alpha={PHASE_ALPHA,5} ");
    long t_0 = Stopwatch.GetTimestamp();
    TuningData[] batch = new TuningData[PHASE_BATCH_SIZE];
    for (int i = 0; i < PHASE_BATCHES; i++)
    {
        Tuner.SampleRandomSlice(tuningData, batch);
        PhaseTuner.MinimizeParallel(batch, cPhase, MSE_SCALING, PHASE_ALPHA);
    }
    Tuner.SyncPhaseChanges(tuningData, cPhase);
    long t_1 = Stopwatch.GetTimestamp();
    double msePost = PhaseTuner.MeanSquareError(tuningData, cPhase, MSE_SCALING);
    Console.Write($"Delta={msePre - msePost:N10} Time={Seconds(t_1 - t_0):0.###}s ");
    PhaseTuner.Report(cPhase);
}

void TestLeorikMSE(List<Data> data)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = Tuner.MeanSquareError(data, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"Leorik's MSE(data) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void TestMaterialMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = FeatureTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cFeatures) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void TestPhaseMSE(float[] coefficients)
{
    long t0 = Stopwatch.GetTimestamp();
    double mse = PhaseTuner.MeanSquareError(tuningData, coefficients, MSE_SCALING);
    long t1 = Stopwatch.GetTimestamp();
    Console.WriteLine($"MSE(cPhase) with MSE_SCALING = {MSE_SCALING} on the dataset: {mse}");
    Console.WriteLine($"Took {Seconds(t1 - t0):0.###} seconds!");
    Console.WriteLine();
}

void RebalanceCoefficients(float[] featureWeights)
{
    //Both the square-feature of a piece and the mobility-feature of a piece can encode material.
    //...but if mobility isn't updated in Qsearch for performance reasons it should all go into the square-features
    Console.WriteLine("Rebalancing...");
    Tuner.Rebalance(Piece.Knight, featureWeights);
    Tuner.Rebalance(Piece.Bishop, featureWeights);
    Tuner.Rebalance(Piece.Rook, featureWeights);
    Tuner.Rebalance(Piece.Queen, featureWeights);
    Tuner.Rebalance(Piece.King, featureWeights);
}

void PrintCoefficients(float[] featureWeights, float[] phaseWeights)
{
    int featureTables = FeatureTuner.MaterialTables + FeatureTuner.PawnStructureTables;
    int mobilityOffset = 128 * featureTables;

    Console.WriteLine("[White Features]");
    for (int i = 0; i < featureTables; i++)
    {
        Console.WriteLine($"//{FeatureTuner.TableNames[i]}");
        FeatureTuner.Report(i, 0, featureWeights);
    }
    Console.WriteLine();
    Console.WriteLine("[Black Features]");
    for (int i = 0; i < featureTables; i++)
    {
        Console.WriteLine($"//{FeatureTuner.TableNames[i]}");
        FeatureTuner.Report(i, FeatureTuner.AllWeights, featureWeights);
    }

    Console.WriteLine();
    Console.WriteLine("[White Mobility]");
    MobilityTuner.Report(mobilityOffset, featureWeights);
    Console.WriteLine();
    Console.WriteLine("[Black Mobility]");
    MobilityTuner.Report(FeatureTuner.AllWeights + mobilityOffset, featureWeights);

    Console.WriteLine();
    Console.WriteLine("Phase");
    PhaseTuner.Report(phaseWeights);
}
