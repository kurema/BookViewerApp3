using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Compression;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;

namespace BookViewerApp.Books
{
    public class CbzBook : IBookFixed, ITocProvider, IDisposable
    {
        public ZipArchive Content { get; private set; }
        public ZipArchiveEntry[] AvailableEntries = new ZipArchiveEntry[0];

        public uint PageCount
        {
            get
            {
                return (uint)(AvailableEntries?.Length ?? 0);
            }
        }

        public string ID
        {
            get
            {
                if (_IDCache != null) return _IDCache;
                string result = "";
                foreach (var item in AvailableEntries.OrderBy((a) => a.FullName))
                {
                    result += Functions.CombineStringAndDouble(item.Name, item.Length);
                }
                return Functions.GetHash(result);
            }
        }

        public TocItem[] Toc { get; private set; }

        private string _IDCache = null;



        public event EventHandler Loaded;

        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public IPageFixed GetPage(uint i)
        {
            return new CbzPage(AvailableEntries[i]);
        }

        private Stream DisposableStream;

        public async Task LoadAsync(Stream stream)
        {
            await Task.Run(() =>
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                try
                {
                    Content = new ZipArchive(stream, ZipArchiveMode.Read, false,
                        System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ?
                        Encoding.GetEncoding(932) : Encoding.UTF8);
                    DisposableStream = stream;
                    OnLoaded(new EventArgs());
                }
                catch { }
            }
            );

            if (Content == null) { return; }

            var entries = new List<ZipArchiveEntry>();
            string[] supportedFile = BookManager.AvailableExtensionsImage;
            var cm = Content.Mode;
            var files = Content.Entries;
            foreach (var file in files)
            {
                var s = Path.GetExtension(file.Name).ToLower();
                var b = supportedFile.Contains(Path.GetExtension(file.Name).ToLower());
                if (supportedFile.Contains(Path.GetExtension(file.Name).ToLower()))
                {
                    entries.Add(file);
                }
            }

            entries = Functions.SortByArchiveEntry(entries, (a) => a.FullName).ToList();

            {
                //toc関係
                var toc = new List<TocItem>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var dirs = Path.GetDirectoryName(entries[i].FullName).Split(Path.DirectorySeparatorChar).ToList();
                    dirs.Add(".");

                    var ctoc = toc;
                    TocItem lastitem = null;
                    for (int j = 0; j < dirs.Count; j++)
                    {
                        dirs[j] = dirs[j] == "" ? "." : dirs[j];
                        if (ctoc.Count == 0 || ctoc.Last().Title != dirs[j])
                        {
                            ctoc.Add(new TocItem() { Children = new TocItem[0], Title = dirs[j], Page = i });
                        }

                        if (lastitem != null) lastitem.Children = ctoc.ToArray();
                        lastitem = ctoc.Last();
                        ctoc = lastitem.Children.ToList();
                    }
                }
                Toc = toc.ToArray();
            }

            AvailableEntries = entries.ToArray();
        }

        protected virtual void Dispose(bool cleanManaged)
        {
            if (cleanManaged)
            {
                Content?.Dispose();
                Content = null;
                DisposableStream?.Close();
                DisposableStream?.Dispose();
                DisposableStream = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

namespace BookViewerApp.Books
{
    public class CbzPage : IPageFixed, IDisposable
    {
        private ZipArchiveEntry Content;

        public IPageOptions Option
        {
            get; set;
        }

        public CbzPage(ZipArchiveEntry entry)
        {
            Content = entry;
            Cache = new MemoryStreamCache()
            {
                MemoryStreamProvider = async (th) =>
                  {
                      using (var s = Content.Open())
                      {
                          return await th.GetMemoryStreamAsync(s);
                      }
                  }
            };
        }

        public async Task<BitmapImage> GetBitmapAsync()
        {
            return await new Books.ImagePageStream((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream()).GetBitmapAsync();
        }

        private Helper.MemoryStreamCache Cache;

        public Task<bool> UpdateRequiredAsync()
        {
            return Task.FromResult(false);
        }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            try
            {
                using (var fileThumb = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await Functions.ResizeImage((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream(), fileThumb, width, () => { Content.ExtractToFile(file.Path, true); });
                }
            }
            catch (Exception)
            {
                try
                { await file.DeleteAsync(); }
                catch { }
            }
            return;
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            try
            {
                await new ImagePageStream((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream()).SetBitmapAsync(image);
            }
            catch
            {
                // ignored
            }
        }

        protected virtual void Dispose(bool managed)
        {
            if (managed)
            {
                Cache?.Dispose();
                Cache = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

}
