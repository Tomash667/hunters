using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Map
    {
        public int w, h;
        public List<Tile> m;

        public Map(int _w, int _h)
        {
            w = _w;
            h = _h;
            m = new List<Tile>(w * h);
            for(int i=0; i<w*h; ++i)
            {
                m.Add(new Tile(Utils.r.Next(100) > 90));
            }
        }

        public Tile this [int x, int y]
        {
            get
            {
                return m[x + y * w];
            }
        }

        public Tile this [Pos pos]
        {
            get
            {
                return m[pos.x + pos.y * w];
            }
        }

        public void Draw(Pos size, Pos offset, Pos buf_offset)
        {
            int left = Math.Max(0, offset.x);
            int right = Math.Min(w, offset.x + size.x);
            int top = Math.Max(0, offset.y);
            int bottom = Math.Min(h, offset.y + size.y);

            for(int y=top; y<bottom; ++y)
            {
                for (int x = left; x < right; ++x)
                    Console.buf[x - offset.x - buf_offset.x + (y - offset.y - buf_offset.y) * Console.Width].Char.UnicodeChar = m[x + y * w].GetGlyph();
            }
        }

        public bool CanMove(Pos old_pos, Pos new_pos, bool diagonal)
        {
            Tile t = m[new_pos.x + new_pos.y * w];
            if(!t.wall && t.unit == null)
            {
                if (!diagonal)
                    return true;
                if (!m[old_pos.x + new_pos.y * w].wall || !m[new_pos.x + old_pos.y * w].wall)
                    return true;
            }
            return false;
        }
    }
}
