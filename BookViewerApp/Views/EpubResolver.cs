using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

using System.IO.Compression;
using System.IO;

namespace BookViewerApp
{
    public class EpubResolver : Windows.Web.IUriToStreamResolver
    {
        public ZipArchive Content { get; private set; }

        public event EventHandler Loaded;
        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public async Task LoadAsync(Stream stream)
        {
            await Task.Run(() => 
            {
                Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                try
                {
                    Content = new ZipArchive(stream, ZipArchiveMode.Read, false,
                        System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ?
                        Encoding.GetEncoding(932) : Encoding.UTF8);
                    OnLoaded(new EventArgs());
                }
                catch { }
            }
            );

        }

        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            return GetContent(uri).AsAsyncOperation();
        }

        protected async Task<IInputStream> GetContent(Uri uri)
        {
            var invalid = new Exception("Invalid Path");
            if (uri == null) throw invalid;
            try
            {
                //Security!
                if (uri.LocalPath.ToLower().StartsWith("/reader/"))
                {
                    var pathTail = uri.LocalPath.Replace("/reader/", "", StringComparison.OrdinalIgnoreCase);
                    var f = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine("ms-appx:///res/reader/", pathTail)));
                    return await f.OpenAsync(Windows.Storage.FileAccessMode.Read);
                }
                if (uri.LocalPath.ToLower().StartsWith("/contents/"))
                {
                    var pathTail = uri.LocalPath.Replace("/contents/", "", StringComparison.OrdinalIgnoreCase);
                    if (Content == null) throw invalid;

                    var entry = Content.Entries.FirstOrDefault(a => a.FullName.ToLower() == pathTail.ToLower());
                    if (entry == null) throw invalid;

                    var s = entry.Open();

                    var ms = new MemoryStream();
                    await s.CopyToAsync(ms);
                    s.Dispose();

                    //AsInputStream() or AsRandomAccessStream() just dont work for MemoryStream... At least for UriToStreamAsync...
                    return ms.AsRandomAccessStream();
                }
                throw invalid;
            }
            catch (Exception) { throw invalid; }
        }
    }

    public class EpubResolverSharp : Windows.Web.IUriToStreamResolver
    {
        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            return GetContent(uri).AsAsyncOperation();
        }

        private SharpCompress.Archives.IArchiveEntry[] Entries;

        public async Task LoadAsync(System.IO.Stream sr)
        {
            SharpCompress.Archives.IArchive archive;
            await Task.Run(() =>
            {
                try
                {
                    archive = SharpCompress.Archives.ArchiveFactory.Open(sr);
                    var entries = new List<SharpCompress.Archives.IArchiveEntry>();
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory && !entry.IsEncrypted)
                        {
                                entries.Add(entry);
                        }
                    }

                    Entries = entries.ToArray();
                }
                catch { this.Entries = new SharpCompress.Archives.IArchiveEntry[0]; }
            });
        }

        protected async Task<IInputStream> GetContent(Uri uri)
        {
            var invalid = new Exception("Invalid Path");
            if (uri == null) throw invalid;
            try
            {
                //Security!
                if (uri.LocalPath.ToLower().StartsWith("/epub.js/"))
                {
                    var pathTail = uri.LocalPath.Replace("/epub.js/", "", StringComparison.OrdinalIgnoreCase);
                    var f = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine("ms-appx:///res/epub.js/", pathTail)));
                    return await f.OpenAsync(Windows.Storage.FileAccessMode.Read);
                }
                if (uri.LocalPath.ToLower().StartsWith("/contents/"))
                {
                    var pathTail = uri.LocalPath.Replace("/contents/", "", StringComparison.OrdinalIgnoreCase);

                    var entry = Entries.FirstOrDefault(a => a.Key.ToLower() == pathTail.ToLower());
                    //var entry = Content.GetEntry(pathTail.ToLower());
                    if (entry == null) throw invalid;

                    var s = entry.OpenEntryStream();

                    var ms = new MemoryStream();
                    s.CopyTo(ms);
                    s.Dispose();

                    return ms.AsInputStream();
                }
                throw invalid;
            }
            catch (Exception) { throw invalid; }
        }
    }
}
