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

        public static bool operator == (Pos a, Pos b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator != (Pos a, Pos b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public int Distance(Pos a)
        {
            return Math.Max(Math.Abs(x - a.x), Math.Abs(y - a.y));
        }

        public override bool Equals(object obj)
        {
            return obj is Pos && this == (Pos)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                return hash;
            }
        }
    }
}
