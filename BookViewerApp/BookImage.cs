using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;

namespace BookViewerApp.Books.Image
{
    public class ImagePageUrl : IPageUrl
    {
        public Uri Uri
        {
            get;private set;
        }

        public ImagePageUrl(Uri uri) { this.Uri = uri; }

        public async Task<ImageSource> GetImageSourceAsync()
        {
            return new Windows.UI.Xaml.Media.Imaging.BitmapImage(Uri);
        }
    }

    public class ImagePageFileStream : IPageFileStream
    {
        private Stream stream;

        public ImagePageFileStream(Stream stream)
        {
            this.stream = stream;
        }

        public void Close()
        {
        }

        public async Task<ImageSource> GetImageSourceAsync()
        {
            throw new NotImplementedException();
        }

        public Stream Open()
        {
            return stream;
        }
    }

    public class ImageBookUriCollection : IBookFixed
    {
        public Uri[] Content { get { return _Content; } private set { _Content = value; OnLoaded(new EventArgs()); } }
        private Uri[] _Content = new Uri[0];

        public uint PageCount
        {
            get
            {
                return (uint)Content.Count();
            }
        }

        public event EventHandler Loaded;

        private void OnLoaded(EventArgs e)
        {
            if (Loaded != null) Loaded(this, e);
        }

        public IPage GetPage(uint i)
        {
            return new ImagePageUrl(Content[i]);
        }
    }
}
