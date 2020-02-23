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

namespace BookViewerApp.Books
{
    public class CbzBook : IBookFixed, ITocProvider
    {
        public ZipArchive Content { get; private set; }
        public ZipArchiveEntry[] AvailableEntries = new ZipArchiveEntry[0];

        public uint PageCount
        {
            get
            {
                return (uint)AvailableEntries.Count();
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

            IOrderedEnumerable<ZipArchiveEntry> tempOrder;
            if ((bool)SettingStorage.GetValue("SortNaturalOrder"))
            {
                tempOrder = entries.OrderBy((a) => new NaturalSort.NaturalList(a.FullName));
                //entries.Sort((a, b) => NaturalSort.NaturalCompare(a.FullName, b.FullName));
            }
            else
            {
                tempOrder = entries.OrderBy((a) => a.FullName);
                //entries.Sort((a, b) => a.FullName.CompareTo(b.FullName));
            }

            if ((bool)SettingStorage.GetValue("SortCoverComesFirst"))
            {
                tempOrder = tempOrder.ThenBy((a) => a.FullName.ToLower().Contains("cover"));
                //entries.Sort((a, b) => b.FullName.ToLower().Contains("cover").CompareTo(a.FullName.ToLower().Contains("cover")));
            }
            entries = tempOrder.ToList();

            {
                //toc関係
                var toc = new List<TocItem>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var dirs = Path.GetDirectoryName(entries[i].FullName).Split(Path.DirectorySeparatorChar).ToList();
                    dirs.Add(".");

                    var ctoc = toc;
                    TocItem lastitem = null;
                    for (int j = 0; j < dirs.Count(); j++)
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
    }
}

namespace BookViewerApp.Books
{
    public class CbzPage : IPageFixed
    {
        private ZipArchiveEntry Content;

        private Windows.Storage.Streams.IRandomAccessStream cache = null;

        public IPageOptions Option
        {
            get; set;
        }

        public CbzPage(ZipArchiveEntry entry)
        {
            Content = entry;
        }

        public async Task<BitmapImage> GetBitmapAsync()
        {
            if (cache == null)
            {
                var s = Content.Open();
                var ms = new MemoryStream();
                s.CopyTo(ms);
                s.Dispose();
                cache = ms.AsRandomAccessStream();
            }
            return await new Books.ImagePageStream(cache).GetBitmapAsync();
        }

        public Task<bool> UpdateRequiredAsync()
        {
            return Task.FromResult(false);
        }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            try
            {
                //if (!System.IO.File.Exists(file.Path))
                Content.ExtractToFile(file.Path, true);
            }
            catch { }
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            try
            {
                if (cache == null)
                {
                    var s = Content.Open();
                    var ms = new MemoryStream();
                    s.CopyTo(ms);
                    s.Dispose();
                    cache = ms.AsRandomAccessStream();
                }
                await new ImagePageStream(cache).SetBitmapAsync(image);
            }
            catch
            {
                // ignored
            }
        }
    }

}
