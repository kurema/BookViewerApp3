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
            for (int i = 0; i < 3; i++)
            {
                try { icons ??= await Extractor.GetAllIcons(uri, new ExtractionSettings(true, false, true, false, 0)); }
                catch { await Task.Delay(500); }
            }
            if (icons == null) return null;

            return (await Task.WhenAll(icons.Where(a => a != null).Select(async a =>
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        {
                            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                            var result2 = await client.GetAsync(a.Uri);
                            using System.IO.Stream st = await result2.Content.ReadAsStreamAsync();

                            if (a.Uri.ToLowerInvariant().EndsWith(".ico")) return new ImageMagick.MagickImageCollection(st, ImageMagick.MagickFormat.Ico).ToArray();
                            else return new ImageMagick.MagickImageCollection(st).ToArray();
                        }
                    }
                    catch { }
                    await Task.Delay(500);
                }
                return null;
            })))?.Where(a => a != null)?.SelectMany(a => a)
                ?.Where(a => a != null)?.OrderByDescending(a => a?.Width)?.FirstOrDefault();
        }
    }
}
