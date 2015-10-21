using League_of_Legends_Counterpicks.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using System.Linq;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace League_of_Legends_Counterpicks.Converters
{
    class CounterCommentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Counter counter = value as Counter;
            if (counter.CounterComments.Count > 0) return new SolidColorBrush(Colors.Yellow);
            else return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
