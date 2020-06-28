using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PreisVergleich.Converter
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value != null)
            {
                switch (value.ToString())
                {
                    case "günstiger":
                        return Brushes.Green;
                    case "1-2€ darüber":
                        return Brushes.Yellow;
                    case "3€ oder mehr darüber":
                        return Brushes.IndianRed;
                    default:
                        return DependencyProperty.UnsetValue;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
