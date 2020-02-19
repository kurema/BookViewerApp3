using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Data;


namespace kurema.FileExplorerControl.Helper.ValueConverters
{
    public class IntToStringValueConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var text = (parameter?.ToString() ?? "").Split(':');
            int cnt = 0;
            int.TryParse(value?.ToString() ?? "0", out cnt);
            return text[Math.Max(0, Math.Min(text.Length - 1, cnt))];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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

    public class IsZeroValueConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string[] text = parameter.ToString().Split(':');
            if (text.Length < 2) throw new ArgumentException();

            return value.ToString() == 0.ToString() ? text[0] : text[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string[] text = parameter.ToString().Split(':');
            return text[0] == value.ToString() ? 0 : 1;
        }
    }

    public class OrderToDataGridSortDirection : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is ViewModels.FileItemViewModel.OrderStatus status)
            {
                var key = parameter.ToString();
                if (status.Key == key)
                {
                    return status.KeyIsAscending ? Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Ascending : Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Descending;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class UlongToHumanReadableSizeConverter : IValueConverter
    {
        public string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000)
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000)
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000)
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) 
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000)
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) 
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B");
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";
            long num;
            if(long.TryParse(value.ToString(),out num))
            {
                return GetBytesReadable(num);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
