using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    static class MyExtensions
    {
        public static IEnumerable<string> SplitText(this string s, int limit, bool even=false)
        {
            if (s.Length <= 0)
                yield break;

            if (s.Length <= limit)
            {
                yield return s;
                yield break;
            }

            int parts = s.Length / limit + 1;
            int part_len = (even ? s.Length / parts + 1 : limit);
            int off = 0;

            for (int i = 0; i < parts; ++i)
            {
                int start = off;
                off += part_len;
                if(off >= s.Length)
                {
                    yield return s.Substring(start);
                    yield break;
                }
                if (s[off] != ' ')
                {
                    // seek forward
                    int next = off + 1;
                    bool found = false;
                    while (next < s.Length)
                    {
                        if (s[next] == ' ')
                        {
                            found = true;
                            break;
                        }
                        ++next;
                    }
                    if (found && next - start < limit)
                    {
                        // found at right
                        off = next;
                    }
                    else
                    {
                        // seek back
                        next = off - 1;
                        found = false;
                        while (next > start)
                        {
                            if (s[next] == ' ')
                            {
                                found = true;
                                break;
                            }
                            --next;
                        }
                        if (found)
                        {
                            // found at left
                            off = next;
                        }
                        // else nothing found, split here
                    }
                }

                int len = off - start;
                ++off;
                yield return s.Substring(start, len);
            }
        }

        public static void Join(this StringBuilder s, List<string> strs)
        {
            if (strs.Count == 0)
                return;
            else if(strs.Count == 1)
            {
                s.Append(strs[0]);
            }
            else if(strs.Count == 2)
            {
                s.Append(strs[0]);
                s.Append(" and ");
                s.Append(strs[1]);
            }
            else
            {
                for(int i=0; i<strs.Count-2; ++i)
                {
                    s.Append(strs[i]);
                    s.Append(", ");
                }
                s.Append(strs[strs.Count - 2]);
                s.Append(" and ");
                s.Append(strs[strs.Count - 1]);
            }
        }

        public static IEnumerable<IndexedItem<T>> GetIndexes<T>(this IEnumerable<T> items)
        {
            return items.Select((item, index) => new IndexedItem<T>(item, index));
        }

        public static void Reserve<T>(this List<T> items, int count)
        {
            items.Capacity = Math.Max(items.Capacity, count);
        }

        public static string Up(this string s)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static T MaxBy<T>(this IEnumerable<T> items, Func<T, int> selector)
        {
            var it = items.GetEnumerator();

            T best = it.Current;
            int best_val = selector(it.Current);

            while(it.MoveNext())
            {
                int val = selector(it.Current);
                if(val > best_val)
                {
                    best_val = val;
                    best = it.Current;
                }
            }

            return best;
        }

        /*public static bool IsSet<T>(this T _value, T _flag) where T : Enum
        {
            long value = Convert.ToInt64(_value);
            long flag = Convert.ToInt64(_flag);
            return (value & flag) != 0;
        }*/

        public static void Write(this BinaryWriter f, Pos pos)
        {
            f.Write(pos.x);
            f.Write(pos.y);
        }

        public static void Write(this BinaryWriter f, Item item)
        {
            if (item != null)
                f.Write(item.id);
            else
                f.Write((byte)0);
        }

        public static void Write(this BinaryWriter f, List<ItemSlot> items)
        {
            if (items == null || items.Count == 0)
                f.Write(0);
            else
            {
                f.Write(items.Count);
                foreach(ItemSlot slot in items)
                {
                    f.Write(slot.item.id);
                    f.Write(slot.count);
                }
            }
        }

        public static void Write(this BinaryWriter f, Unit unit)
        {
            if (unit != null)
                f.Write(unit.id);
            else
                f.Write(-1);
        }

        public static void Read(this BinaryReader f, out int result)
        {
            result = f.ReadInt32();
        }

        public static void Read(this BinaryReader f, out byte result)
        {
            result = f.ReadByte();
        }

        public static void Read(this BinaryReader f, out bool result)
        {
            result = f.ReadBoolean();
        }

        public static void Read(this BinaryReader f, out Pos result)
        {
            result = new Pos(f.ReadInt32(), f.ReadInt32());
        }

        public static void Read(this BinaryReader f, out Item item)
        {
            string s = f.ReadString();
            if (s.Length == 0)
                item = null;
            else
            {
                item = Item.Find(s);
                if (item == null)
                    throw new Exception(string.Format("Invalid item id '{0}'.", s));
            }
        }

        public static void Read(this BinaryReader f, out List<ItemSlot> items)
        {
            int count = f.ReadInt32();
            if (count == 0)
                items = null;
            else
            {
                Item item;
                int count2;
                items = new List<ItemSlot>(count);
                for(int i=0; i<count; ++i)
                {
                    f.Read(out item);
                    count2 = f.ReadInt32();
                    if(count2 < 1)
                        throw new Exception(string.Format("Item '{0}' have count '{1}'.", item.id, count2));
                    items.Add(new ItemSlot(item, count2));
                }
            }
        }

        public static void Read(this BinaryReader f, out Unit unit)
        {
            int id = f.ReadInt32();
            if (id == -1)
                unit = null;
            else
                unit = Unit.units[id];
        }
    }

    public struct IndexedItem<T>
    {
        public T item;
        public int index;

        public IndexedItem(T _item, int _index)
        {
            item = _item;
            index = _index;
        }
    }

    public class LList<T>
    {
        public class Item
        {
            public T item;
            public Item next, prev;

            public Item(T _item)
            {
                item = _item;
                next = null;
                prev = null;
            }
        }

        public struct Iterator
        {
            public Item item;
            public LList<T> list;

            public Iterator(Item _item, LList<T> _list)
            {
                item = _item;
                list = _list;
            }

            public static bool operator == (Iterator a, Iterator b)
            {
                Debug.Assert(a.list == b.list);
                return a.item == b.item;
            }

            public static bool operator != (Iterator a, Iterator b)
            {
                Debug.Assert(a.list == b.list);
                return a.item != b.item;
            }

            public T Current
            {
                get
                {
                    Debug.Assert(item != null);
                    return item.item;
                }
            }

            public static Iterator operator ++ (Iterator a)
            {
                Debug.Assert(a.item != null && a.item.next != null);
                return new Iterator(a.item.next, a.list);
            }

            public override bool Equals(object o)
            {
                if (o == null)
                    return false;

                if (o is Iterator)
                {
                    Iterator a = (Iterator)o;
                    return item == a.item;
                }
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return item.GetHashCode();
            }
        }

        Item first;
        Item last;
        int count;

        public LList()
        {
            Clear();
        }

        public Iterator Add(T item)
        {
            Item i = new Item(item);

            if(last != null)
            {
                i.prev = last;
                last.next = i;
                last = i;
            }
            else
            {
                first = i;
                last = i;
            }

            ++count;
            return new Iterator(i, this);
        }

        public Iterator Begin()
        {
            return new Iterator(first, this);
        }

        public Iterator End()
        {
            return new Iterator(null, this);
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public Iterator Insert(Iterator at, T item)
        {
            Debug.Assert(at.list == this);
            if(at.item == null)
            {
                // add at end
                return Add(item);
            }
            else
            {
                Item i = new Item(item);
                // set previous
                if(at.item.prev != null)
                    at.item.prev.next = i;
                // set current
                i.prev = at.item.prev;
                i.next = at.item;
                // set next
                at.item.prev = i;

                if (at.item == first)
                    first = i;

                ++count;
                return new Iterator(i, this);
            }
        }

        public Iterator Erase(Iterator at)
        {
            Debug.Assert(at.list == this && at.item != null);
            // set prev
            if (at.item.prev != null)
                at.item.prev.next = at.item.next;
            else
                first = at.item.next;
            // set next
            if (at.item.next != null)
                at.item.next.prev = at.item.prev;
            else
                last = null;
            --count;
            if (at.item.next != null)
                return new Iterator(at.item.next, this);
            else
                return End();
        }

        public void Clear()
        {
            first = null;
            last = null;
            count = 0;
        }
    }
}
