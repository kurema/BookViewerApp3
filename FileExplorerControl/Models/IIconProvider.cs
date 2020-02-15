using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Media;

namespace kurema.FileExplorerControl.Models
{
    public interface IIconProvider
    {
        Func<ImageSource> DefaultIconSmall { get; }
        Func<ImageSource> DefaultIconLarge { get; }

        Func<ImageSource> GetIconSmall(Models.IFileItem item);
        Func<ImageSource> GetIconLarge(Models.IFileItem item);
    }
}
