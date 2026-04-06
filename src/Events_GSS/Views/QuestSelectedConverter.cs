using System;
using Microsoft.UI.Xaml.Data;
using Events_GSS.Data.Models;
using Events_GSS.ViewModels;

namespace Events_GSS.Views
{
    public class QuestSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Quest quest && parameter is CreateEventViewModel vm)
            {
                return vm.IsQuestSelected(quest);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
