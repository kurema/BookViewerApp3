using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using System.Runtime.InteropServices.WindowsRuntime;

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;

namespace BookViewerApp.Books
{
    public class CompressedBook : IBookFixed, ITocProvider
    {
        public string ID
        {
            get
            {
                return IDCache = IDCache ?? GetID();
            }
        }

        private string IDCache = null;
        private string GetID()
        {
            string result = "";
            foreach (var item in Entries.OrderBy((a) => a.Key))
            {
                result += Functions.CombineStringAndDouble(item.Key, item.Size);
            }
            return Functions.GetHash(result);
        }

        public uint PageCount => (uint)Entries.Count();

        public event EventHandler Loaded;
        private void OnLoaded()
        {
            Loaded?.Invoke(this, new EventArgs());
        }

        public TocItem[] Toc { get; private set; }

        //private SharpCompress.Archive.IArchiveEntry Target;
        private SharpCompress.Archives.IArchiveEntry[] Entries;

        public async Task LoadAsync(Stream sr)
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
                            if (BookManager.AvailableExtensionsImage.Contains(Path.GetExtension(entry.Key).ToLower()))
                            {
                                entries.Add(entry);
                            }

                        }
                    }

                    IOrderedEnumerable<SharpCompress.Archives.IArchiveEntry> tempOrder;
                    if ((bool)SettingStorage.GetValue("SortNaturalOrder"))
                    {
                        tempOrder = entries.OrderBy((a) => new NaturalSort.NaturalList(a.Key));
                    }
                    else
                    {
                        tempOrder = entries.OrderBy((a) => a.Key);
                    }
                    if ((bool)SettingStorage.GetValue("SortCoverComesFirst"))
                    {
                        tempOrder = tempOrder.ThenBy((a) => a.Key.ToLower().Contains("cover"));
                    }
                    entries = tempOrder.ToList();

                    {
                        //toc関係
                        var toc = new List<TocItem>();
                        for (int i = 0; i < entries.Count; i++)
                        {
                            var dirs = Path.GetDirectoryName(entries[i].Key).Split(Path.DirectorySeparatorChar).ToList();
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

                    Entries = entries.ToArray();
                    OnLoaded();
                }
                catch { Entries = new SharpCompress.Archives.IArchiveEntry[0]; }
            });
        }

        public IPageFixed GetPage(uint i)
        {
            return new CompressedPage(Entries[i]);
        }
    }
}

namespace BookViewerApp.Books
{
    public class CompressedPage : IPageFixed
    {
        public IPageOptions Option
        {
            get; set;
        }

        private SharpCompress.Archives.IArchiveEntry Entry;

        public CompressedPage(SharpCompress.Archives.IArchiveEntry Entry) { this.Entry = Entry; }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            try
            {
                //await Functions.SaveStreamToFile(GetStream(), file);
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var s = Entry.OpenEntryStream();
                    var buffer = new byte[s.Length];
                    await s.ReadAsync(buffer, 0, (int)s.Length);
                    var ibuffer = buffer.AsBuffer();
                    await fileStream.WriteAsync(ibuffer);
                }
            }
            catch { }
        }

        private Windows.Storage.Streams.IRandomAccessStream GetStream()
        {
            try
            {
                var s = Entry.OpenEntryStream();
                var ms = new MemoryStream();
                s.CopyTo(ms);
                s.Dispose();
                ms.Seek(0, SeekOrigin.Begin);
                return ms.AsRandomAccessStream();
            }
            catch
            {
                return null;
            }
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            await new ImagePageStream(GetStream()).SetBitmapAsync(image);
        }

        public Task<bool> UpdateRequiredAsync()
        {
            return Task.FromResult(false);
        }
    }

}
