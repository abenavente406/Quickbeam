﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModernIde.Helpers.Converters
{
    [ValueConversion(typeof (bool), typeof (Visibility))]
    public class TagBookmarkVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool outputBoolean = false;
            if (value != null)
                outputBoolean = (bool) value;

            if (!App.ModernIdeStorage.ModernIdeSettings.HalomapOnlyShowBookmarkedTags)
                return Visibility.Visible;

            return (outputBoolean)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}