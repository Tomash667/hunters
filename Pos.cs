using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    struct Pos
    {
        public int x, y;

        public Pos(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public static Pos operator + (Pos a, Pos b)
        {
            return new Pos(a.x + b.x, a.y + b.y);
        }

        public static Pos operator - (Pos a, Pos b)
        {
            return new Pos(a.x - b.x, a.y - b.y);
        }

        public static Pos operator / (Pos a, int v)
        {
            return new Pos(a.x / v, a.y / v);
        }

        public static Pos operator * (Pos a, int v)
        {
            return new Pos(a.x * v, a.y * v);
        }

        public int Distance(Pos a)
        {
            return Math.Max(Math.Abs(x - a.x), Math.Abs(y - a.y));
        }
    }
}
