using System;
using System.Collections.Generic;

using Windows.UI.Xaml.Media;

namespace kurema.FileExplorerControl.Models
{
    public class IconProviderDefault : IIconProvider
    {
        public Func<ImageSource> DefaultIconSmall => () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_unknown_s.png"));

        public Func<ImageSource> DefaultIconLarge => () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_unknown_l.png"));

        public Func<ImageSource> GetIconSmall(IFileItem item)
        {
            if (item?.IsFolder == true)
            {
                return () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_folder_s.png"));
            }
            return null;
        }

        public Func<ImageSource> GetIconLarge(IFileItem item)
        {
            if (item?.IsFolder == true)
            {
                return () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_folder_l.png"));
            }
            return null;
        }

        public static (ImageSource SmallIcon, ImageSource LargeIcon) GetIcon(IFileItem file, IEnumerable<IIconProvider> iconProviders)
        {
            Func<ImageSource> small = null;
            Func<ImageSource> large = null;
            Func<ImageSource> smallDefault = null;
            Func<ImageSource> largeDefault = null;

            foreach (var item in iconProviders)
            {
                small = small ?? item.GetIconSmall(file);
                large = large ?? item.GetIconLarge(file);
                smallDefault = smallDefault ?? item.DefaultIconSmall;
                largeDefault = largeDefault ?? item.DefaultIconLarge;
            }

            return ((small ?? smallDefault)?.Invoke(), (large ?? largeDefault)?.Invoke());
        }
    }
}
