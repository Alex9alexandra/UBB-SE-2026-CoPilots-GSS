using System;
using System.Collections.Generic;
using System.Text;

using Events_GSS.Data.Models;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Events_GSS.Converters
{
    // Returns Visible if the event starts within 48 hours and hasn't started yet (req 5.6).
    public class UpcomingEventConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not Event ev) return Visibility.Collapsed;

            var now = DateTime.Now;
            var hoursUntilStart = (ev.StartDateTime - now).TotalHours;

            // Show badge if event starts within 48 hours but hasn't started yet
            return hoursUntilStart > 0 && hoursUntilStart <= 48
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
