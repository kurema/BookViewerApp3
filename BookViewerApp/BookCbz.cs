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

namespace BookViewerApp.Books.Cbz
{
    public class CbzBook : IBookFixed
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
                foreach(var item in AvailableEntries)
                {
                    result += Functions.CombineStringAndDouble(item.Name, item.Length);
                }
                return Functions.GetHash(result);
            }
        }
        private string _IDCache = null;



        public event EventHandler Loaded;

        private void OnLoaded(EventArgs e)
        {
            if (Loaded != null) Loaded(this, e);
        }

        public IPageFixed GetPage(uint i)
        {
            return new CbzPage(AvailableEntries[i]);
        }

        public async Task LoadAsync(Stream stream)
        {
            await Task.Run(() =>
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                Content = new ZipArchive(stream, ZipArchiveMode.Read,false,Encoding.GetEncoding(932));
                OnLoaded(new EventArgs());
            }
            );

            var entries = new List<ZipArchiveEntry>();
            string[] supportedFile = BookManager.AvailableExtensionsImage;
            var cm=Content.Mode;
            var files = Content.Entries;
            foreach(var file in files)
            {
                var s = Path.GetExtension(file.Name).ToLower();
                var b = supportedFile.Contains(Path.GetExtension(file.Name).ToLower());
                if (supportedFile.Contains(Path.GetExtension(file.Name).ToLower()))
                {
                    entries.Add(file);
                }
            }
            entries.Sort((a, b) => a.FullName.CompareTo(b.FullName));
            AvailableEntries = entries.ToArray();
        }
    }

    public class CbzPage : IPageFixed
    {
        private ZipArchiveEntry Content;
        public IPageOptions Option
        {
            get; set;
        }

        public CbzPage(ZipArchiveEntry entry)
        {
            Content = entry;
        }

        public async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync()
        {
            var s = Content.Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            s.Dispose();
            return await new Image.ImagePageStream(ms.AsRandomAccessStream()).GetBitmapAsync();
        }

        public async Task<bool> UpdateRequiredAsync()
        {
            return false;
        }

        public async Task SaveImageAsync(StorageFile file,uint width)
        {
            try {
                //if (!System.IO.File.Exists(file.Path))
                    Content.ExtractToFile(file.Path, true);
            }
            catch { }
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            var s = Content.Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            s.Dispose();
            await new Image.ImagePageStream(ms.AsRandomAccessStream()).SetBitmapAsync(image);
        }
    }

}
