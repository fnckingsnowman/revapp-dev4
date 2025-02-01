using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace RevoluteConfigApp.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Convert bool to Visibility (inverse logic)
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Convert Visibility back to bool (inverse logic)
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }
}