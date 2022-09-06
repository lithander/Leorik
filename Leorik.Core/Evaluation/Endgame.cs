using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leorik.Core
{
    public static class Endgame
    {
        public static HashSet<string> Drawn = new()
        {
            "KNvK", "KvKN",
            "KBvK", "KvKB",
            "KNNvK", "KvKNN",

            "KNNvKN", "KNvKNN",
            "KNNvKB", "KBvKNN",
            
            "KRvKN", "KNvKR",
            "KRvKB", "KBvKR"
        };
    }
}
