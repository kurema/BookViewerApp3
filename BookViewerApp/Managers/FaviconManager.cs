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
        public async static Task<ImageMagick.IMagickImage<ushort>?> GetMaximumIcon(string uri)
        {
            if (uri == null) return null;
            IEnumerable<WebImage>? icons = null;
            for (int i = 0; i < 2; i++)
            {
                try { icons = icons ?? await Extractor.GetAllIcons(uri, new ExtractionSettings(true, false, true, false, 0)); }
                catch { }
            }
            if (icons == null) return null;

            //int currentMaxWidth = -1;
            //ImageMagick.MagickImage? currentMax = null;
            //foreach (var item in icons)
            //{
            //    ImageMagick.MagickImage? img = null;
            //    for (int i = 0; i < 2; i++)
            //    {
            //        try { img = img ?? await item.GetImageAsync(); }
            //        catch { continue; }
            //    }
            //    if (img?.Width > currentMaxWidth)
            //    {
            //        currentMaxWidth = img.Width;
            //        currentMax = img;
            //    }
            //}
            //return currentMax;

            return (await Task.WhenAll(icons.Where(a => a != null).Select(async a =>
            {
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        if(a.Uri.EndsWith(".ico"))
                        {
                            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                            var result2 = await client.GetAsync(a.Uri);
                            using System.IO.Stream st = await result2.Content.ReadAsStreamAsync();
                            var image2 = new ImageMagick.MagickImageCollection(st,ImageMagick.MagickFormat.Ico);
                            return image2.ToArray();
                        }
                        else
                        {
                            return new[] { await a.GetImageAsync() };
                        }
                    }
                    catch { }
                }
                return null;
            })))?.Where(a => a != null)?.SelectMany(a=>a)
                ?.Where(a => a != null)?.OrderByDescending(a => a?.Width)?.FirstOrDefault();
        }
    }
}
