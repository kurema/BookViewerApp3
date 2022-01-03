using kurema.FileExplorerControl.Models.FileItems;
using System;
using System.Collections.Generic;

using Windows.UI.Xaml.Media;

using System.Threading;
using System.Threading.Tasks;

namespace kurema.FileExplorerControl.Models.IconProviders;

public class IconProviderDefault : IIconProvider
{
    public Func<ImageSource> DefaultIconSmall => () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/Icons/unknown_s.png"));

    public Func<ImageSource> DefaultIconLarge => () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/Icons/unknown_l.png"));

    private static Windows.UI.Xaml.Media.Imaging.BitmapImage IconSmallCache;
    private static Windows.UI.Xaml.Media.Imaging.BitmapImage IconLargeCache;

    public Task<Func<ImageSource>> GetIconSmall(IFileItem item, CancellationToken cancellationToken)
    {
        if (item is IIconProviderProvider container && container.Icon != null)
        {
            return Task.FromResult(container.Icon.DefaultIconSmall);
        }
        if (item?.IsFolder == true)
        {
            return Task.FromResult<Func<ImageSource>>(() => IconSmallCache ??= new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/Icons/folder_s.png")));
        }
        return Task.FromResult<Func<ImageSource>>(null);
    }

    public Task<Func<ImageSource>> GetIconLarge(IFileItem item, CancellationToken cancellationToken)
    {
        if (item is IIconProviderProvider container && container.Icon != null)
        {
            return Task.FromResult(container.Icon.DefaultIconLarge);
        }
        if (item?.IsFolder == true)
        {
            return Task.FromResult<Func<ImageSource>>(() => IconLargeCache ??= new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/Icons/folder_l.png")));
        }
        return Task.FromResult<Func<ImageSource>>(null);
    }

    public async static Task<(ImageSource SmallIcon, ImageSource LargeIcon)> GetIcon(IFileItem file, IEnumerable<IIconProvider> iconProviders, CancellationToken cancellationToken)
    {
        Func<ImageSource> small = null;
        Func<ImageSource> large = null;
        Func<ImageSource> smallDefault = null;
        Func<ImageSource> largeDefault = null;

        foreach (var item in iconProviders)
        {
            small ??= await item.GetIconSmall(file, cancellationToken);
            large ??= await item.GetIconLarge(file, cancellationToken);
            smallDefault ??= item.DefaultIconSmall;
            largeDefault ??= item.DefaultIconLarge;
        }

        return ((small ?? smallDefault)?.Invoke(), (large ?? largeDefault)?.Invoke());
    }
}
