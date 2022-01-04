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

namespace BookViewerApp.Views;

public class EpubResolver : Windows.Web.IUriToStreamResolver
{
    public EpubResolver(IStorageFile file, string pathEpub, string pathReader, string pathReaderLocal, string pathHome)
    {
        File = file ?? throw new ArgumentNullException(nameof(file));
        PathEpub = pathEpub ?? throw new ArgumentNullException(nameof(pathEpub));
        PathReader = new System.Text.RegularExpressions.Regex(pathReader, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? throw new ArgumentNullException(nameof(pathReader));
        PathReaderLocal = pathReaderLocal ?? throw new ArgumentNullException(nameof(pathReaderLocal));
        PathHome = pathHome ?? throw new ArgumentNullException(nameof(pathHome));
    }

    //This is why you can't decode zip.
    //https://social.msdn.microsoft.com/Forums/Windowsapps/en-US/28dfbf3e-6fb2-4f6a-b898-d9c361bb2c70/iuritostreamresolveruritostreamasync-invalidcastexception-in-tasktoasyncoperationwithprogress?forum=winappswithcsharp
    //https://stackoverflow.com/questions/59185615/how-to-make-a-custom-response-to-my-webview-with-a-iuritostreamresolver


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

    public string PathEpub { get; }
    public System.Text.RegularExpressions.Regex PathReader { get; }

    public string PathReaderLocal { get; }

    public string PathHome { get; }

    public static EpubResolver GetResolverBasic(IStorageFile file) => new(file, "/contents/book.epub", "^/reader/", "ms-appx:///res/reader/", "/reader/index.html");
    public static EpubResolver GetResolverBibi(IStorageFile file) => new(file, "/bibi-bookshelf/book.epub", "^/bibi/", "ms-appx:///res/bibi/bibi/", "/bibi/index.html?book=book.epub");

    protected async Task<IInputStream> GetContent(Uri uri)
    {
        var invalid = new Exception("Invalid Path");
        if (uri is null) throw invalid;
        try
        {
            //Security!
            if (PathReader.IsMatch(uri.LocalPath))
            {
                var pathTail = PathReader.Replace(uri.LocalPath, "");
                pathTail = string.IsNullOrWhiteSpace(pathTail) ? "index.html" : pathTail;
                var f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine(PathReaderLocal, pathTail)));
                return await f.OpenAsync(FileAccessMode.Read);
            }
            if (uri.LocalPath.ToLowerInvariant() == PathEpub)
            {
                if (File is null) throw invalid;
                return await File.OpenReadAsync();
            }
            throw invalid;
        }
        catch (Exception) { throw invalid; }
    }
}
