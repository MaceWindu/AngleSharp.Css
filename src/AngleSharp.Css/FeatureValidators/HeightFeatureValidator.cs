﻿namespace AngleSharp.Css.FeatureValidators
{
    using AngleSharp.Css.Converters;
    using AngleSharp.Css.Dom;
    using System;
    using static ValueConverters;

    sealed class HeightFeatureValidator : IFeatureValidator
    {
        #region Methods

        public Boolean Validate(IMediaFeature feature, IRenderDevice device)
        {
            var length = LengthConverter.Convert(feature.Value);

            if (length != null)
            {
                var desired = length.AsNumber();
                var available = (Single)device.ViewPortHeight;

                if (feature.IsMaximum)
                {
                    return available <= desired;
                }
                else if (feature.IsMinimum)
                {
                    return available >= desired;
                }

                return desired == available;
            }

            return false;
        }

        #endregion
    }
}
