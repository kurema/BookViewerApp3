using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace BookViewerApp
{
    public class LicenseStorage
    {
        public static string CurrentLicense { get { return _CurrentLicense; } }
        private static string _CurrentLicense;

        public static string GetLicense(string Key)
        {
            if (_Licenses.ContainsKey(Key))
            {
                return _Licenses[Key];
            }
            return null;
        }

        public static Dictionary<String, String> Licenses { get
            {
                return new Dictionary<String, String>(_Licenses);
            }
        }
        private static Dictionary<String, String> _Licenses;

        public static async Task LoadAsync() { _Licenses = await GetLicenses(); }

        public static string GetLicenseName(string value)
        {
            using (var sr = new StringReader(value))
            {
                return sr.ReadLine();
            }
        }

        public static async System.Threading.Tasks.Task<Dictionary<String, String>> GetLicenses()
        {
            var result = new Dictionary<String, String>();
            var files = await (await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("LICENSE")).GetFilesAsync();
            foreach (var f in files)
            {
                using (var st = (await f.OpenReadAsync()).AsStream())
                using (var rd = new System.IO.StreamReader(st))
                {
                    if (f.Name == "LICENSE")
                    {
                        _CurrentLicense = await rd.ReadToEndAsync();
                    }
                    else {
                        result.Add(f.Name, await rd.ReadToEndAsync());
                    }
                }
            }
            return result;
        }

    }
}
