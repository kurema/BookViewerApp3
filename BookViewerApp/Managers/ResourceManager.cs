using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace BookViewerApp.Managers;

public static class ResourceManager
{
    public static readonly ResourceLoader Loader = new();
    public static readonly ResourceLoader LoaderFileExplorer = ResourceLoader.GetForCurrentView("FileExplorerControl/Resources");
}
