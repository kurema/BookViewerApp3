using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using BookViewerApp.ViewModels;
using System.Reflection.Metadata.Ecma335;

namespace BookViewerApp.ValueConverters;
public sealed class BookIdToImageSource : IValueConverter
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

public sealed class LocalizeConverter : IValueConverter
{
    public const string FileExplorerPrefix = "FileExplorerControl/";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string resourceId = parameter as string;
        if (string.IsNullOrEmpty(resourceId)) return DependencyProperty.UnsetValue;
        resourceId = resourceId.Replace('.', '/');
        if (resourceId.StartsWith(FileExplorerPrefix)) return Managers.ResourceManager.LoaderFileExplorer.GetString(resourceId.Replace(FileExplorerPrefix, ""));
        return Managers.ResourceManager.Loader.GetString(resourceId);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

public sealed class LocalizeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string resourceId = value?.ToString();
        return !string.IsNullOrEmpty(resourceId) ? Managers.ResourceManager.Loader.GetString(String.Format(parameter?.ToString() ?? "{0}", resourceId)) : DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

public sealed class UrlDomainConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return Managers.ExtensionAdBlockerManager.GetHostOfUri(value?.ToString()) ?? value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value;
    }
}

public sealed class AdBlockerIsHostEnabled : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var host = Managers.ExtensionAdBlockerManager.GetHostOfUri(value?.ToString());
        if (host is null) return false;
        return !Managers.ExtensionAdBlockerManager.IsInWhitelist(host.ToUpperInvariant());
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}