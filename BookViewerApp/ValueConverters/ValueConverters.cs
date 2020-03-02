using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using BookViewerApp.ViewModels;

namespace BookViewerApp.ValueConverters
{
    public class RateToPersantageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is float f) { return f * 100; }
            else if (value is double d) { return d * 100; }
            return (double)value * 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is float f) { return f / 100; }
            else if (value is double d) { return d / 100; }
            return (double)value / 100;
        }
    }

    public class RateToPersantageIntValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is float f) { return (int)(f * 100); }
            else if (value is double d) { return (int)(d * 100); }
            return (int)((double)value * 100);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is float f) { return f / 100; }
            else if (value is double d) { return d / 100; }
            return (double)value / 100;
        }
    }


    public class TextToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string) { return value; }
            else if (value is double) { return ((double)value).ToString(); }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double result;
            if (double.TryParse(value.ToString(), out result))
            {
                return (double)result;
            }
            else
            {
                return 0.0;
            }
        }
    }

    public class BookIdToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            Managers.ThumbnailManager.SetToImageSourceNoWait(value.ToString(), result);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
            // this is impossible.
        }
    }

    public class FloatEqualOneToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is float && (float) value != 1.0f)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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

    public class BoolToFlowDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is bool b && targetType==typeof(FlowDirection))
            {
                return b ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
            else if(targetType == typeof(FlowDirection))
            {
                return FlowDirection.LeftToRight;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if(value is FlowDirection f && targetType==typeof(bool))
            {
                return f == FlowDirection.RightToLeft;
            }
            else if(targetType == typeof(bool))
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string[] text = parameter.ToString().Split(':');
            if (text.Length < 2) throw new ArgumentException();
            if (value is bool b)
            {
                return b ? text[0] : text[1];
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string[] text = parameter.ToString().Split(':');
            return text[0] == value.ToString();
        }
    }

    public sealed class LocalizeConverter : IValueConverter
    {
        private static readonly Windows.ApplicationModel.Resources.ResourceLoader Loader = new Windows.ApplicationModel.Resources.ResourceLoader();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string resourceId = parameter as string;
            return !string.IsNullOrEmpty(resourceId) ? Loader.GetString(resourceId.Replace('.', '/')) : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
