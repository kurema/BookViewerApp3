using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Books.Cbz
{
    public class CbzBook : IBookFixed
    {
        public System.IO.Compression.ZipArchive Content { get; private set; }

        public uint PageCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPage GetPage(uint i)
        {
            throw new NotImplementedException();
        }

        public async Task LoadAsync(System.IO.Stream stream)
        {
            await Task.Run(() =>
            {
                Content = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
            }
            );
            throw new NotImplementedException();
        }
    }
}
