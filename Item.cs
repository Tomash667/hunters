using hunters.Stats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            Gun,
            Armor,
            Potion,
            Ammo,
            Other
        }

        public enum WeaponType
        {
            Slash,
            Pierce,
            Blunt
        }

        public enum AmmoType
        {
            Bullet9mm
            // 12 gauge
            // .38
            // 5.56
        }

        public Type type;
        public string id, name, desc;
        public float weight;
        public int value;

        public int power; // attack for weapon/gun, defense for armor
        public int subtype; // damage type for weapon, ammo type for gun/ammo
        public int range, capacity; // for gun
        public Material material;

        public bool IsStackable
        {
            get
            {
                return type == Type.Other || type == Type.Potion || type == Type.Ammo;
            }
        }

        public bool IsEquipable
        {
            get
            {
                return type == Type.Weapon || type == Type.Armor || type == Type.Gun;
            }
        }

        public bool IsUseable
        {
            get
            {
                return type == Type.Potion || type == Type.Ammo || type == Type.Gun;
            }
        }

        public static Item Find(string id)
        {
            foreach(Item item in items)
            {
                if (item.id == id)
                    return item;
            }
            return null;
        }

        public static string ToString(WeaponType type)
        {
            switch(type)
            {
                case WeaponType.Slash:
                    return "slash";
                case WeaponType.Pierce:
                    return "pierce";
                case WeaponType.Blunt:
                    return "blunt";
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(WeaponType));
            }
        }

        public static string ToString(AmmoType type)
        {
            switch(type)
            {
                case AmmoType.Bullet9mm:
                    return "9 mm";
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(AmmoType));
            }
        }

        enum Keyword
        {
            Name,
            Desc,
            Value,
            Weight,
            Attack,
            Defense,
            Subtype,
            Material,
            Range,
            Capacity,

            Max
        }

        static bool CanHave(Type type, Keyword key)
        {
            switch(key)
            {
                case Keyword.Name:
                case Keyword.Desc:
                case Keyword.Weight:
                case Keyword.Value:
                    return true;
                case Keyword.Attack:
                    return type == Type.Weapon || type == Type.Gun;
                case Keyword.Subtype:
                    return type == Type.Weapon || type == Type.Gun || type == Type.Ammo;
                case Keyword.Material:
                    return type == Type.Weapon;
                case Keyword.Defense:
                    return type == Type.Armor;
                case Keyword.Capacity:
                case Keyword.Range:
                    return type == Type.Gun;
                default:
                    return false;
            }
        }

        static List<Item> items = new List<Item>();

        public static void LoadItems()
        {
            Game.Instance.Log("Loading items...");

            string path = "../../System/items.txt";

            Tokenizer t = new Tokenizer();
            int errors = 0;

            t.AddKeywords(new TupleList<string, Type>{
                {"weapon", Type.Weapon},
                {"gun", Type.Gun},
                {"armor", Type.Armor},
                {"potion", Type.Potion},
                {"ammo", Type.Ammo},
                {"other", Type.Other}
            });

            t.AddKeywords(new TupleList<string, Keyword>{
                {"name", Keyword.Name},
                {"desc", Keyword.Desc},
                {"value", Keyword.Value},
                {"weight", Keyword.Weight},
                {"attack", Keyword.Attack},
                {"defense", Keyword.Defense},
                {"subtype", Keyword.Subtype},
                {"material", Keyword.Material},
                {"range", Keyword.Range},
                {"capacity", Keyword.Capacity}
            });

            t.AddKeywords(new TupleList<string, WeaponType>{
                {"slash", WeaponType.Slash},
                {"pierce", WeaponType.Pierce},
                {"blunt", WeaponType.Blunt}
            });

            t.AddKeywords(new TupleList<string, Material>{
                {"wood", Material.Wood},
                {"iron", Material.Iron},
                {"steel", Material.Steel},
                {"silver", Material.Silver}
            });

            t.AddKeywords(new TupleList<string, AmmoType>{
                {"bullet9mm", AmmoType.Bullet9mm}
            });

            if (!t.FromFile(path))
                throw new Exception(string.Format("Failed to open file \"{0}\".", path));

            try
            {
                while(t.Next())
                {
                retry:
                    try
                    {
                        
                        Item item = new Item();

                        // type
                        item.type = t.GetKeyword<Type>();
                        t.Next();

                        // id
                        item.id = t.GetItem();
                        t.Next();

                        // {
                        t.AssertSymbol('{');
                        t.Next();

                        bool[] have = new bool[(int)Keyword.Max];

                        while(true)
                        {
                            if (t.IsSymbol('}'))
                                break;

                            Keyword key = t.GetKeyword<Keyword>();
                            string key_name = t.GetKeywordName();
                            t.Next();

                            if(have[(int)key])
                                throw new Exception(string.Format("Item '{0}' already have {1}.", item.id, key_name));
                            have[(int)key] = true;

                            if(!CanHave(item.type, key))
                                throw new Exception(string.Format("Item '{0}' can't have {1}.", item.id, key_name));

                            switch(key)
                            {
                                case Keyword.Name:
                                    item.name = t.GetString();
                                    break;
                                case Keyword.Desc:
                                    item.desc = t.GetString();
                                    break;
                                case Keyword.Value:
                                    item.value = t.GetInt();
                                    if (item.value < 0)
                                        throw new Exception(string.Format("Item '{0}' have negative value {1}.", item.id, item.value));
                                    break;
                                case Keyword.Weight:
                                    item.weight = t.GetNumberFloat();
                                    if (item.weight < 0)
                                        throw new Exception(string.Format("Item '{0}' have nagative weight {1}.", item.id, item.weight));
                                    break;
                                case Keyword.Attack:
                                    item.power = t.GetInt();
                                    if (item.power < 0)
                                        throw new Exception(string.Format("Item '{0}' have negative attack {1}.", item.id, item.power));
                                    break;
                                case Keyword.Defense:
                                    item.power = t.GetInt();
                                    if (item.power < 0)
                                        throw new Exception(string.Format("Item '{0}' can't have negative defense {1}.", item.id, item.power));
                                    break;
                                case Keyword.Subtype:
                                    if (item.type == Type.Weapon)
                                        item.subtype = (int)t.GetKeyword<WeaponType>();
                                    else
                                        item.subtype = (int)t.GetKeyword<AmmoType>();
                                    break;
                                case Keyword.Material:
                                    item.material = t.GetKeyword<Material>();
                                    break;
                                case Keyword.Capacity:
                                    item.capacity = t.GetInt();
                                    if(item.capacity < 1)
                                        throw new Exception(string.Format("Item '{0}' have negative capacity {1}.", item.id, item.capacity));
                                    break;
                                case Keyword.Range:
                                    item.range = t.GetInt();
                                    if(item.range < 1)
                                        throw new Exception(string.Format("Item '{0}' have negative range {1}.", item.id, item.range));
                                    break;
                            }

                            t.Next();
                        }

                        if (!have[(int)Keyword.Name])
                            throw new Exception(string.Format("Item '{0}' don't have name.", item.id));
                        if (!have[(int)Keyword.Desc])
                            throw new Exception(string.Format("Item '{0}' don't have description.", item.id));
                        if (!have[(int)Keyword.Weight])
                            throw new Exception(string.Format("Item '{0}' don't have weight.", item.id));
                        if (!have[(int)Keyword.Value])
                            throw new Exception(string.Format("Item '{0}' don't have value.", item.id));
                        if(item.type == Type.Weapon)
                        {
                            if (!have[(int)Keyword.Attack])
                                throw new Exception(string.Format("Item '{0}' don't have attack.", item.id));
                            if (!have[(int)Keyword.Subtype])
                                throw new Exception(string.Format("Item '{0}' don't have subtype.", item.id));
                            if (!have[(int)Keyword.Material])
                                throw new Exception(string.Format("Item '{0}' don't have material.", item.id));
                        }
                        else if(item.type == Type.Gun)
                        {
                            if (!have[(int)Keyword.Attack])
                                throw new Exception(string.Format("Item '{0}' don't have attack.", item.id));
                            if (!have[(int)Keyword.Subtype])
                                throw new Exception(string.Format("Item '{0}' don't have subtype.", item.id));
                            if (!have[(int)Keyword.Range])
                                throw new Exception(string.Format("Item '{0}' don't have range.", item.id));
                            if (!have[(int)Keyword.Capacity])
                                throw new Exception(string.Format("Item '{0}' don't have capacity.", item.id));
                        }
                        else if(item.type == Type.Armor)
                        {
                            if (!have[(int)Keyword.Defense])
                                throw new Exception(string.Format("Item '{0}' don't have defense.", item.id));
                        }
                        else if(item.type == Type.Ammo)
                        {
                            if (!have[(int)Keyword.Subtype])
                                throw new Exception(string.Format("Item '{0}' don't have subtype.", item.id));
                        }

                        items.Add(item);
                    }
                    catch(Exception e)
                    {
                        ++errors;
                        Game.Instance.Log(e.ToString());
                        t.SkipTo(Tokenizer.Token.KeywordGroupType, typeof(Type), null);
                        goto retry;
                    }
                }
            }
            catch(Exception e)
            {
                ++errors;
                Game.Instance.Log(string.Format("Failed to load items: {0}.", e.ToString()));
            }

            if (errors > 0)
                throw new Exception("Failed to load items, check log for details.");

            Game.Instance.Log("Finished loading items.");
        }
    }
}
