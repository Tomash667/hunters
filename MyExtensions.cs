using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    public static class MyExtensions
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

        public static Iterator<T> Begin<T>(this List<T> items)
        {
            return new Iterator<T>(items, 0);
        }

        public static Iterator<T> End<T>(this List<T> items)
        {
            return new Iterator<T>(items, items.Count);
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

    public struct Iterator<T>
    {
        public List<T> items;
        public int index;

        public Iterator(List<T> _items, int _index)
        {
            items = _items;
            index = _index;
        }

        public Iterator<T> Insert(T what)
        {
            items.Insert(index, what);
            Iterator<T> result = new Iterator<T>(items, index);
            ++index;
            return result;
        }

        public void Erase()
        {
            if (index < items.Count)
                items.RemoveAt(index);
        }

        public T Current
        {
            get
            {
                return items[index];
            }
        }

        public static bool operator == (Iterator<T> a, Iterator<T> b)
        {
            Debug.Assert(a.items == b.items);
            return a.index == b.index;
        }

        public static bool operator != (Iterator<T> a, Iterator<T> b)
        {
            Debug.Assert(a.items == b.items);
            return a.index != b.index;
        }

        public static Iterator<T> operator ++ (Iterator<T> a)
        {
            return new Iterator<T>(a.items, a.index + 1);
        }

        public override bool Equals(object o)
        {
            if (o == null)
                return false;

            if (o is Iterator<T>)
            {
                Iterator<T> a = (Iterator<T>)o;
                return items == a.items && index == a.index;
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + items.GetHashCode();
            hash = (hash * 7) + index.GetHashCode();
            return hash;
        }
    }
}
