using League_of_Legends_Counterpicks.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using System.Linq;

namespace League_of_Legends_Counterpicks.Converters
{
    class PlayingVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ObservableCollection<Comment> comments = (ObservableCollection<Comment>)value;
            return comments.Where(c => c.Page == PageEnum.CommentPage.Playing);

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
