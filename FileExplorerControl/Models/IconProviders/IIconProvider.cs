using kurema.FileExplorerControl.Models.FileItems;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Windows.UI.Xaml.Media;

namespace kurema.FileExplorerControl.Models.IconProviders
{
    public interface IIconProvider
    {
        Func<ImageSource> DefaultIconSmall { get; }
        Func<ImageSource> DefaultIconLarge { get; }

        Task<Func<ImageSource>> GetIconSmall(IFileItem item, CancellationToken cancellationToken);
        Task<Func<ImageSource>> GetIconLarge(IFileItem item, CancellationToken cancellationToken);
    }
}
