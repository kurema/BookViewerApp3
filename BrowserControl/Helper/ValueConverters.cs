using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

namespace kurema.BrowserControl.Helper.ValueConverters;

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

public class ObjectToTreeViewItemConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IEnumerable<object> obj)
        {
            return obj.Select(a => new Windows.UI.Xaml.Controls.TreeViewNode()
            {
                HasUnrealizedChildren = true,
                Content = a,
                IsExpanded = false,
            }).ToArray();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class UIElementCollectionEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string[] text = parameter.ToString().Split(':');
        if (text.Length < 2) throw new ArgumentException();
        return (value as Windows.UI.Xaml.Controls.UIElementCollection)?.Count == 0 ? text[0] : text[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class IntTableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not int number) return null;
        string[] text = parameter.ToString().Split(':');
        return text[Math.Min(text.Length - 1, number)];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
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
