using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class GameItem
    {
        public Item item;
        public int count, ammo;

        public GameItem(Item _item, int _count, int _ammo=0)
        {
            item = _item;
            count = _count;
            ammo = _ammo;
        }

        public static void AddItem(GameItem slot, List<GameItem> slots)
        {
            if (slot.item.IsStackable)
            {
                foreach(GameItem item in slots)
                {
                    if(item.item == slot.item)
                    {
                        item.count += slot.count;
                        return;
                    }
                }
            }

            slots.Add(slot);
        }

        public override string ToString()
        {
            switch (item.type)
            {
                case Item.Type.Weapon:
                    return string.Format("{0} (attack {1} {2})", item.name, item.power,
                        Item.ToString((Item.WeaponType)item.subtype));
                case Item.Type.Gun:
                    return string.Format("{0} (attack {1}, {2} {3}/{4}, range {5})", item.name, item.power,
                        Item.ToString((Item.AmmoType)item.subtype), ammo, item.capacity, item.range);
                case Item.Type.Armor:
                    return string.Format("{0} (defense {1})", item.name, item.power);
                case Item.Type.Potion:
                    return string.Format("{0} (heals)", Name);
                case Item.Type.Ammo:
                    return string.Format("{0} ({1})", Name, Item.ToString((Item.AmmoType)item.subtype));
                default:
                case Item.Type.Other:
                    return Name;
            }
        }

        public string Name
        {
            get
            {
                if (count > 1)
                    return string.Format("{0} {1}s", count, item.name);
                else
                    return item.name;
            }
        }
    }
}
