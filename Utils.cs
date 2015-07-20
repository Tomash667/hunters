﻿using System;
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

        private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

        public static IEnumerable<Pos> Line(Pos start, Pos end)
        {
            int x0 = start.x,
                y0 = start.y,
                x1 = end.x,
                y1 = end.y;

            bool steep = (Math.Abs(y1 - y0) > Math.Abs(x1 - x0));
            if (steep)
            {
                Swap<int>(ref x0, ref y0);
                Swap<int>(ref x1, ref y1);
            }
            if (x0 > x1)
            {
                Swap<int>(ref x0, ref x1);
                Swap<int>(ref y0, ref y1);
            }
            int dX = (x1 - x0),
                dY = Math.Abs(y1 - y0),
                err = (dX / 2),
                ystep = (y0 < y1 ? 1 : -1),
                y = y0;
 
            for (int x = x0; x <= x1; ++x)
            {
                if (steep)
                    yield return new Pos(y, x);
                else
                    yield return new Pos(x, y);
                err = err - dY;
                if (err < 0)
                {
                    y += ystep;
                    err += dX;
                }
            }
        }

        public static IEnumerable<Pos> DrawCircle(Pos pos, int radius)
        {
            int x = radius;
            int y = 0;
            int decisionOver2 = 1 - x;   // Decision criterion divided by 2 evaluated at x=r, y=0

            while (x >= y)
            {
                yield return new Pos(x + pos.x, y + pos.y);
                yield return new Pos(x + pos.x, y + pos.y);
                yield return new Pos(y + pos.x, x + pos.y);
                yield return new Pos(-x + pos.x, y + pos.y);
                yield return new Pos(-y + pos.x, x + pos.y);
                yield return new Pos(-x + pos.x, -y + pos.y);
                yield return new Pos(-y + pos.x, -x + pos.y);
                yield return new Pos(x + pos.x, -y + pos.y);
                yield return new Pos(y + pos.x, -x + pos.y);
                y++;
                if (decisionOver2 <= 0)
                {
                    decisionOver2 += 2 * y + 1;   // Change in decision criterion for y -> y+1
                }
                else
                {
                    x--;
                    decisionOver2 += 2 * (y - x) + 1;   // Change for y -> y+1, x -> x-1
                }
            }
        }
    }
}
