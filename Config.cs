using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    /*  [keys]
     *  drop = d
     *  help = ?
     *  closeFast = c
     *  close = C
     *  unk = alt+c
    
    
     */
  
    class Config
    {
        public class Section
        {
            public string name;
            public Dictionary<string, string> items = new Dictionary<string, string>();

            public string this[string key]
            {
                get
                {
                    return items[key];
                }
                set
                {
                    items[key] = value;
                }
            }
        }

        List<Section> sections = new List<Section>();

        public Section this[string name]
        {
            get
            {
                Section section = sections.SingleOrDefault(x => x.name == name);
                if(section == null)
                {
                    section = new Section {name = name};
                    sections.Add(section);
                }
                return section;
            }
        }

        public void Parse(string filename)
        {
            Tokenizer t = new Tokenizer();
            if (!t.FromFile(filename))
                return;

            sections.Clear();
            Section global = this[""];
            Section section = global;

            try
            {
                while(t.Next())
                {
                    if(t.IsSymbol('['))
                    {
                        t.Next();
                        section = this[t.GetItem()];
                        t.Next();
                        t.AssertSymbol(']');
                    }
                    else
                    {
                        string key = t.GetItem();
                        t.Next();
                        t.AssertSymbol('=');
                        t.NextLine();
                        string value = t.GetItem();
                        section[key] = value;
                    }
                }
            }
            catch(Exception e)
            {
                string err = e.ToString();
            }
        }
    }
}
