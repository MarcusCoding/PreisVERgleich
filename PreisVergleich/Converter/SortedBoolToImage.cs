using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PreisVergleich.Converter
{
    public class SortBoolToImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return $"/PreisVergleich;component/Resources/filtering/filter_{parameter.ToString()}.png";
            }

            if((bool)value == true)
            {
                return $"/PreisVergleich;component/Resources/filtering/filter_{parameter.ToString()}_down.png";
            }
            else
            {
                return $"/PreisVergleich;component/Resources/filtering/filter_{parameter.ToString()}_up.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
