using kurema.FileExplorerControl.Models.FileItems;
using System;

using Windows.UI.Xaml.Media;

namespace kurema.FileExplorerControl.Models.IconProviders
{
    public class IconProviderDelegate : IIconProvider
    {
        public IconProviderDelegate(Func<IFileItem, (Func<ImageSource> Small, Func<ImageSource> Large)> iconSource, Func<ImageSource> defaultIconSmall = null, Func<ImageSource> defaultIconLarge = null)
        {
            IconSource = iconSource ?? throw new ArgumentNullException(nameof(iconSource));
            DefaultIconSmall = defaultIconSmall;
            DefaultIconLarge = defaultIconLarge;
        }

        public Func<ImageSource> DefaultIconSmall { get; set; }

        public Func<ImageSource> DefaultIconLarge { get; set; }

        public Func<IFileItem, (Func<ImageSource> Small, Func<ImageSource> Large)> IconSource { get; set; }

        public Func<ImageSource> GetIconLarge(IFileItem item) => IconSource(item).Large ?? null;

        public Func<ImageSource> GetIconSmall(IFileItem item) => IconSource(item).Small ?? null;
    }
}
