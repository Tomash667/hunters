using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Unit
    {
        public const int INDEX_WEAPON = -1;
        public const int INDEX_ARMOR = -2;

        public Pos pos;
        public int hp, hpmax;
        public bool ai;
        public Item weapon, armor;
        public List<ItemSlot> items;

        public Unit()
        {
            hpmax = 10;
            hp = hpmax;
            weapon = null;
            armor = null;
            items = new List<ItemSlot>();
        }

        public void AddItem(ItemSlot slot)
        {
            ItemSlot.AddItem(slot, items);
        }

        public IEnumerable<IndexedItem<ItemSlot>> Items
        {
            get
            {
                foreach (var a in GetEquipped())
                    yield return new IndexedItem<ItemSlot>(new ItemSlot(a.item, 1), a.index);
                foreach (var a in items.GetIndexes())
                    yield return a;
            }
        }

        public IEnumerable<IndexedItem<Item>> GetEquipped()
        {
            if (weapon != null)
                yield return new IndexedItem<Item>(weapon, INDEX_WEAPON);
            if (armor != null)
                yield return new IndexedItem<Item>(armor, INDEX_ARMOR);
        }

        public static int GetIndex(Item item)
        {
            switch(item.type)
            {
                case Item.Type.Weapon:
                    return INDEX_WEAPON;
                case Item.Type.Armor:
                    return INDEX_ARMOR;
                default:
                    return 0;
            }
        }

        public void Equip(Item item)
        {
            switch(item.type)
            {
                case Item.Type.Weapon:
                    weapon = item;
                    break;
                case Item.Type.Armor:
                    armor = item;
                    break;
            }
        }

        public Item GetEquipped(int index)
        {
            switch(index)
            {
                case INDEX_WEAPON:
                    return weapon;
                case INDEX_ARMOR:
                    return armor;
                default:
                    return null;
            }
        }

        public void RemoveEquipped(int index)
        {
            switch(index)
            {
                case INDEX_WEAPON:
                    weapon = null;
                    break;
                case INDEX_ARMOR:
                    armor = null;
                    break;
            }
        }
    }
}
