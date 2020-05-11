using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

using BookViewerApp.Helper;


namespace BookViewerApp.Books
{
    public class ImagePageUrl : IPageFixed
    {
        public Uri Uri
        {
            get; private set;
        }

        public IPageOptions Option
        {
            get; set;
        }

        public ImagePageUrl(Uri uri) { Uri = uri; }

        public Task<BitmapImage> GetBitmapAsync()
        {
            return Task.FromResult(new BitmapImage(Uri));
        }

        public Task<bool> UpdateRequiredAsync()
        {
            return Task.FromResult(false);
        }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            //ToDo: Fix me!
            if (Uri.IsFile)
            {
                var thm = await (await StorageFile.GetFileFromApplicationUriAsync(Uri)).GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView);

                await Functions.SaveStreamToFile(thm, file);
            }
        }

        public Task SetBitmapAsync(BitmapImage image)
        {
            image.UriSource = Uri;
            return Task.CompletedTask;
        }
    }
}

namespace BookViewerApp.Books
{
    public class ImagePageStream : IPageFixed
    {
        private IRandomAccessStream stream;

        public ImagePageStream(IRandomAccessStream stream)
        {
            this.stream = stream;
        }

        public IPageOptions Option
        {
            get; set;
        }

        public Task<BitmapImage> GetBitmapAsync()
        {
            var image = new BitmapImage();
            stream.Seek(0);
            image?.SetSource(stream);
            return Task.FromResult(image);
        }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            await Functions.SaveStreamToFile(stream, file);
        }

        public Task SetBitmapAsync(BitmapImage image)
        {
            stream.Seek(0);
            image?.SetSource(stream);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateRequiredAsync()
        {
            return Task.FromResult(false);
        }
    }
}

namespace BookViewerApp.Books
{
    public class ImageBookUriCollection : IBookFixed
    {
        public Uri[] Content { get { return _Content; } private set { _Content = value; OnLoaded(new EventArgs()); } }
        private Uri[] _Content = new Uri[0];

        public ImageBookUriCollection(params Uri[] uri)
        {
            Content = uri;
        }

        public ImageBookUriCollection(params string[] uri)
        {
            var result = new Uri[uri.Length];
            for (int i = 0; i < uri.Length; i++)
            {
                result[i] = new Uri(uri[i]);
            }
            Content = result;
        }

        public uint PageCount => (uint)(Content?.Length ?? 0);

        public string ID
        {
            get
            {
                string result = "";
                foreach (var item in Content)
                {
                    result += "\"" + item.GetHashCode() + "\"";
                }
                return Functions.GetHash(result);
            }
        }

        public event EventHandler Loaded;

        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public IPageFixed GetPage(uint i)
        {
            return new ImagePageUrl(Content[i]);
        }
    }
}
