using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Books.Cbz
{
    public class CbzBook : IBookFixed
    {
        public System.IO.Compression.ZipArchive Content { get; private set; }
        public string[] AvailableEntries = new string[0];

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
            return new Image.ImagePageFileStream(Content.GetEntry(AvailableEntries[i]).Open());
        }

        public async Task LoadAsync(System.IO.Stream stream)
        {
            await Task.Run(() =>
            {
                Content = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
                OnLoaded(new EventArgs());
            }
            );
            throw new NotImplementedException();
        }
    }

}
