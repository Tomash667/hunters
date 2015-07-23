using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Tile
    {
        public enum Type
        {
            Empty,
            Wall,
            Door
        }

        [Flags]
        public enum Flags
        {
            Lit = 1<<0,
            Known = 1<<1,
            Open = 1<<2,
            LastOpen = 1<<3
        }

        public Unit unit;
        public List<GameItem> items;
        public Type type;
        public Flags flags;

        public Tile()
        {
            int c = Utils.Random(100);
            if (c < 70)
                type = Type.Empty;
            else if (c < 90)
                type = Type.Wall;
            else
                type = Type.Door;
            unit = null;
            items = null;
            flags = 0;
        }

        public char GetGlyph(out bool lit)
        {
            if((flags & Flags.Known) == 0)
            {
                lit = false;
                return ' ';
            }
            if ((flags & Flags.Lit) != 0)
            {
                lit = true;
                if (unit != null)
                    return '@';
                else if (items != null)
                    return 'i';
                else if(type == Type.Door)
                {
                    if((flags & Flags.Open) != 0)
                    {
                        flags |= Flags.LastOpen;
                        return '-';
                    }
                    else
                    {
                        flags &= ~Flags.LastOpen;
                        return '+';
                    }

                }
            }
            else
            {
                lit = false;
                if(type == Type.Door)
                {
                    if((flags & Flags.LastOpen) != 0)
                        return '-';
                    else
                        return '+';
                }
            }
            if(type == Type.Wall)
                return '#';
            else
                return '.';
        }

        public void AddItem(Item item, int count, int ammo)
        {
            GameItem.AddItem(new GameItem(item, count, ammo), items);
        }

        public bool CanMove()
        {
            if (type == Type.Empty)
                return true;
            else if (type == Type.Door)
                return (flags & Flags.Open) != 0;
            else
                return false;
        }

        public void Save(BinaryWriter f)
        {
            f.Write(unit);
            f.Write(items);
            f.Write((byte)type);
            f.Write((byte)flags);
        }

        public void Load(BinaryReader f)
        {
            f.Read(out unit);
            f.Read(out items);
            byte b;
            f.Read(out b);
            type = (Type)b;
            f.Read(out b);
            flags = (Flags)b;
        }
    }
}
