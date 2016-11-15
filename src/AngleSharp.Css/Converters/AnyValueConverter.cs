﻿namespace AngleSharp.Css.Converters
{
    using AngleSharp.Css.Dom;
    using AngleSharp.Text;

    sealed class AnyValueConverter : IValueConverter
    {
        public ICssValue Convert(StringSource source)
        {
            var value = source.Content;
            source.Next(value.Length);
            return new BaseValue(value);
        }
    }
}
