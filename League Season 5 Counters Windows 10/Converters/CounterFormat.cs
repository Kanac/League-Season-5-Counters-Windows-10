using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace League_of_Legends_Counterpicks.Converters
{
    class CounterFormat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            String champName = value as String;
            return "Countering " + champName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
