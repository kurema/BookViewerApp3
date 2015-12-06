using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows;
using pdf=Windows.Data.Pdf;

namespace BookViewerApp.Books.Pdf
{
    public class PdfBook : IBookFixed
    {
        public pdf.PdfDocument Content { get; private set; }
        private bool PageLoaded = false;

        public uint PageCount
        {
            get
            {
                if (PageLoaded) return Content.PageCount;
                else return 0;
            }
        }

        public IPage GetPage(uint i)
        {
            if (i < PageCount) return new PdfPage(Content.GetPage(i));
            else throw new Exception("Incorrect page.");//ToDo:Implement Exception.
        }

        public async Task Load(Windows.Storage.IStorageFile file)
        {
            Content = await pdf.PdfDocument.LoadFromFileAsync(file);
            PageLoaded = true;
        }
    }

    public class PdfPage : IPageStream
    {
        public pdf.PdfPage Content { get; private set; }
        public PdfPage(pdf.PdfPage page)
        {
            Content = page;
        }

        public async Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            await Content.RenderToStreamAsync(stream);
        }

        public async Task PreparePageAsync()
        {
            await Content.PreparePageAsync();
        }
    }
}
