using Core;
using Core.Graph;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VisualStudioExtension.Converters
{
    [ValueConversion(typeof(HandlerInfo), typeof(string))]
    class CommandEventToIconConverter : IValueConverter
    {
        // font Candara
        static BitmapImage commandImage;
        static BitmapImage eventImage;

        static CommandEventToIconConverter()
        {
            commandImage = new BitmapImage();
            commandImage.BeginInit();
            commandImage.UriSource = new Uri(@"pack://application:,,,/VisualStudioExtension;Component/Resources/IconCommand.png");
            commandImage.EndInit();

            eventImage = new BitmapImage();
            eventImage.BeginInit();
            eventImage.UriSource = new Uri(@"pack://application:,,,/VisualStudioExtension;Component/Resources/IconEvent.png");
            eventImage.EndInit();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nodeType = (GraphNodeType)value;
            switch (nodeType)
            {
                case GraphNodeType.Command: return commandImage;
                case GraphNodeType.Event: return eventImage;
                default: return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
