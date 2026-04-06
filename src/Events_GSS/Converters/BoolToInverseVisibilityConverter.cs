using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Events_GSS.Converters
{
    // Inverts a bool and converts to Visibility.
    // Used to hide content while IsLoading is true.
    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
