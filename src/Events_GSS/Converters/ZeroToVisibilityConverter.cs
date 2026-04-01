using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Events_GSS.Converters
{
    // Shows element when an integer value is zero.
    // Used for the "No events found" empty state message.
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
