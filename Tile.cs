using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Tile
    {
        public bool wall;
        public Unit unit;
        public List<ItemSlot> items;

        public Tile(bool _wall)
        {
            wall = _wall;
            unit = null;
            items = null;
        }

        public char GetGlyph()
        {
            if (unit != null)
                return '@';
            else if (wall)
                return '#';
            else if (items != null)
                return 'i';
            else
                return '.';
        }

        public void AddItem(Item item, int count)
        {
            ItemSlot.AddItem(new ItemSlot(item, count), items);
        }
    }
}
