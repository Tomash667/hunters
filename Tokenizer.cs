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
            Eol
        }
        
        [Flags]
        public enum Flags
        {
            JoinMinus = 1<<0,
            JoinDot = 1<<1,
            Unescape = 1<<2
        }

        const int NPOS = -1;

        Flags flags;
        Token token;
        string str, item;
        char symbol;
        int pos, line, charpos;

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
		        if(pos2 == NPOS)
		        {
			        // only whitespaces, end of file after minus
                    token = Token.Symbol;
                    symbol = '-';
                    item = "-";
			        return true;
		        }

		        c = str[pos2];
		        if(c >= '0' && c <= '9')
		        {
			        // negative number
			        pos = FindFirstOf(" \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\"", pos2);
			        if(pos2 == NPOS)
			        {
				        pos = str.Length;
				        item = str.Substring(pos2);
			        }
			        else
				        item = str.Substring(pos2, pos-pos2);

			        long val;
                    float valf;

                    Utils.StringToNumberResult result = Utils.StringToNumber(item, out val, out valf);
			        int co = StringToNumber(token.c_str(), val, _float);
			        _int = -(int)val;
			        _uint = 0;
			        _float = -_float;
			        if(val > UINT_MAX)
				        WARN(Format("Tokenizer: Too big number %I64, stored as int(%d) and uint(%u).", val, _int, _uint));
			        if(co == 2)
				        type = T_FLOAT;
			        else if(co == 1)
				        type = T_INT;
			        else
				        type = T_ITEM;
			        return true;
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
			        return true;
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
		        // coś znaleziono, znajdź koniec tego czegość
		        bool ignore_dot = false;
		        if((c >= '0' && c <= '9') || IS_SET(flags, F_JOIN_DOT))
			        ignore_dot = true;
		        pos = FindFirstOf(ignore_dot ? " \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\"" : " \t\n\r,/;'\\[]`<>?:|{}=~!@#$%^&*()+-\".", pos2);
		        if(pos2 == string::npos)
		        {
			        pos = str->length();
			        token = str->substr(pos2);
		        }
		        else
			        token = str->substr(pos2, pos-pos2);

		        // czy to liczba?
		        if(c >= '0' && c <= '9')
		        {
			        __int64 val;
			        int co = StringToNumber(token.c_str(), val, _float);
			        _int = (int)val;
			        _uint = (uint)val;
			        if(val > UINT_MAX)
				        WARN(Format("Tokenizer: Too big number %I64, stored as int(%d) and uint(%u).", val, _int, _uint));
			        if(co == 2)
				        type = T_FLOAT;
			        else if(co == 1)
				        type = T_INT;
			        else
				        type = T_ITEM;
		        }
		        else
		        {
			        // czy to słowo kluczowe
			        for(uint i=0; i<keywords.size(); ++i)
			        {
				        if(token == keywords[i].name)
				        {
					        type = T_KEYWORD;
					        _int = i;
					        return true;
				        }
			        }

			        // zwykły tekst
			        type = T_ITEM;
		        }
	        }

	        return true;
        }

        public string GetLineItem()
        {
            return "";
        }

        int FindFirstOf(string match, int start)
        {
            return 0;
        }

        int FindFirstNotOf(string match, int start)
        {
            return 0;
        }

        int FindFirstOfStr(string match, int start)
        {
            return 0;
        }

        int FindEndOfQuote(int start)
        {
            return 0;
        }

        public string GetCurrent()
        {
            switch(token)
            {
                case Token.Symbol:
                    return string.Format("symbol '{0}'", symbol);
                default:
                    return "unknown";
            }
        }

        public string FormatExpected(Token _token, int what)
        {
            switch(_token)
            {
                case Token.Symbol:
                    if (what != 0)
                        return string.Format("symbol '{0}'", (char)what);
                    else
                        return "symbol";
                default:
                    return "unknown";
            }
        }

        public void Unexpected(Token _token, int what=0)
        {
            throw new Exception(string.Format("Unexpected {0}, expecting {1}.", GetCurrent(), FormatExpected(_token, what)));
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

        public bool IsItem()
        {
            return IsToken(Token.Item);
        }

        //=======================================================================================================================
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

        //=======================================================================================================================
        public string MustGetItem()
        {
            AssertItem();
            return item;
        }
    }
}
