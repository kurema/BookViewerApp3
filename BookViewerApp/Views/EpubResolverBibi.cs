using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.IO;
using Windows.Storage;

namespace BookViewerApp.Views
{
    public class EpubResolverBibi : Windows.Web.IUriToStreamResolver
    {
        //This is why you can't decode zip.
        //https://social.msdn.microsoft.com/Forums/Windowsapps/en-US/28dfbf3e-6fb2-4f6a-b898-d9c361bb2c70/iuritostreamresolveruritostreamasync-invalidcastexception-in-tasktoasyncoperationwithprogress?forum=winappswithcsharp
        //https://stackoverflow.com/questions/59185615/how-to-make-a-custom-response-to-my-webview-with-a-iuritostreamresolver

        public EpubResolverBibi(IStorageFile file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public event EventHandler Loaded;
        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public IStorageFile File { get; private set; }

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
                if (uri.LocalPath.ToLower().StartsWith("/bibi/"))
                {
                    var pathTail = uri.LocalPath.Replace("/bibi/", "", StringComparison.OrdinalIgnoreCase);
                    pathTail = string.IsNullOrWhiteSpace(pathTail) ? "index.html" : pathTail;
                    var f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine("ms-appx:///res/bibi/bibi/", pathTail)));
                    return await f.OpenReadAsync();
                }
                else if (uri.LocalPath.ToLower() == "/bibi-bookshelf/book.epub")
                {
                    if (File == null) throw invalid;
                    return await File.OpenReadAsync();
                }
                throw invalid;
            }
            catch (Exception) { throw invalid; }
        }
    }

}
