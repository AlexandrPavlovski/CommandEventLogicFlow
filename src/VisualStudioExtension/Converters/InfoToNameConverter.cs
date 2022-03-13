using Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using VisualStudioExtension.ViewModels;

namespace VisualStudioExtension.Converters
{
    class InfoToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case HandlerInfo handlerInfo:
                    return handlerInfo.MethodSymbol.ContainingType.Name;

                case InstantiationInfoVM instantiationInfo:

                    return instantiationInfo.Text;

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
