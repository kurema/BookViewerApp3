using kurema.FileExplorerControl.Models.FileItems;
using System;

using Windows.UI.Xaml.Media;
using System.Threading;
using System.Threading.Tasks;

namespace kurema.FileExplorerControl.Models.IconProviders
{
    public class IconProviderDelegate : IIconProvider
    {
        public IconProviderDelegate(Func<IFileItem, CancellationToken, Task<(Func<ImageSource> Small, Func<ImageSource> Large)>> iconSource, Func<ImageSource> defaultIconSmall = null, Func<ImageSource> defaultIconLarge = null)
        {
            IconSource = iconSource ?? throw new ArgumentNullException(nameof(iconSource));
            DefaultIconSmall = defaultIconSmall;
            DefaultIconLarge = defaultIconLarge;
        }

        public Func<ImageSource> DefaultIconSmall { get; set; }

        public Func<ImageSource> DefaultIconLarge { get; set; }

        public Func<IFileItem, CancellationToken, Task<(Func<ImageSource> Small, Func<ImageSource> Large)>> IconSource { get; set; }

        public async Task<Func<ImageSource>> GetIconLarge(IFileItem item, CancellationToken cancellationToken) => (await IconSource(item, cancellationToken)).Large;

        public async Task<Func<ImageSource>> GetIconSmall(IFileItem item, CancellationToken cancellationToken) => (await IconSource(item, cancellationToken)).Small;

        public static Func<IFileItem, CancellationToken, Task<(Func<ImageSource> Small, Func<ImageSource> Large)>> NullResult => (a, _) => Task.FromResult<(Func<ImageSource> Small, Func<ImageSource> Large)>((null, null));
    }
}
