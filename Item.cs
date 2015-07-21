using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Item
    {
        public enum Type
        {
            Weapon,
            Armor,
            Potion,
            Other
        }

        /*public enum WeaponType
        {
            Melee,
            Gun
        }*/

        public Type type;
        public string id, name;
        public int value;
        //public int subtype;

        public bool IsStackable
        {
            get
            {
                return type == Type.Other;
            }
        }

        public bool IsEquipable
        {
            get
            {
                return type == Type.Weapon || type == Type.Armor;
            }
        }

        public bool IsUseable
        {
            get
            {
                return type == Type.Potion;
            }
        }

        public override string ToString()
        {
            switch(type)
            {
                case Type.Weapon: return string.Format("{0} (attack {1})", name, value);
                case Type.Armor: return string.Format("{0} (defense {1})", name, value);
                case Type.Potion: return string.Format("{0} (heals)", name);
                default:
                case Type.Other: return name;
            }
        }

        public static Item[] items = new Item[] {
            new Item {id = "knife", name = "knife", type = Type.Weapon, value = 2},
            new Item {id = "ljacket", name = "leather jacket", type = Type.Armor, value = 1},
            new Item {id = "potion", name = "potion", type = Type.Potion},
            new Item {id = "stuff", name = "stuff", type = Type.Other}
        };

        public static Item Find(string id)
        {
            foreach(Item item in items)
            {
                if (item.id == id)
                    return item;
            }
            return null;
        }
    }

    class ItemSlot
    {
        public Item item;
        public int count;

        public ItemSlot(Item _item, int _count)
        {
            item = _item;
            count = _count;
        }

        public static void AddItem(ItemSlot slot, List<ItemSlot> slots)
        {
            if (slot.item.IsStackable)
            {
                foreach(ItemSlot item in slots)
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
    }
}
