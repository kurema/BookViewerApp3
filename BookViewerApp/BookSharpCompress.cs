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

        public uint PageCount
        {
            get
            {
                return (uint)Entries.Count();
            }
        }

        public event EventHandler Loaded;
        private void OnLoaded()
        {
            if (Loaded != null) Loaded(this, new EventArgs());
        }

        private SharpCompress.Archives.IArchiveEntry Target;
        private SharpCompress.Archives.IArchiveEntry[] Entries;

        public async Task LoadAsync(System.IO.Stream sr)
        {
            SharpCompress.Archives.IArchive archive;
            await Task.Run(() =>
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
                if ((bool)SettingStorage.GetValue("SortNaturalOrder"))
                {
                    entries.Sort((a, b) => NaturalSort.NaturalCompare(a.Key, b.Key));
                }else
                {
                    entries.Sort((a,b)=>a.Key.CompareTo(b.Key));

                }
                if ((bool)SettingStorage.GetValue("SortCoverComesFirst"))
                {
                    entries.Sort((a, b) => b.Key.ToLower().Contains("cover").CompareTo(a.Key.ToLower().Contains("cover")));
                }

                Entries = entries.ToArray();
                OnLoaded();
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
            var s = Entry.OpenEntryStream();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            s.Dispose();
            ms.Seek(0, SeekOrigin.Begin);
            return ms.AsRandomAccessStream();
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
