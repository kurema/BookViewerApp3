using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Managers;

public static class ResourceManager
{
    public static readonly Windows.ApplicationModel.Resources.ResourceLoader Loader = new Windows.ApplicationModel.Resources.ResourceLoader();
}
