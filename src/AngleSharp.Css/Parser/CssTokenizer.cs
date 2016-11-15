﻿namespace AngleSharp.Css.Parser
{
    using AngleSharp.Common;
    using AngleSharp.Css.Dom.Events;
    using AngleSharp.Css.Parser.Tokens;
    using AngleSharp.Text;
    using System;
    using System.Globalization;

    /// <summary>
    /// The CSS tokenizer.
    /// See http://dev.w3.org/csswg/css-syntax/#tokenization for more details.
    /// </summary>
    sealed class CssTokenizer : BaseTokenizer
	{
		#region Fields

        private TextPosition _position;

        #endregion

        #region Events

        /// <summary>
        /// Fired in case of a parse error.
        /// </summary>
        public event EventHandler<CssErrorEvent> Error;

        #endregion

        #region ctor

        /// <summary>
        /// CSS Tokenization
        /// </summary>
        /// <param name="source">The source code manager.</param>
        public CssTokenizer(TextSource source)
            : base(source)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the next available token.
        /// </summary>
        /// <returns>The next available token.</returns>
        public CssToken Get()
        {
            var current = GetNext();
            _position = GetCurrentPosition();
            return Data(current);
        }

        /// <summary>
        /// Gets the trimmed content until one of the chars is hit.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        public String ContentTo(Char a, Char b = Symbols.EndOfFile)
        {
            var current = SkipSpaces();
            var length = 0;

            while (!current.IsOneOf(a, b, Symbols.EndOfFile))
            {
                StringBuffer.Append(current);
                length = current.IsSpaceCharacter() ? length + 1 : 0;
                current = GetNext();
            }

            Back();
            StringBuffer.Remove(StringBuffer.Length - length, length);
            return FlushBuffer();
        }

        internal void RaiseErrorOccurred(CssParseError error, TextPosition position)
        {
            Error?.Invoke(this, new CssErrorEvent(error, position));
        }

        #endregion

        #region States

        /// <summary>
        /// 4.4.1. Data state
        /// </summary>
        private CssToken Data(Char current)
        {
            _position = GetCurrentPosition();

            switch (current)
            {
                case Symbols.FormFeed:
                case Symbols.LineFeed:
                case Symbols.CarriageReturn:
                case Symbols.Tab:
                case Symbols.Space:
                    return NewWhitespace(current);

                case Symbols.DoubleQuote:
                    return StringDQ();

                case Symbols.Num:
                    return HashStart();

                case Symbols.Dollar:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.Ends);
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.SingleQuote:
                    return StringSQ();

                case Symbols.RoundBracketOpen:
                    return NewOpenRound();

                case Symbols.RoundBracketClose:
                    return NewCloseRound();

                case Symbols.Asterisk:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.InText);
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.Plus:
                {
                    var c1 = GetNext();

                    if (c1 != Symbols.EndOfFile)
                    {
                        var c2 = GetNext();
                        Back(2);

                        if (c1.IsDigit() || (c1 == Symbols.Dot && c2.IsDigit()))
                        {
                            return NumberStart(current);
                        }
                    }
                    else
                    {
                        Back();
                    }

                    return NewDelimiter(current);
                }

                case Symbols.Comma:
                    return NewComma();

                case Symbols.Dot:
                {
                    var c = GetNext();

                    if (c.IsDigit())
                    {
                        return NumberStart(GetPrevious());
                    }

                    return NewDelimiter(GetPrevious());
                }

                case Symbols.Minus:
                {
                    var c1 = GetNext();

                    if (c1 != Symbols.EndOfFile)
                    {
                        var c2 = GetNext();
                        Back(2);

                        if (c1.IsDigit() || (c1 == Symbols.Dot && c2.IsDigit()))
                        {
                            return NumberStart(current);
                        }
                        else if (c1.IsNameStart())
                        {
                            return IdentStart(current);
                        }
                        else if (c1 == Symbols.ReverseSolidus && !c2.IsLineBreak() && c2 != Symbols.EndOfFile)
                        {
                            return IdentStart(current);
                        }
                        else if (c1 == Symbols.Minus && c2 == Symbols.GreaterThan)
                        {
                            Advance(2);
                            return NewCloseComment();
                        }
                    }
                    else
                    {
                        Back();
                    }

                    return NewDelimiter(current);
                }

                case Symbols.Solidus:
                    current = GetNext();

                    if (current == Symbols.Asterisk)
                    {
                        return Comment();
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.ReverseSolidus:
                    current = GetNext();

                    if (current.IsLineBreak())
                    {
                        RaiseErrorOccurred(CssParseError.LineBreakUnexpected);
                        return NewDelimiter(GetPrevious());
                    }
                    else if (current == Symbols.EndOfFile)
                    {
                        RaiseErrorOccurred(CssParseError.EOF);
                        return NewDelimiter(GetPrevious());
                    }

                    return IdentStart(GetPrevious());
                case Symbols.Colon:
                    return NewColon();

                case Symbols.Semicolon:
                    return NewSemicolon();

                case Symbols.LessThan:
                    current = GetNext();

                    if (current == Symbols.ExclamationMark)
                    {
                        current = GetNext();

                        if (current == Symbols.Minus)
                        {
                            current = GetNext();

                            if (current == Symbols.Minus)
                            {
                                return NewOpenComment();
                            }

                            current = GetPrevious();
                        }

                        current = GetPrevious();
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.At:
                    return AtKeywordStart();

                case Symbols.SquareBracketOpen:
                    return NewOpenSquare();

                case Symbols.SquareBracketClose:
                    return NewCloseSquare();

                case Symbols.Accent:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.Begins);
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.CurlyBracketOpen:
                    return NewOpenCurly();

                case Symbols.CurlyBracketClose:
                    return NewCloseCurly();

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return NumberStart(current);

                case 'U':
                case 'u':
                    current = GetNext();

                    if (current == Symbols.Plus)
                    {
                        current = GetNext();

                        if (current.IsHex() || current == Symbols.QuestionMark)
                        {
                            return UnicodeRange(current);
                        }

                        current = GetPrevious();
                    }

                    return IdentStart(GetPrevious());

                case Symbols.Pipe:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.InToken);
                    }
                    else if (current == Symbols.Pipe)
                    {
                        return NewColumn();
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.Tilde:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.InList);
                    }

                    return NewDelimiter(GetPrevious());

                case Symbols.EndOfFile:
                    return NewEof();

                case Symbols.ExclamationMark:
                    current = GetNext();

                    if (current == Symbols.Equality)
                    {
                        return NewMatch(CombinatorSymbols.Unlike);
                    }

                    return NewDelimiter(GetPrevious());

                default:
                    if (current.IsNameStart())
                    {
                        return IdentStart(current);
                    }

                    return NewDelimiter(current);
            }
        }

        /// <summary>
        /// 4.4.2. Double quoted string state
        /// </summary>
        private CssToken StringDQ()
        {
            while (true)
            {
                var current = GetNext();

                switch (current)
                {
                    case Symbols.DoubleQuote:
                    case Symbols.EndOfFile:
                        return NewString(FlushBuffer(), Symbols.DoubleQuote);

                    case Symbols.FormFeed:
                    case Symbols.LineFeed:
                        RaiseErrorOccurred(CssParseError.LineBreakUnexpected);
                        Back();
                        return NewString(FlushBuffer(), Symbols.DoubleQuote, bad: true);

                    case Symbols.ReverseSolidus:
                        current = GetNext();

                        if (current.IsLineBreak())
                        {
                            StringBuffer.AppendLine();
                        }
                        else if (current != Symbols.EndOfFile)
                        {
                            StringBuffer.Append(ConsumeEscape(current));
                        }
                        else
                        {
                            RaiseErrorOccurred(CssParseError.EOF);
                            Back();
                            return NewString(FlushBuffer(), Symbols.DoubleQuote, bad: true);
                        }

                        break;

                    default:
                        StringBuffer.Append(current);
                        break;
                }
            }
        }

        /// <summary>
        /// 4.4.3. Single quoted string state
        /// </summary>
        private CssToken StringSQ()
        {
            while (true)
            {
                var current = GetNext();

                switch (current)
                {
                    case Symbols.SingleQuote:
                    case Symbols.EndOfFile:
                        return NewString(FlushBuffer(), Symbols.SingleQuote);

                    case Symbols.FormFeed:
                    case Symbols.LineFeed:
                        RaiseErrorOccurred(CssParseError.LineBreakUnexpected);
                        Back();
                        return NewString(FlushBuffer(), Symbols.SingleQuote, bad: true);

                    case Symbols.ReverseSolidus:
                        current = GetNext();

                        if (current.IsLineBreak())
                        {
                            StringBuffer.AppendLine();
                        }
                        else if (current != Symbols.EndOfFile)
                        {
                            StringBuffer.Append(ConsumeEscape(current));
                        }
                        else
                        {
                            RaiseErrorOccurred(CssParseError.EOF);
                            Back();
                            return NewString(FlushBuffer(), Symbols.SingleQuote, bad: true);
                        }

                        break;

                    default:
                        StringBuffer.Append(current);
                        break;
                }
            }
        }

        /// <summary>
        /// Color literal state.
        /// </summary>
        private CssToken ColorLiteral()
        {
            var current = GetNext();

            while (current.IsHex())
            {
                StringBuffer.Append(current);
                current = GetNext();
            }

            Back();
            return NewColor(FlushBuffer());
        }

        /// <summary>
        /// 4.4.4. Hash state
        /// </summary>
        private CssToken HashStart()
        {
            var current = GetNext();

            if (current.IsNameStart())
            {
                StringBuffer.Append(current);
                return HashRest();
            }
            else if (IsValidEscape(current))
            {
                current = GetNext();
                StringBuffer.Append(ConsumeEscape(current));
                return HashRest();
            }
            else if (current == Symbols.ReverseSolidus)
            {
                RaiseErrorOccurred(CssParseError.InvalidCharacter);
                Back();
                return NewDelimiter(Symbols.Num);
            }
            else
            {
                Back();
                return NewDelimiter(Symbols.Num);
            }
        }

        /// <summary>
        /// 4.4.5. Hash-rest state
        /// </summary>
        private CssToken HashRest()
        {
            while (true)
            {
                var current = GetNext();

                if (current.IsName())
                {
                    StringBuffer.Append(current);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else if (current == Symbols.ReverseSolidus)
                {
                    RaiseErrorOccurred(CssParseError.InvalidCharacter);
                    Back();
                    return NewHash(FlushBuffer());
                }
                else
                {
                    Back();
                    return NewHash(FlushBuffer());
                }
            }
        }

        /// <summary>
        /// 4.4.6. Comment state
        /// </summary>
        private CssToken Comment()
        {
            var current = GetNext();

            while (current != Symbols.EndOfFile)
            {
                if (current == Symbols.Asterisk)
                {
                    current = GetNext();

                    if (current == Symbols.Solidus)
                    {
                        return NewComment(FlushBuffer());
                    }

                    StringBuffer.Append(Symbols.Asterisk);
                }
                else
                {
                    StringBuffer.Append(current);
                    current = GetNext();
                }
            }

            RaiseErrorOccurred(CssParseError.EOF);
            return NewComment(FlushBuffer(), bad: true);
        }

        /// <summary>
        /// 4.4.7. At-keyword state
        /// </summary>
        private CssToken AtKeywordStart()
        {
            var current = GetNext();

            if (current == Symbols.Minus)
            {
                current = GetNext();

                if (current.IsNameStart() || IsValidEscape(current))
                {
                    StringBuffer.Append(Symbols.Minus);
                    return AtKeywordRest(current);
                }

                Back(2);
                return NewDelimiter(Symbols.At);
            }
            else if (current.IsNameStart())
            {
                StringBuffer.Append(current);
                return AtKeywordRest(GetNext());
            }
            else if (IsValidEscape(current))
            {
                current = GetNext();
                StringBuffer.Append(ConsumeEscape(current));
                return AtKeywordRest(GetNext());
            }
            else
            {
                Back();
                return NewDelimiter(Symbols.At);
            }
        }

        /// <summary>
        /// 4.4.8. At-keyword-rest state
        /// </summary>
        private CssToken AtKeywordRest(Char current)
        {
            while (true)
            {
                if (current.IsName())
                {
                    StringBuffer.Append(current);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else
                {
                    Back();
                    return NewAtKeyword(FlushBuffer());
                }

                current = GetNext();
            }
        }

        /// <summary>
        /// 4.4.9. Ident state
        /// </summary>
        private CssToken IdentStart(Char current)
        {
            if (current == Symbols.Minus)
            {
                current = GetNext();

                if (current.IsNameStart() || IsValidEscape(current))
                {
                    StringBuffer.Append(Symbols.Minus);
                    return IdentRest(current);
                }

                Back();
                return NewDelimiter(Symbols.Minus);
            }
            else if (current.IsNameStart())
            {
                StringBuffer.Append(current);
                return IdentRest(GetNext());
            }
            else if (current == Symbols.ReverseSolidus && IsValidEscape(current))
            {
                current = GetNext();
                StringBuffer.Append(ConsumeEscape(current));
                return IdentRest(GetNext());
            }

            return Data(current);
        }

        /// <summary>
        /// 4.4.10. Ident-rest state
        /// </summary>
        private CssToken IdentRest(Char current)
        {
            while (true)
            {
                if (current.IsName())
                {
                    StringBuffer.Append(current);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else if (current == Symbols.RoundBracketOpen)
                {
                    var name = FlushBuffer();

                    if (name.Isi(FunctionNames.Url) || 
                        name.Isi(FunctionNames.Domain) ||
                        name.Isi(FunctionNames.UrlPrefix) ||
                        name.Isi(FunctionNames.Regexp))
                    {
                        return UrlStart(name);
                    }
                    
                    return NewFunction(name);
                }
                else
                {
                    Back();
                    return NewIdent(FlushBuffer());
                }

                current = GetNext();
            }
        }

        /// <summary>
        /// 4.4.11. Transform-function-whitespace state
        /// </summary>
        private CssToken TransformFunctionWhitespace(Char current)
        {
            while (true)
            {
                current = GetNext();

                if (current == Symbols.RoundBracketOpen)
                {
                    Back();
                    return NewFunction(FlushBuffer());
                }
                else if (!current.IsSpaceCharacter())
                {
                    Back(2);
                    return NewIdent(FlushBuffer());
                }
            }
        }

        /// <summary>
        /// 4.4.12. Number state
        /// </summary>
        private CssToken NumberStart(Char current)
        {
            while (true)
            {
                if (current.IsOneOf(Symbols.Plus, Symbols.Minus))
                {
                    StringBuffer.Append(current);
                    current = GetNext();

                    if (current == Symbols.Dot)
                    {
                        StringBuffer.Append(current);
                        StringBuffer.Append(GetNext());
                        return NumberFraction();
                    }

                    StringBuffer.Append(current);
                    return NumberRest();
                }
                else if (current == Symbols.Dot)
                {
                    StringBuffer.Append(current);
                    StringBuffer.Append(GetNext());
                    return NumberFraction();
                }
                else if (current.IsDigit())
                {
                    StringBuffer.Append(current);
                    return NumberRest();
                }

                current = GetNext();
            }
        }

        /// <summary>
        /// 4.4.13. Number-rest state
        /// </summary>
        private CssToken NumberRest()
        {
            var current = GetNext();

            while (true)
            {

                if (current.IsDigit())
                {
                    StringBuffer.Append(current);
                }
                else if (current.IsNameStart())
                {
                    var number = FlushBuffer();
                    StringBuffer.Append(current);
                    return Dimension(number);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    var number = FlushBuffer();
                    StringBuffer.Append(ConsumeEscape(current));
                    return Dimension(number);
                }
                else
                {
                    break;
                }

                current = GetNext();
            }

            switch (current)
            {
                case Symbols.Dot:
                    current = GetNext();

                    if (current.IsDigit())
                    {
                        StringBuffer.Append(Symbols.Dot).Append(current);
                        return NumberFraction();
                    }

                    Back();
                    return NewNumber(FlushBuffer());

                case '%':
                    return NewPercentage(FlushBuffer());

                case 'e':
                case 'E':
                    return NumberExponential(current);

                case Symbols.Minus:
                    return NumberDash();

                default:
                    Back();
                    return NewNumber(FlushBuffer());
            }
        }

        /// <summary>
        /// 4.4.14. Number-fraction state
        /// </summary>
        private CssToken NumberFraction()
        {
            var current = GetNext();

            while (true)
            {
                if (current.IsDigit())
                {
                    StringBuffer.Append(current);
                }
                else if (current.IsNameStart())
                {
                    var number = FlushBuffer();
                    StringBuffer.Append(current);
                    return Dimension(number);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    var number = FlushBuffer();
                    StringBuffer.Append(ConsumeEscape(current));
                    return Dimension(number);
                }
                else
                {
                    break;
                }

                current = GetNext();
            }

            switch (current)
            {
                case 'e':
                case 'E':
                    return NumberExponential(current);

                case '%':
                    return NewPercentage(FlushBuffer());

                case Symbols.Minus:
                    return NumberDash();

                default:
                    Back();
                    return NewNumber(FlushBuffer());
            }
        }

        /// <summary>
        /// 4.4.15. Dimension state
        /// </summary>
        private CssToken Dimension(String number)
        {
            while (true)
            {
                var current = GetNext();

                if (current.IsLetter())
                {
                    StringBuffer.Append(current);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else
                {
                    Back();
                    return NewDimension(number, FlushBuffer());
                }
            }
        }

        /// <summary>
        /// 4.4.16. SciNotation state
        /// </summary>
        private CssToken SciNotation()
        {
            while (true)
            {
                var current = GetNext();

                if (current.IsDigit())
                {
                    StringBuffer.Append(current);
                }
                else
                {
                    Back();
                    return NewNumber(FlushBuffer());
                }
            }
        }

        /// <summary>
        /// 4.4.17. URL state
        /// </summary>
        private CssToken UrlStart(String functionName)
        {
            var current = SkipSpaces();

            switch (current)
            {
                case Symbols.EndOfFile:
                    RaiseErrorOccurred(CssParseError.EOF);
                    return NewUrl(functionName, String.Empty, bad: true);

                case Symbols.DoubleQuote:
                    return UrlDQ(functionName);

                case Symbols.SingleQuote:
                    return UrlSQ(functionName);

                case Symbols.RoundBracketClose:
                    return NewUrl(functionName, String.Empty, bad: false);

                default:
                    return UrlUQ(current, functionName);
            }
        }

        /// <summary>
        /// 4.4.18. URL-double-quoted state
        /// </summary>
        private CssToken UrlDQ(String functionName)
        {
            while (true)
            {
                var current = GetNext();

                if (current.IsLineBreak())
                {
                    RaiseErrorOccurred(CssParseError.LineBreakUnexpected);
                    return UrlBad(functionName);
                }
                else if (Symbols.EndOfFile == current)
                {
                    return NewUrl(functionName, FlushBuffer());
                }
                else if (current == Symbols.DoubleQuote)
                {
                    return UrlEnd(functionName);
                }
                else if (current != Symbols.ReverseSolidus)
                {
                    StringBuffer.Append(current);
                }
                else
                {
                    current = GetNext();

                    if (current == Symbols.EndOfFile)
                    {
                        Back(2);
                        RaiseErrorOccurred(CssParseError.EOF);
                        return NewUrl(functionName, FlushBuffer(), bad: true);
                    }
                    else if (current.IsLineBreak())
                    {
                        StringBuffer.AppendLine();
                    }
                    else
                    {
                        StringBuffer.Append(ConsumeEscape(current));
                    }
                }
            }
        }

        /// <summary>
        /// 4.4.19. URL-single-quoted state
        /// </summary>
        private CssToken UrlSQ(String functionName)
        {
            while (true)
            {
                var current = GetNext();

                if (current.IsLineBreak())
                {
                    RaiseErrorOccurred(CssParseError.LineBreakUnexpected);
                    return UrlBad(functionName);
                }
                else if (current == Symbols.EndOfFile)
                {
                    return NewUrl(functionName, FlushBuffer());
                }
                else if (current == Symbols.SingleQuote)
                {
                    return UrlEnd(functionName);
                }
                else if (current != Symbols.ReverseSolidus)
                {
                    StringBuffer.Append(current);
                }
                else
                {
                    current = GetNext();

                    if (current == Symbols.EndOfFile)
                    {
                        Back(2);
                        RaiseErrorOccurred(CssParseError.EOF);
                        return NewUrl(functionName, FlushBuffer(), bad: true);
                    }
                    else if (current.IsLineBreak())
                    {
                        StringBuffer.AppendLine();
                    }
                    else
                    {
                        StringBuffer.Append(ConsumeEscape(current));
                    }
                }
            }
        }

        /// <summary>
        /// 4.4.21. URL-unquoted state
        /// </summary>
        private CssToken UrlUQ(Char current, String functionName)
        {
            while (true)
            {
                if (current.IsSpaceCharacter())
                {
                    return UrlEnd(functionName);
                }
                else if (current.IsOneOf(Symbols.RoundBracketClose, Symbols.EndOfFile))
                {
                    return NewUrl(functionName, FlushBuffer());
                }
                else if (current.IsOneOf(Symbols.DoubleQuote, Symbols.SingleQuote, Symbols.RoundBracketOpen) || current.IsNonPrintable())
                {
                    RaiseErrorOccurred(CssParseError.InvalidCharacter);
                    return UrlBad(functionName);
                }
                else if (current != Symbols.ReverseSolidus)
                {
                    StringBuffer.Append(current);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else
                {
                    RaiseErrorOccurred(CssParseError.InvalidCharacter);
                    return UrlBad(functionName);
                }

                current = GetNext();
            }
        }

        /// <summary>
        /// 4.4.20. URL-end state
        /// </summary>
        private CssToken UrlEnd(String functionName)
        {
            while (true)
            {
                var current = GetNext();

                if (current == Symbols.RoundBracketClose)
                {
                    return NewUrl(functionName, FlushBuffer());
                }
                else if (!current.IsSpaceCharacter())
                {
                    RaiseErrorOccurred(CssParseError.InvalidCharacter);
                    Back();
                    return UrlBad(functionName);
                }
            }
        }

        /// <summary>
        /// 4.4.22. Bad URL state
        /// </summary>
        private CssToken UrlBad(String functionName)
        {
            var current = Current;
            var curly = 0;
            var round = 1;

            while (current != Symbols.EndOfFile)
            {
                if (current == Symbols.Semicolon)
                {
                    Back();
                    return NewUrl(functionName, FlushBuffer(), true);
                }
                else if (current == Symbols.CurlyBracketClose && --curly == -1)
                {
                    Back();
                    return NewUrl(functionName, FlushBuffer(), true);
                }
                else if (current == Symbols.RoundBracketClose && --round == 0)
                {
                    return NewUrl(functionName, FlushBuffer(), true);
                }
                else if (IsValidEscape(current))
                {
                    current = GetNext();
                    StringBuffer.Append(ConsumeEscape(current));
                }
                else
                {
                    if (current == Symbols.RoundBracketOpen)
                    {
                        ++round;
                    }
                    else if (curly == Symbols.CurlyBracketOpen)
                    {
                        ++curly;
                    }

                    StringBuffer.Append(current);
                }

                current = GetNext();
            }

            RaiseErrorOccurred(CssParseError.EOF);
            return NewUrl(functionName, FlushBuffer(), bad: true);
        }

        /// <summary>
        /// 4.4.23. Unicode-range State
        /// </summary>
        private CssToken UnicodeRange(Char current)
        {
            for (var i = 0; i < 6 && current.IsHex(); i++)
            {
                StringBuffer.Append(current);
                current = GetNext();
            }

            if (StringBuffer.Length != 6)
            {
                for (var i = 0; i < 6 - StringBuffer.Length; i++)
                {
                    if (current != Symbols.QuestionMark)
                    {
                        current = GetPrevious();
                        break;
                    }

                    StringBuffer.Append(current);
                    current = GetNext();
                }

                return NewRange(FlushBuffer());
            }
            else if (current == Symbols.Minus)
            {
                current = GetNext();

                if (current.IsHex())
                {
                    var start = FlushBuffer();

                    for (var i = 0; i < 6; i++)
                    {
                        if (!current.IsHex())
                        {
                            current = GetPrevious();
                            break;
                        }

                        StringBuffer.Append(current);
                        current = GetNext();
                    }

                    var end = FlushBuffer();
                    return NewRange(start, end);
                }
                else
                {
                    Back(2);
                    return NewRange(FlushBuffer());
                }
            }
            else
            {
                Back();
                return NewRange(FlushBuffer());
            }
        }

        #endregion

        #region Tokens

        private CssToken NewMatch(String match)
        {
            return new CssToken(CssTokenType.Match, match) { Position = _position };
        }

        private CssToken NewColumn()
        {
            return new CssToken(CssTokenType.Column, CombinatorSymbols.Column) { Position = _position };
        }

        private CssToken NewCloseCurly()
        {
            return new CssToken(CssTokenType.CurlyBracketClose, "}") { Position = _position };
        }

        private CssToken NewOpenCurly()
        {
            return new CssToken(CssTokenType.CurlyBracketOpen, "{") { Position = _position };
        }

        private CssToken NewCloseSquare()
        {
            return new CssToken(CssTokenType.SquareBracketClose, "]") { Position = _position };
        }

        private CssToken NewOpenSquare()
        {
            return new CssToken(CssTokenType.SquareBracketOpen, "[") { Position = _position };
        }

        private CssToken NewOpenComment()
        {
            return new CssToken(CssTokenType.Cdo, "<!--") { Position = _position };
        }

        private CssToken NewSemicolon()
        {
            return new CssToken(CssTokenType.Semicolon, ";") { Position = _position };
        }

        private CssToken NewColon()
        {
            return new CssToken(CssTokenType.Colon, ":") { Position = _position };
        }

        private CssToken NewCloseComment()
        {
            return new CssToken(CssTokenType.Cdc, "-->") { Position = _position };
        }

        private CssToken NewComma()
        {
            return new CssToken(CssTokenType.Comma, ",") { Position = _position };
        }

        private CssToken NewCloseRound()
        {
            return new CssToken(CssTokenType.RoundBracketClose, ")") { Position = _position };
        }

        private CssToken NewOpenRound()
        {
            return new CssToken(CssTokenType.RoundBracketOpen, "(") { Position = _position };
        }

        private CssToken NewString(String value, Char quote, Boolean bad = false)
        {
            return new CssStringToken(value, quote, bad) { Position = _position };
        }

        private CssToken NewHash(String value)
        {
            return new CssToken(CssTokenType.Hash, value) { Position = _position };
        }

        private CssToken NewComment(String value, Boolean bad = false)
        {
            return new CssCommentToken(value, bad) { Position = _position };
        }

        private CssToken NewAtKeyword(String value)
        {
            return new CssToken(CssTokenType.AtKeyword, value) { Position = _position };
        }

        private CssToken NewIdent(String value)
        {
            return new CssToken(CssTokenType.Ident, value) { Position = _position };
        }

        private CssToken NewFunction(String value)
        {
            return new CssToken(CssTokenType.Function, value) { Position = _position };
        }

        private CssToken NewPercentage(String value)
        {
            return new CssUnitToken(CssTokenType.Percentage, value, "%") { Position = _position };
        }

        private CssToken NewDimension(String value, String unit)
        {
            return new CssUnitToken(CssTokenType.Dimension, value, unit) { Position = _position };
        }

        private CssToken NewUrl(String functionName, String data, Boolean bad = false)
        {
            return new CssUrlToken(functionName, data, bad) { Position = _position };
        }

        private CssToken NewRange(String range)
        {
            return new CssRangeToken(range) { Position = _position };
        }

        private CssToken NewRange(String start, String end)
        {
            return new CssRangeToken(start, end) { Position = _position };
        }

        private CssToken NewWhitespace(Char c)
        {
            return new CssToken(CssTokenType.Whitespace, c.ToString()) { Position = _position };
        }

        private CssToken NewNumber(String number)
        {
            return new CssNumberToken(number) { Position = _position };
        }

        private CssToken NewDelimiter(Char c)
        {
            return new CssToken(CssTokenType.Delim, c.ToString()) { Position = _position };
        }

        private CssToken NewColor(String text)
        {
            return new CssToken(CssTokenType.Color, text) { Position = _position };
        }

        private CssToken NewEof()
        {
            return new CssToken(CssTokenType.EndOfFile, String.Empty) { Position = _position };
        }

        #endregion

        #region Helpers

        private CssToken NumberExponential(Char letter)
        {
            var current = GetNext();

            if (current.IsDigit())
            {
                StringBuffer.Append(letter).Append(current);
                return SciNotation();
            }
            else if (current == Symbols.Plus || current == Symbols.Minus)
            {
                var op = current;
                current = GetNext();

                if (current.IsDigit())
                {
                    StringBuffer.Append(letter).Append(op).Append(current);
                    return SciNotation();
                }

                Back();
            }

            var number = FlushBuffer();
            StringBuffer.Append(letter);
            Back();
            return Dimension(number);
        }

        private CssToken NumberDash()
        {
            var current = GetNext();

            if (current.IsNameStart())
            {
                var number = FlushBuffer();
                StringBuffer.Append(Symbols.Minus).Append(current);
                return Dimension(number);
            }
            else if (IsValidEscape(current))
            {
                current = GetNext();
                var number = FlushBuffer();
                StringBuffer.Append(Symbols.Minus).Append(ConsumeEscape(current));
                return Dimension(number);
            }
            else
            {
                Back(2);
                return NewNumber(FlushBuffer());
            }
        }

        private String ConsumeEscape(Char current)
        {
            if (current.IsHex())
            {
                var isHex = true;
                var escape = new Char[6];
                var length = 0;

                while (isHex && length < escape.Length)
                {
                    escape[length++] = current;
                    current = GetNext();
                    isHex = current.IsHex();
                }

                if (!current.IsSpaceCharacter())
                {
                    Back();
                }

                var code = Int32.Parse(new String(escape, 0, length), NumberStyles.HexNumber);

                if (!code.IsInvalid())
                {
                    return code.ConvertFromUtf32();
                }

                current = Symbols.Replacement;
            }

            return current.ToString();
        }

        private Boolean IsValidEscape(Char current)
        {
            if (current == Symbols.ReverseSolidus)
            {
                current = GetNext();
                Back();

                return current != Symbols.EndOfFile && !current.IsLineBreak();
            }
                
            return false;
        }

        private void RaiseErrorOccurred(CssParseError code)
        {
            RaiseErrorOccurred(code, GetCurrentPosition());
        }

        #endregion
    }
}
