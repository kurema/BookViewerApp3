using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using System.Reflection.Metadata.Ecma335;

namespace BookViewerApp.ValueConverters;

public sealed class RateToPersantageValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return null;
        if (value is float f) { return f * 100; }
        else if (value is double d) { return d * 100; }
        return (double)value * 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return null;
        if (value is float f) { return f / 100; }
        else if (value is double d) { return d / 100; }
        return (double)value / 100;
    }
}

public sealed class RateToPersantageIntValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return null;
        if (value is float f) { return (int)(f * 100); }
        else if (value is double d) { return (int)(d * 100); }
        return (int)((double)value * 100);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is null) return null;
        if (value is float f) { return f / 100; }
        else if (value is double d) { return d / 100; }
        return (double)value / 100;
    }
}


public sealed class TextToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string) { return value; }
        else if (value is double) { return ((double)value).ToString(); }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (double.TryParse(value.ToString(), out double result))
        {
            return (double)result;
        }
        else
        {
            return 0.0;
        }
    }
}

public sealed class FloatEqualOneToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float && (float)value != 1.0f)
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

public sealed class EqualZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string[] text = parameter?.ToString()?.Split(':') ?? new string[0];
        if (text.Length < 2) return "";
        if (double.TryParse(value?.ToString(), out double result) && result == 0)
        {
            return text[0];
        }
        else
        {
            return text[1];
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class BoolToDoubleValueConverter : IValueConverter
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

public sealed class BoolToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && targetType == typeof(FlowDirection))
        {
            return b ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        else if (targetType == typeof(FlowDirection))
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
        if (value is FlowDirection f && targetType == typeof(bool))
        {
            return f == FlowDirection.RightToLeft;
        }
        else if (targetType == typeof(bool))
        {
            return false;
        }
        else
        {
            return null;
        }
    }
}

public sealed class BoolToStringConverter : IValueConverter
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

public sealed class NullableBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        switch (value)
        {
            case null:
                return parameter?.ToString() is "true" or "True" || parameter?.ToString() == bool.TrueString;
            case bool b:
                return b;
        }
        return value.ToString() switch
        {
            "true" or "True" => true,
            "false" or "False" => false,
            _ => parameter?.ToString() is "true" or "True"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Convert(value, targetType, parameter, language);
    }
}

public sealed class StringChangeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var word = value.ToString();
        var dic = parameter.ToString().Split("::").Select(a => a.Split(":")).Where(a => a.Length >= 2).ToDictionary(a => a[0], a => a[1]);
        if (dic.ContainsKey(word)) return dic[word];
        if (dic.ContainsKey("default")) return dic["default"];
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class StringNullOrEmptyConverter : kurema.FileExplorerControl.Helper.ValueConverters.StringNullOrEmptyConverter
{
}

public sealed class IsValidUriIValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return Uri.TryCreate(value?.ToString(), UriKind.Absolute, out _);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class LanguageCodeToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var culture = new System.Globalization.CultureInfo(value?.ToString(), false);
            switch (parameter)
            {
                case nameof(culture.DisplayName):
                case null: return culture?.DisplayName;
                case nameof(culture.EnglishName): return culture?.EnglishName;
                case nameof(culture.NativeName): return culture?.NativeName;
            }
            return culture?.DisplayName;
        }
        catch
        {
            return "";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class BoolToAcrylicBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? AcrylicBackgroundSource.HostBackdrop : AcrylicBackgroundSource.Backdrop;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
