using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Utils
    {
        public class SubtractiveGenerator
        {
            public static int MAX = 1000000000;
            private int[] state;
            private int pos;

            private int mod(int n)
            {
                return ((n % MAX) + MAX) % MAX;
            }

            public SubtractiveGenerator(int seed)
            {
                state = new int[55];

                int[] temp = new int[55];
                temp[0] = mod(seed);
                temp[1] = 1;
                for (int i = 2; i < 55; ++i)
                    temp[i] = mod(temp[i - 2] - temp[i - 1]);

                for (int i = 0; i < 55; ++i)
                    state[i] = temp[(34 * (i + 1)) % 55];

                pos = 54;
                for (int i = 55; i < 220; ++i)
                    next();
            }

            public int next()
            {
                int temp = mod(state[(pos + 1) % 55] - state[(pos + 32) % 55]);
                pos = (pos + 1) % 55;
                state[pos] = temp;
                return temp;
            }

            public void Save(BinaryWriter f)
            {
                f.Write(pos);
                for (int i = 0; i < 55; ++i)
                    f.Write(state[i]);
            }

            public void Load(BinaryReader f)
            {
                f.Read(out pos);
                for (int i = 0; i < 55; ++i)
                    f.Read(out state[i]);
            }
        }

        public static SubtractiveGenerator rnd = new SubtractiveGenerator((int)DateTime.Now.Ticks);

        public static int Random(int a)
        {
            return rnd.next() % a;
        }

        public static int Random(int a, int b)
        {
            return rnd.next() % (b - a + 1) + a;
        }

        public static bool Chance(int c)
        {
            return Random(100) <= c;
        }

        public static bool K2()
        {
            return Random(2) == 0;
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

        public static string Unescape(string str, int pos, int pos2)
        {
            // !!!!!!!!!!!!!
            return "";
        }

        public enum StringToNumberResult
        {
            Broken,
            Int,
            Float
        }

        public static StringToNumberResult StringToNumber(string str, out long i, out float f)
        {
            /*
             * i = 0;
            f = 0;
            uint diver = 10;
            uint digits = 0;
            char c;
            var it = str.GetEnumerator();
            bool sign = false;
            if (it.Current == '-')
            {
                sign = true;
                it.MoveNext();
            }

            do
            {
                c = it.Current;
                if (c == '.')
                {
                    it.MoveNext();
                    break;
                }
                else if (c >= '0' && c <= '9')
                {
                    i *= 10;
                    i += (int)c - '0';
                }
                else
                    return StringToNumberResult.Broken;
            }
            while (it.MoveNext());
             * */
            i = 0;
            f = 0;
            uint diver = 10;
            uint digits = 0;
            char c;
            bool sign = false;
            if (*s == '-')
            {
                sign = true;
                ++s;
            }

            while ((c = *s) != 0)
            {
                if (c == '.')
                {
                    ++s;
                    break;
                }
                else if (c >= '0' && c <= '9')
                {
                    i *= 10;
                    i += (int)c - '0';
                }
                else
                    return StringToNumberResult.Broken;
                ++s;
            }

            if (c == 0)
            {
                if (sign)
                    i = -i;
                f = (float)i;
                return StringToNumberResult.Int;
            }

            while ((c = *s) != 0)
            {
                if (c == 'f')
                {
                    if (digits == 0)
                        return StringToNumberResult.Broken;
                    break;
                }
                else if (c >= '0' && c <= '9')
                {
                    ++digits;
                    f += ((float)((int)c - '0')) / diver;
                    diver *= 10;
                }
                else
                    return StringToNumberResult.Broken;
                ++s;
            }
            f += (float)i;
            if (sign)
            {
                f = -f;
                i = -i;
            }
            return StringToNumberResult.Float;
        }
    }
}
