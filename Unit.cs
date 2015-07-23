using System;
using System.Collections.Generic;
using System.IO;
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
        public int hp, hpmax, id;
        public bool ai;
        public GameItem weapon, armor;
        public List<GameItem> items;

        public Unit()
        {
            hpmax = 10;
            hp = hpmax;
            weapon = null;
            armor = null;
            items = new List<GameItem>();
        }

        public void AddItem(GameItem slot)
        {
            GameItem.AddItem(slot, items);
        }

        public IEnumerable<IndexedItem<GameItem>> Items
        {
            get
            {
                return GetEquipped().Concat(items.GetIndexes());
            }
        }

        public IEnumerable<IndexedItem<GameItem>> GetEquipped()
        {
            if (weapon != null)
                yield return new IndexedItem<GameItem>(weapon, INDEX_WEAPON);
            if (armor != null)
                yield return new IndexedItem<GameItem>(armor, INDEX_ARMOR);
        }

        public static int GetIndex(Item.Type type)
        {
            switch(type)
            {
                case Item.Type.Weapon:
                    return INDEX_WEAPON;
                case Item.Type.Armor:
                    return INDEX_ARMOR;
                default:
                    return 0;
            }
        }

        public void Equip(GameItem item)
        {
            switch(item.item.type)
            {
                case Item.Type.Weapon:
                    weapon = item;
                    break;
                case Item.Type.Armor:
                    armor = item;
                    break;
            }
        }

        public GameItem GetEquipped(int index)
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

        public void Save(BinaryWriter f)
        {
            f.Write(id);
            f.Write(pos);
            f.Write(hp);
            f.Write(hpmax);
            f.Write(ai);
            f.Write(weapon);
            f.Write(armor);
            f.Write(items);
        }

        public void Load(BinaryReader f)
        {
            f.Read(out id);
            f.Read(out pos);
            f.Read(out hp);
            f.Read(out hpmax);
            f.Read(out ai);
            f.Read(out weapon);
            f.Read(out armor);
            f.Read(out items);
        }

        public static List<Unit> units;
    }
}
