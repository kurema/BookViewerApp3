using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

namespace BookViewerApp.ValueConverters
{
    public class RateToPersantageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (double)value * 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (double)value / 100;
        }
    }

    public class BookIdToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            ThumbnailManager.SetToImageSourceNoWait(value.ToString(), result);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
            // this is impossible.
        }
    }

    public class BoolToDoubleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && targetType == typeof(double))
            {
                return (bool)value ? -1 : 1;
            }
            else if (targetType == typeof(double))
            {
                return 1;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is double && targetType == typeof(bool))
            {
                if ((double)value == 1.0) return false;
                else if ((double)value == -1.0) return true;
            }
            return false;
        }
    }
}
