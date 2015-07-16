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
        public string name;
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
            new Item {name = "knife", type = Type.Weapon, value = 2},
            new Item {name = "leather jacket", type = Type.Armor, value = 1},
            new Item {name = "potion", type = Type.Potion},
            new Item {name = "stuff", type = Type.Other}
        };
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
