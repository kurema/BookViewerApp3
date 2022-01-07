using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Data;


namespace kurema.FileExplorerControl.Helper.ValueConverters;

public class IntToStringValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var text = (parameter?.ToString() ?? "").Split(':');
        int.TryParse(value?.ToString() ?? "0", out int cnt);
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
        else if (value is null && text.Length < 3)
        {
            return text[2];
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

public class StringTableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return GetTableValue(parameter?.ToString(), value?.ToString(), false);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        //targetType does not seem to be correct. why?
        return GetTableValue(parameter?.ToString(), value?.ToString(), true);
    }

    public static string GetTableValue(string table, string value, bool reverse)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(value)) return "";
        var text = table.Split(':');
        var @default = "";
        for (int i = 0; i + 1 < text.Length; i += 2)
        {
            var k = text[i];
            var v = text[i + 1];
            if (reverse) (k, v) = (v, k);
            if (k == value) return v;
            if (k == "" && !reverse) @default = v;
        }
        return @default;
    }
}

public class StringTableEnumConverter<T> : IValueConverter where T : Enum
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return StringTableConverter.GetTableValue(parameter?.ToString(), value?.ToString(), false);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var result = StringTableConverter.GetTableValue(parameter?.ToString(), value?.ToString(), true);
        return Enum.Parse(typeof(T), result);
    }
}

public class StringTableCaseFormatTypeConverter : StringTableEnumConverter<ViewModels.RenameRegexViewModel.CaseFormatType>
{
}

public class EqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b)
        {
            return parameter;
        }
        else
        {
            throw new NotImplementedException();
        }
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

public class OrderToDirectionFontIconGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (String.IsNullOrEmpty(parameter as string)) return "";
        var key = parameter.ToString();
        if (value is ViewModels.FileItemViewModel.OrderStatus status)
        {
            if (status.Key == key)
            {
                return status.KeyIsAscending ? "\uE70E" : "\uE70D";
            }
        }
        return "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class OrderToDataGridSortDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ViewModels.FileItemViewModel.OrderStatus status)
        {
            var key = parameter?.ToString();
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

public class CanRenameToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ViewModels.FileItemViewModel vm)
        {
            return !(vm?.Content?.RenameCommand?.CanExecute("newname") == true);
        }
        else
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return "";
        try
        {
            return string.Format(parameter?.ToString() ?? "{0}", value);
        }
        catch
        {
            return value?.ToString();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class LocalizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string resourceId = parameter as string;
        return !string.IsNullOrEmpty(resourceId) ? Application.ResourceLoader.Loader.GetString(resourceId.Replace('.', '/')) : Windows.UI.Xaml.DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
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
        readable /= 1024;
        // Return formatted number with suffix
        return readable.ToString("0.### ") + suffix;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return "";
        if (long.TryParse(value.ToString(), out long num))
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
