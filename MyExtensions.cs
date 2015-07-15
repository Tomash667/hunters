using System;
using System.Collections.Generic;
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
}
