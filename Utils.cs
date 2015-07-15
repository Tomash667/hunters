using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Utils
    {
        public static Random r = new Random();

        public static bool Chance(int c)
        {
            return r.Next(100) <= c;
        }

        public static bool K2()
        {
            return r.Next(2) == 0;
        }
    }
}
