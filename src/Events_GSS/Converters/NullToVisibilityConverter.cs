using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Events_GSS.Converters
{
    // Shows element when value is null, hides it otherwise.
    // Used for ErrorMessage — only visible when there's an error.
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is string s && !string.IsNullOrEmpty(s)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
