using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Compression;
using Windows.UI.Xaml.Media;

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

        public event EventHandler Loaded;

        private void OnLoaded(EventArgs e)
        {
            if (Loaded != null) Loaded(this, e);
        }

        public IPage GetPage(uint i)
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
            string[] supportedFile = {".jpg",".jpeg",".gif",".png",".bmp",".tiff",".tif",".hdp",".wdp",".jxr"};//Not smart yet.
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

    public class CbzPage : IPage
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

        public async Task<ImageSource> GetImageSourceAsync()
        {
            var s = Content.Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            s.Dispose();
            return await new Image.ImagePageStream(ms.AsRandomAccessStream()).GetImageSourceAsync();
        }

        public async Task<bool> UpdateRequiredAsync()
        {
            return false;
        }
    }

}
