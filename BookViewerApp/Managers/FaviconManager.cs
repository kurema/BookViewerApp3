using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;

using BookViewerApp.Helper;
using WebImageExtractor;

#nullable enable
namespace BookViewerApp.Managers
{
    public static class FaviconManager
    {
        private async static Task<Windows.Storage.StorageFolder> GetDataFolder() => await Functions.GetSaveFolderLocalCache().CreateFolderAsync("favicon", Windows.Storage.CreationCollisionOption.OpenIfExists);

        public static string GetFileNameFromID(string ID) => "" + Helper.Functions.EscapeFileName(ID) + Extension;

        public static string Extension => ".png";

        //public static System.Drawing.Image? GetImage(Uri uri)
        //{
        //    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //    var source = new FaviconFetcher.HttpSource()
        //    {
        //        CachePolicy= new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable)
        //    };
        //    var fetcher = new FaviconFetcher.Fetcher(source);
        //    var image = fetcher.FetchClosest(uri, new System.Drawing.Size(500, 500));
        //    return image;
        //}

        public async static Task<ImageMagick.MagickImage?> GetMaximumIcon(string uri)
        {
            if (uri == null) return null;
            var icons = await Extractor.GetAllIcons(uri, new ExtractionSettings(true, false, true, true, 1));

            int currentMaxWidth = -1;
            ImageMagick.MagickImage? currentMax = null;
            foreach (var item in icons)
            {
                var img = await item.GetImageAsync();
                if (img.Width > currentMaxWidth) currentMax = img;
            }
            return currentMax;

            //return (await Task.WhenAll(icons.Where(a => a != null).Select(async a => await a.GetImageAsync())))
            //    ?.Where(a => a != null)?.OrderByDescending(a => a.Width)?.FirstOrDefault();
        }
    }
}
