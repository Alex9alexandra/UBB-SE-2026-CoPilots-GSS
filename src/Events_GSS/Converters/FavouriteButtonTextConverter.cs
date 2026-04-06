using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.UI.Xaml.Data;

namespace Events_GSS.Converters
{
    // Returns "Unfavourite" or "Favourite" based on current favourite state.
    public class FavouriteButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool b && b ? "Unfavourite" : "Favourite";

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
