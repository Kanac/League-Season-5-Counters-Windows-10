using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace League_of_Legends_Counterpicks.Converters
{
    public class NameToImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var counter = value as string;
            var uri = "ms-appx:///Assets/" + counter + "_Square_0.png";
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
