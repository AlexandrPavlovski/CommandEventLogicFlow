using Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace VisualStudioExtension.Converters
{
    class InstantiationToMenuItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case HandlerInfo _:
                    return null;

                case InstantiationInfo instantiationInfo:

                    return instantiationInfo.ToString();

                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
