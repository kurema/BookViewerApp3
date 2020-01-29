using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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

        public string ID
        {
            get;private set;
        }

        public IPageFixed GetPage(uint i)
        {
            if (i < PageCount) return new PdfPage(Content.GetPage(i));
            else throw new Exception("Incorrect page.");//ToDo:Implement Exception.
        }

        public async Task Load(Windows.Storage.IStorageFile file)
        {
            Content = await pdf.PdfDocument.LoadFromFileAsync(file);
            OnLoaded(new EventArgs());
            PageLoaded = true;
            ID = Functions.CombineStringAndDouble(file.Name, Content.PageCount);
        }

        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

    }

    public class PdfPage : IPageFixed
    {
        public pdf.PdfPage Content { get; private set; }

        public IPageOptions Option
        {
            get; set;
        }
        public IPageOptions LastOption;

        public PdfPage(pdf.PdfPage page)
        {
            Content = page;
        }

        public async Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            if (Option != null)
            {
                //Strange code. Maybe fix needed.
                if (Option is PageOptionsControl)
                {
                    LastOption = (PageOptions)((PageOptionsControl)this.Option);
                }
                else { LastOption = Option; }

                var pdfOption = new pdf.PdfPageRenderOptions();
                if (Option.TargetHeight/Content.Size.Height < Option.TargetWidth/Content.Size.Width)
                {
                    pdfOption.DestinationHeight = (uint)Option.TargetHeight*2;
                }
                else {
                    pdfOption.DestinationWidth = (uint)Option.TargetWidth*2;
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

        public async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync()
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            await result.SetSourceAsync(stream);
            return result;
        }

        public async Task<bool> UpdateRequiredAsync()
        {
            if (LastOption != null && Option != null && LastOption.TargetHeight < Option.TargetHeight || LastOption.TargetWidth < Option.TargetWidth)
                //if (LastOption != null && Option != null)
            { return true; }
            else { return false; }
        }

        public async Task SaveImageAsync(StorageFile file,uint width)
        {
            var pdfOption = new pdf.PdfPageRenderOptions();
            pdfOption.DestinationWidth = width;
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await Content.RenderToStreamAsync(stream, pdfOption);
            await Functions.SaveStreamToFile(stream, file);
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            await image.SetSourceAsync(stream);
        }
    }
}
