using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hunters
{
    class Tokenizer
    {
        public enum Token
        {
            None,
            Item,
            Keyword,
            Symbol,
            Int,
            Float,
            String,
            Eof,
            Eol,

            // special
            Number,
            KeywordGroup,
            KeywordGroupType
        }
        
        [Flags]
        public enum Flags
        {
            JoinMinus = 1<<0,
            JoinDot = 1<<1,
            Unescape = 1<<2
        }

        class Keyword
        {
            public string name;
            public int id, group;
        }

        const int NPOS = -1;

        Flags flags;
        Token token;
        string str, item;
        char symbol;
        int pos, line, charpos, _int;
        float _float;
        uint _uint;
        Dictionary<string, Keyword> keywords = new Dictionary<string, Keyword>();
        Keyword keyword;

        public Tokenizer(Flags _flags = Flags.Unescape)
        {
            flags = _flags;
        }

        public bool FromFile(string filepath)
        {
            pos = 0;
            line = 0;
            charpos = 0;
            token = Token.None;

            try
            {
                str = File.ReadAllText(filepath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Next(bool return_eol=false)
        {
            redo:
            if(IsEof())
                return false;

            if(pos >= str.Length)
            {
                token = Token.Eof;
                return false;
            }

            int pos2 = FindFirstNotOf(return_eol ? " \t" : " \t\n\r", pos);
	        if(pos2 == NPOS)
	        {
                // only whitespaces left, end of file
                token = Token.Eof;
                return false;
	        }

	        const string symbols = ",./;'\\[]`<>?:|{}=~!@#$%^&*()+-";
	        char c = str[pos2];

	        if(c == '\r')
	        {
		        pos = pos2+1;
		        if(pos < str.Length && str[pos] == '\n')
			        ++pos;
		        token = Token.Eol;
	        }
	        else if(c == '\n')
	        {
		        pos = pos2+1;
                token = Token.Eol;
	        }
	        if(c == '/')
	        {
		        char c2 = str[pos2+1];
		        if(c2 == '/')
		        {
                    // line comment, skip to end of line
			        pos = FindFirstOf("\n", pos2+1);
			        if(pos == NPOS)
			        {
                        token = Token.Eof;
				        return false;
			        }
			        else
				        goto redo;
		        }
		        else if(c2 == '*')
		        {
                    // multiline comment, search for end of comment
			        int prev_line = line;
			        int prev_charpos = charpos;
			        pos = FindFirstOfStr("*/", pos2+1);
			        if(pos == NPOS)
                        throw new Exception(string.Format("({0},{1}) Not closed comment started at line {2}, character {3}!",
                            line+1, charpos+1, prev_line+1, prev_charpos+1));
			        goto redo;
		        }
		        else
		        {
                    // / symbol
			        ++charpos;
			        pos = pos2+1;
                    token = Token.Symbol;
                    item = c.ToString();
		        }
	        }
	        else if(c == '"')
	        {
		        // quote, search for end
		        int cp = charpos;
		        pos = FindEndOfQuote(pos2+1);

		        if(pos == NPOS || str[pos] != '"')
			        throw new Exception(string.Format("({0},{1}) Not closed \" opened at {2}!", line+1, charpos+1, cp+1));

                if((flags & Flags.Unescape) != 0)
                    item = Utils.Unescape(str, pos2 + 1, pos - pos2 - 1);
                else
                    item = str.Substring(pos2 + 1, pos - pos2 - 1);
                token = Token.String;
		        ++pos;
	        }
	        else if(c == '-' && (flags & Flags.JoinMinus) != 0)
	        {
		        ++charpos;
		        pos = pos2+1;
		        int old_pos = pos;
		        int old_charpos = charpos;
		        int old_line = line;

		        // find next character
		        pos2 = FindFirstNotOf(return_eol ? " \t" : " \t\n\r", pos);
                if (pos2 == NPOS)
                {
                    // only whitespaces, end of file after minus
                    token = Token.Symbol;
                    symbol = '-';
                    item = "-";
                }
                else
                {
                    c = str[pos2];
                    if (c >= '0' && c <= '9')
                    {
                        // negative number
                        pos = FindFirstOf(" \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\"", pos2);
                        if (pos2 == NPOS)
                        {
                            pos = str.Length;
                            item = str.Substring(pos2);
                        }
                        else
                            item = str.Substring(pos2, pos - pos2);

                        long val;
                        Utils.StringToNumberResult result = Utils.StringToNumber(item, out val, out _float);
                        _int = -(int)val;
                        _uint = 0;
                        _float = -_float;
                        if (-val < int.MinValue)
                            throw new Exception(string.Format("Too big number {0} to store as int.", val));
                        if (result == Utils.StringToNumberResult.Float)
                            token = Token.Float;
                        else if (result == Utils.StringToNumberResult.Int)
                            token = Token.Int;
                        else
                            token = Token.Item;
                    }
                    else
                    {
                        // not a number, minus
                        token = Token.Symbol;
                        symbol = '-';
                        item = "-";
                        pos = old_pos;
                        charpos = old_charpos;
                        line = old_line;
                    }
                }
	        }
	        else if(symbols.IndexOf(c) != -1)
	        {
		        // symbol
		        ++charpos;
		        pos = pos2+1;
                token = Token.Symbol;
		        symbol = c;
		        item = c.ToString();
	        }
	        else
	        {
		        // find end of item
		        bool ignore_dot = false;
		        if((c >= '0' && c <= '9') || (flags & Flags.JoinDot) != 0)
			        ignore_dot = true;
		        pos = FindFirstOf(ignore_dot ? " \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\"" : " \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\".", pos2);
		        if(pos2 == NPOS)
		        {
                    pos = str.Length;
			        item = str.Substring(pos2);
		        }
		        else
			        item = str.Substring(pos2, pos-pos2);

		        // is that number
		        if(c >= '0' && c <= '9')
		        {
			        long val;
                    Utils.StringToNumberResult result = Utils.StringToNumber(item, out val, out _float);
                    _int = (int)val;
                    _uint = (uint)val;
                    if (val > uint.MaxValue)
                        throw new Exception(string.Format("Too big number {0} to store as uint.", val));

                    if (result == Utils.StringToNumberResult.Float)
                        token = Token.Float;
                    else if (result == Utils.StringToNumberResult.Int)
                        token = Token.Int;
                    else
                        token = Token.Item;
		        }
		        else
		        {
                    // keyword or item
                    if(keywords.TryGetValue(item, out keyword))
                        token = Token.Keyword;
                    else
                        token = Token.Item;
		        }
	        }

	        return true;
        }

        public bool NextLine()
        {
            if(IsEof())
		        return false;

	        if(pos >= str.Length)
	        {
                token = Token.Eof;
		        return false;
	        }

	        int pos2 = FindFirstOf("\n\r", pos);
	        if(pos2 == NPOS)
		        item = str.Substring(pos);
	        else
		        item = str.Substring(pos, pos2-pos);
	
	        token = Token.Item;
            pos = pos2;
            return item.Length > 0;
        }

        public void SkipTo(Token _token, object what, object what2)
        {
            do
            {
                if(token == _token)
                {
                    if(what == null)
                        return;
                    switch(token)
                    {
                        case Token.None:
                        case Token.Eof:
                        case Token.Eol:
                            return;
                        case Token.Item:
                        case Token.String:
                            if(item == (string)what)
                                return;
                            break;
                        case Token.Keyword:
                            if(what2 == null)
                            {
                                // keyword with id
                                if(keyword.id == (int)what)
                                    return;
                            }
                            else
                            {
                                // keyword with id/group
                                if(keyword.id == (int)what && keyword.group == (int)what2)
                                    return;
                            }
                            break;
                        case Token.Symbol:
                            if(symbol == (char)what)
                                return;
                            break;
                        case Token.Int:
                            if(_int == (int)what)
                                return;
                            break;
                        case Token.Float:
                            if(_float == (float)what)
                                return;
                            break;
                    }
                }
                else if(_token >= Token.Number)
                {
                    switch(_token)
                    {
                        case Token.Number:
                            if(token == Token.Int || token == Token.Float)
                            {
                                if (what == null)
                                    return;
                                else if(what is int)
                                {
                                     if (_int == (int)what)
                                         return;
                                }
                                else
                                {
                                    if (_float == (float)what)
                                        return;
                                }
                            }
                            break;
                        case Token.KeywordGroup:
                            if(token == Token.Keyword)
                            {
                                if (keyword.group == (int)what)
                                    return;
                            }
                            break;
                        case Token.KeywordGroupType:
                            if(token == Token.Keyword)
                            {
                                int _group = ((Type)what).GetHashCode();
                                if (keyword.group == _group)
                                    return;
                            }
                            break;
                    }
                }
            }
            while (Next());
        }

        int FindFirstOf(string match, int start)
        {
            char c;

	        for(int i=start, end = str.Length; i<end; ++i)
	        {
		        c = str[i];

		        for(int j=0; j<match.Length; ++j)
		        {
			        if(c == match[j])
				        return i;
		        }

		        if(c == '\n')
		        {
			        ++line;
			        charpos = 0;
		        }
		        else
			        ++charpos;
	        }

	        return NPOS;
        }

        int FindFirstNotOf(string match, int start)
        {
	        char c;
	        bool found;

	        for(int i=start, end = str.Length; i<end; ++i)
	        {
                c = str[i];
		        found = false;

		        for(int j=0; j<match.Length; ++j)
		        {
			        if(c == match[j])
			        {
				        found = true;
				        break;
			        }
		        }

		        if(!found)
			        return i;

		        if(c == '\n')
		        {
			        ++line;
			        charpos = 0;
		        }
		        else
			        ++charpos;
	        }

	        return NPOS;
        }

        int FindFirstOfStr(string match, int start)
        {
	        for(int i=start, end = str.Length; i<end; ++i)
	        {
		        char c = str[i];
		        if(c == match[0])
		        {
                    bool ok = true;
                    for(int j=0; j<match.Length; ++j)
                    {
                        ++charpos;
                        ++i;
                        if (i == end)
                            return NPOS;
                        if(match[j] != str[i])
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok)
                        return i;
		        }
		        else if(c == '\n')
		        {
			        ++line;
			        charpos = 0;
		        }
		        else
			        ++charpos;
	        }

	        return NPOS;
        }

        int FindEndOfQuote(int start)
        {
	        for(int i=start, end = str.Length; i<end; ++i)
	        {
		        char c = str[i];

		        if(c == '"')
		        {
			        if(i == start || str[i-1] != '\\')
				        return i;
		        }
		        else if(c == '\n')
		        {
			        ++line;
			        charpos = 0;
		        }
		        else
			        ++charpos;
	        }

	        return NPOS;
        }

        public static string TokenName(Token token)
        {
            switch(token)
            {
                case Token.None:
                    return "none";
                case Token.Item:
                    return "item";
                case Token.Keyword:
                    return "keyword";
                case Token.Symbol:
                    return "symbol";
                case Token.Int:
                    return "int";
                case Token.Float:
                    return "float";
                case Token.String:
                    return "string";
                case Token.Eof:
                    return "end of file";
                case Token.Eol:
                    return "end of line";
                default:
                    return "unknown";
            }
        }

        public string GetCurrent()
        {
            switch(token)
            {
                case Token.None:
                    return "none";
                case Token.Item:
                    return string.Format("item \"{0}\"", item);
                case Token.Keyword:
                    return string.Format("keyword \"{0}\" ({1}, {2})", item, keyword.id, keyword.group);
                case Token.Symbol:
                    return string.Format("symbol '{0}'", symbol);
                case Token.Int:
                    return string.Format("int {0}", _int);
                case Token.Float:
                    return string.Format("float {0}", _float);
                case Token.String:
                    return string.Format("string \"{0}\"", item);
                case Token.Eof:
                    return "end of file";
                case Token.Eol:
                    return "end of line";
                default:
                    return "unknown";
            }
        }

        public string FormatExpected(Token _token, object what)
        {
            switch(_token)
            {
                case Token.Symbol:
                    return string.Format("symbol '{0}'", (char)what);
                case Token.KeywordGroup:
                    return string.Format("keyword group {0}", (int)what);
                case Token.KeywordGroupType:
                    return string.Format("keyword group '{0}'", ((Type)what).ToString());
                default:
                    return TokenName(_token);
            }
        }

        public void Unexpected(Token _token)
        {
            throw new Exception(string.Format("({0},{1}) Unexpected {2}, expecting {3}.", line+1, charpos+1, GetCurrent(),
                TokenName(_token)));
        }

        public void Unexpected(Token _token, object what)
        {
            throw new Exception(string.Format("({0},{1}) Unexpected {2}, expecting {3}.", line+1, charpos+1, GetCurrent(),
                FormatExpected(_token, what)));
        }

        //=======================================================================================================================
        public void AddKeyword(string name, int id, int group=-1)
        {
            Keyword k = new Keyword
            {
                name = name,
                id = id,
                group = group
            };
            keywords[name] = k;
        }

        public void AddKeywords<T>(TupleList<string,T> list) where T : struct, IConvertible
        {
            int group = typeof(T).GetHashCode();
            foreach(var item in list)
                AddKeyword(item.Item1, Convert.ToInt32(item.Item2), group);
        }

        //=======================================================================================================================
        public bool IsToken(Token _token)
        {
            return token == _token;
        }

        public bool IsEof()
        {
            return IsToken(Token.Eof);
        }

        public bool IsSymbol()
        {
            return IsToken(Token.Symbol);
        }

        public bool IsSymbol(char c)
        {
            return IsSymbol() && symbol == c;
        }

        public bool IsKeyword()
        {
            return IsToken(Token.Keyword);
        }

        public bool IsKeyword(int id)
        {
            return IsKeyword() && keyword.id == id;
        }

        public bool IsKeyword(int id, int group)
        {
            return IsKeyword() && keyword.id == id && keyword.group == group;
        }

        public bool IsKeywordGroup(int group)
        {
            return IsKeyword() && keyword.group == group;
        }

        public bool IsItem()
        {
            return IsToken(Token.Item);
        }

        public bool IsString()
        {
            return IsToken(Token.String);
        }

        public bool IsInt()
        {
            return IsToken(Token.Int);
        }

        public bool IsFloat()
        {
            return IsToken(Token.Float);
        }

        public bool IsNumber()
        {
            return IsInt() || IsFloat();
        }

        //=======================================================================================================================
        public void AssertSymbol()
        {
            if (!IsSymbol())
                Unexpected(Token.Symbol);
        }

        public void AssertSymbol(char c)
        {
            if (!IsSymbol(c))
                Unexpected(Token.Symbol, c);
        }

        public void AssertItem()
        {
            if (!IsItem())
                Unexpected(Token.Item);
        }

        public void AssertKeyword()
        {
            if (!IsKeyword())
                Unexpected(Token.Keyword);
        }

        public void AssertKeywordGroup(int group)
        {
            if (!IsKeywordGroup(group))
                Unexpected(Token.KeywordGroup, group);
        }

        public void AssertKeywordGroup(Type group_type)
        {
            int group = group_type.GetHashCode();
            if (!IsKeywordGroup(group))
                Unexpected(Token.KeywordGroupType, group_type);
        }

        public void AssertString()
        {
            if (!IsString())
                Unexpected(Token.String);
        }

        public void AssertInt()
        {
            if (!IsInt())
                Unexpected(Token.Int);
        }

        public void AssertFloat()
        {
            if (!IsFloat())
                Unexpected(Token.Float);
        }

        public void AssertNumber()
        {
            if (!IsNumber())
                Unexpected(Token.Number);
        }
        
        //=======================================================================================================================
        public string GetItem()
        {
            AssertItem();
            return item;
        }

        public string GetKeywordName()
        {
            AssertKeyword();
            return keyword.name;
        }

        public T GetKeyword<T>() where T : struct, IConvertible
        {
            AssertKeywordGroup(typeof(T));
            return (T)(object)keyword.id;
        }

        public string GetString()
        {
            AssertString();
            return item;
        }

        public char GetSymbol()
        {
            AssertSymbol();
            return symbol;
        }

        public int GetInt()
        {
            AssertInt();
            return _int;
        }

        public float GetFloat()
        {
            AssertFloat();
            return _float;
        }

        public int GetNumberInt()
        {
            AssertNumber();
            return _int;
        }

        public float GetNumberFloat()
        {
            AssertNumber();
            return _float;
        }
    }
}
