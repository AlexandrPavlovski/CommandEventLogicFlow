using Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VisualStudioExtension.Converters
{
    [ValueConversion(typeof(HandlerInfo), typeof(string))]
    class HandlerInfoToHandlerClassNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var handlerInfo = (HandlerInfo)value;
            return handlerInfo.MethodSymbol.ContainingType.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
