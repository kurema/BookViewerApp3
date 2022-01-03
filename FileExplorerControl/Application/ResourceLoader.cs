using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kurema.FileExplorerControl.Application;

public static class ResourceLoader
{
    private static Windows.ApplicationModel.Resources.ResourceLoader _Loader;
    public static Windows.ApplicationModel.Resources.ResourceLoader Loader => _Loader ??= Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse(AssemblyName + "/Resources");
    public static string AssemblyName => typeof(ResourceLoader).Assembly.GetName().Name;

}
