using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows;
using Windows.UI.Xaml.Media;
using pdf = Windows.Data.Pdf;

namespace BookViewerApp.Books.Pdf
{
    public class PdfBook : IBookFixed
    {
        public pdf.PdfDocument Content { get; private set; }
        private bool PageLoaded = false;

        public event EventHandler Loaded;

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
            OnLoaded(new EventArgs());
            PageLoaded = true;
        }

        private void OnLoaded(EventArgs e)
        {
            if (Loaded != null) Loaded(this, e);
        }

    }

    public class PdfPage : IPage
    {
        public pdf.PdfPage Content { get; private set; }

        public IPageOptions Option
        {
            get; set;
        }

        public PdfPage(pdf.PdfPage page)
        {
            Content = page;
        }

        public async Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            if (Option != null)
            {
                var pdfOption = new pdf.PdfPageRenderOptions();
                
                if (Option.TargetHeight/Content.Size.Height < Option.TargetWidth/Content.Size.Width)
                {
                    pdfOption.DestinationHeight = (uint)Option.TargetHeight;
                }
                else {
                    pdfOption.DestinationWidth = (uint)Option.TargetWidth;
                }
                await Content.RenderToStreamAsync(stream,pdfOption);
            }
            else
            {
                await Content.RenderToStreamAsync(stream);
            }
        }

        public async Task PreparePageAsync()
        {
            await Content.PreparePageAsync();
        }

        public async Task<ImageSource> GetImageSourceAsync()
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            await result.SetSourceAsync(stream);
            return result;
        }
    }
}
