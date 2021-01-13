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

#nullable enable
namespace BookViewerApp.Books
{
    public class CompressedBook : IBookFixed, ITocProvider, IDisposableBasic
    {
        public string ID
        {
            get
            {
                return IDCache ??= GetID();
            }
        }

        private string? IDCache = null;
        private string GetID()
        {
            string result = "";
            foreach (var item in Entries.OrderBy((a) => a.Key))
            {
                result += Functions.CombineStringAndDouble(item.Key, item.Size);
            }
            return Functions.GetHash(result);
        }

        public uint PageCount => (uint)(Entries?.Length ?? 0);

        public event EventHandler? Loaded;
        private void OnLoaded()
        {
            Loaded?.Invoke(this, new EventArgs());
        }

        public TocItem[] Toc { get; private set; } = new TocItem[0];

        //private SharpCompress.Archive.IArchiveEntry Target;
        private SharpCompress.Archives.IArchiveEntry[] Entries = new SharpCompress.Archives.IArchiveEntry[0];

        private SharpCompress.Archives.IArchive? DisposableContent;//To Dispose
        private Stream? DisposableStream;

        public async Task LoadAsync(Stream sr)
        {
            SharpCompress.Archives.IArchive archive;
            await Task.Run(() =>
            {
                try
                {
                    DisposableContent = archive = SharpCompress.Archives.ArchiveFactory.Open(sr);
                    DisposableStream = sr;
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

                    entries = Functions.SortByArchiveEntry(entries, (a) => a.Key).ToList();

                    {
                        //toc関係
                        var toc = new List<TocItem>();
                        for (int i = 0; i < entries.Count; i++)
                        {
                            var dirs = Path.GetDirectoryName(entries[i].Key).Split(Path.DirectorySeparatorChar).ToList();
                            dirs.Add(".");

                            var ctoc = toc;
                            TocItem? lastitem = null;
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

        public void DisposeBasic()
        {
            DisposableContent?.Dispose();
            DisposableContent = null;
            DisposableStream?.Close();
            DisposableStream?.Dispose();
            DisposableStream = null;
        }
    }
}

namespace BookViewerApp.Books
{
    public class CompressedPage : IPageFixed
    {
        public IPageOptions? Option
        {
            get; set;
        }

        //private SharpCompress.Archives.IArchiveEntry Entry;

        public CompressedPage(SharpCompress.Archives.IArchiveEntry Entry)
        {
            //this.Entry = Entry;
            Cache = new MemoryStreamCache()
            {
                MemoryStreamProvider = async (th) =>
                {
                    using var s = Entry.OpenEntryStream();
                    return await th.GetMemoryStreamAsync(s);
                }
            };
        }

        private readonly MemoryStreamCache Cache;

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            try
            {
                //await Functions.SaveStreamToFile(GetStream(), file);
                using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                var ms = await Cache.GetMemoryStreamByProviderAsync();
                await Functions.ResizeImage(ms.AsRandomAccessStream(), fileStream, width, async () =>
                {
                    using var s = await Cache.GetMemoryStreamByProviderAsync();
                    var buffer = new byte[s.Length];
                    await s.ReadAsync(buffer, 0, (int)s.Length);
                    var ibuffer = buffer.AsBuffer();
                    await fileStream.WriteAsync(ibuffer);
                });
            }
            catch (Exception)
            {
                try
                {
                    await file.DeleteAsync();
                }
                catch { }
            }
        }


        public async Task SetBitmapAsync(BitmapImage image, double width, double height)
        {
            var stream = await Cache.GetMemoryStreamByProviderAsync();
            if (stream == null) return;
            Task? task = new ImagePageStream(stream.AsRandomAccessStream())?.SetBitmapAsync(image, width, height);
            if (task != null) await task;
        }

        public Task<bool> UpdateRequiredAsync(double width, double height)
        {
            return Task.FromResult(false);
        }
    }

}
