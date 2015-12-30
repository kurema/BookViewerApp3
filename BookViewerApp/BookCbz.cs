using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Compression;

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
            var s = AvailableEntries[i].Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return new Image.ImagePageStream(ms.AsRandomAccessStream());
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
            string[] supportedFile = {".jpg",".jpeg",".gif",".png" };//fix me!
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
            AvailableEntries = entries.ToArray();
        }
    }

}
