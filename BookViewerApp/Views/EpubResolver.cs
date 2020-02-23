using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

using System.IO.Compression;
using System.IO;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;

namespace BookViewerApp.Views
{
    public class EpubResolver : Windows.Web.IUriToStreamResolver
    {
        //This is why you can't decode zip.
        //https://social.msdn.microsoft.com/Forums/Windowsapps/en-US/28dfbf3e-6fb2-4f6a-b898-d9c361bb2c70/iuritostreamresolveruritostreamasync-invalidcastexception-in-tasktoasyncoperationwithprogress?forum=winappswithcsharp
        //https://stackoverflow.com/questions/59185615/how-to-make-a-custom-response-to-my-webview-with-a-iuritostreamresolver

        public EpubResolver(StorageFile file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public event EventHandler Loaded;
        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public StorageFile File { get; private set; }

        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            return GetContent(uri).AsAsyncOperation();
        }

        protected async Task<IInputStream> GetContent(Uri uri)
        {
            var invalid = new Exception("Invalid Path");
            if (uri == null) throw invalid;
            try
            {
                //Security!
                if (uri.LocalPath.ToLower().StartsWith("/reader/"))
                {
                    var pathTail = uri.LocalPath.Replace("/reader/", "", StringComparison.OrdinalIgnoreCase);
                    var f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine("ms-appx:///res/reader/", pathTail)));
                    return await f.OpenAsync(FileAccessMode.Read);
                }
                if (uri.LocalPath.ToLower().StartsWith("/contents/"))
                {
                    var pathTail = uri.LocalPath.Replace("/contents/book.epub", "", StringComparison.OrdinalIgnoreCase);
                    if (File == null) throw invalid;
                    return await File.OpenReadAsync();
                }
                throw invalid;
            }
            catch (Exception) { throw invalid; }
        }
    }
}
