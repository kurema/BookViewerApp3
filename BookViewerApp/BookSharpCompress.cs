using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using System.Runtime.InteropServices.WindowsRuntime;

namespace BookViewerApp.Books.Compressed
{
    public class CompressedBook : IBookFixed
    {
        public string ID
        {
            get
            {
                return IDCache = IDCache ?? GetID();
            }
        }

        private string IDCache = null;
        private string GetID() {
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

        //private SharpCompress.Archive.IArchiveEntry Target;
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
                            if (BookManager.AvailableExtensionsImage.Contains(System.IO.Path.GetExtension(entry.Key).ToLower()))
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
                    Entries = entries.ToArray();
                    OnLoaded();
                }
                catch { this.Entries = new SharpCompress.Archives.IArchiveEntry[0]; }
            });
        }

        public IPageFixed GetPage(uint i)
        {
            return new CompressedPage(Entries[i]);
        }
    }

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
                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
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
            await new Image.ImagePageStream(GetStream()).SetBitmapAsync(image);
        }

        public async Task<bool> UpdateRequiredAsync()
        {
            return false;
        }
    }

}
