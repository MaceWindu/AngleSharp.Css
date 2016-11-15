﻿namespace AngleSharp.Css.Dom
{
    using AngleSharp.Css.Converters;
    using static ValueConverters;

    /// <summary>
    /// Information can be found on MDN:
    /// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/stroke
    /// Gets the value that should be used for the stroke-width.
    /// </summary>
    sealed class CssStrokeProperty : CssProperty
	{
		#region Fields

		private static readonly IValueConverter StyleConverter = PaintConverter;

		#endregion

		#region ctor

		internal CssStrokeProperty()
			: base(PropertyNames.Stroke, PropertyFlags.Animatable)
		{
		}

		#endregion

		#region Properties

		internal override IValueConverter Converter
		{
			get { return StyleConverter; }
		}

		#endregion
	}
}
