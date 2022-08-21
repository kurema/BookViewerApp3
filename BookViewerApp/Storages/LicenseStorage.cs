using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

#nullable enable
namespace BookViewerApp.Storages;

public class LicenseStorage
{
    public static StorageContent<Licenses.licenses> LocalLicense = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/Licenses.xml", () => new Licenses.licenses());

    [Obsolete]
    public static string? CurrentLicense { get => LocalLicense.Content?.firstparty?[0]?.license?.term; }

    [Obsolete]
    public static Dictionary<String, String?>? Licenses { get => LocalLicense.Content?.thirdparty?.ToDictionary(a => a.title, a => a.license?.term); }
}
