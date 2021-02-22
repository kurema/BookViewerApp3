using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;

using BookViewerApp.Helper;

namespace BookViewerApp.Managers
{
    public static class FaviconManager
    {
        private async static Task<Windows.Storage.StorageFolder> GetDataFolder() => await Functions.GetSaveFolderLocalCache().CreateFolderAsync("favicon", Windows.Storage.CreationCollisionOption.OpenIfExists);

        public static string GetFileNameFromID(string ID) => "" + Helper.Functions.EscapeFileName(ID) + Extension;

        public static string Extension => ".jpeg";

    }
}
