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
    class CounterCommentMatchupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Counter counter = value as Counter;
            if (counter.Page == PageEnum.ChampionPage.Counter) return counter.ChampionFeedbackName + " versus " + counter.Name;
            else return counter.ChampionFeedbackName + " with " + counter.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
