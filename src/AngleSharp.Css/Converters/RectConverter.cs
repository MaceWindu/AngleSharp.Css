﻿namespace AngleSharp.Css.Converters
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Css.Extensions;
    using AngleSharp.Css.Parser;
    using AngleSharp.Css.Values;
    using AngleSharp.Text;
    using System;
    using System.IO;

    sealed class RectConverter : IValueConverter
    {
        public ICssValue Convert(StringSource source)
        {
            if (source.IsFunction(FunctionNames.Rect))
            {
                var top = source.ToLength();
                SkipIfComma(source);
                var right = source.ToLength();
                SkipIfComma(source);
                var bottom = source.ToLength();
                SkipIfComma(source);
                var left = source.ToLength();
                var f = source.SkipSpacesAndComments();

                if (top.HasValue && right.HasValue && bottom.HasValue && left.HasValue && f == Symbols.RoundBracketClose)
                {
                    var shape = new Shape(top.Value, right.Value, bottom.Value, left.Value);
                    source.SkipCurrentAndSpaces();
                    return new RectValue(shape);
                }
            }

            return null;
        }

        private void SkipIfComma(StringSource source)
        {
            if (source.SkipSpacesAndComments() == Symbols.Comma)
            {
                source.SkipCurrentAndSpaces();
            }
        }

        sealed class RectValue : ICssValue
        {
            private readonly Shape _shape;

            public RectValue(Shape shape)
            {
                _shape = shape;
            }

            public String CssText
            {
                get { return _shape.ToString(); }
            }

            public void ToCss(TextWriter writer, IStyleFormatter formatter)
            {
                writer.Write(CssText);
            }
        }
    }
}
