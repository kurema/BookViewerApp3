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

public abstract class EpubResolverBase : Windows.Web.IUriToStreamResolver
{
    public const string InvalidPathMessage = "404 Not Found: The path is invalid.";

    //Used for WebView2
    public virtual string Host { get; set; } = "resolver.example";

    public string GetUri(string path) => $"http://{Host}{path}";

    public event EventHandler Loaded;
    protected void OnLoaded(EventArgs e)
    {
        Loaded?.Invoke(this, e);
    }

    public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
    {
        return GetContent(uri).AsAsyncOperation();
    }

    public string PathEpub { get; protected set; }
    public System.Text.RegularExpressions.Regex PathReader { get; protected set; }

    public string PathReaderLocal { get; protected set; }

    public string PathHome { get; protected set; }

    protected abstract Task<IInputStream> GetContent(Uri uri);

    public static async Task<EpubResolverZip> GetResolverBibiZip(IStorageFile file)
        => new EpubResolverZip(new ZipArchive((await file.OpenReadAsync()).AsStream()), "/bibi-bookshelf/book/", "^/bibi/", "ms-appx:///res/bibi/bibi/", "/bibi/index.html?book=book");
    public static EpubResolverFile GetResolverBasicFile(IStorageFile file) => new(file, "/contents/book.epub", "^/reader/", "ms-appx:///res/reader/", "/reader/index.html");
    public static EpubResolverFile GetResolverBibiFile(IStorageFile file) => new(file, "/bibi-bookshelf/book.epub", "^/bibi/", "ms-appx:///res/bibi/bibi/", "/bibi/index.html?book=book.epub");

    //Pdf.js do not work on edgeHTML.
    public static async Task<EpubResolverStorageAndZip> GetResolverPdfJs(IStorageFile file)
        => new EpubResolverStorageAndZip(file, new ZipArchive((await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///res/pdfjs/pdfjs.zip"))).OpenReadAsync()).AsStream())
            , "/book.pdf", "^/pdfjs/", "/pdfjs/web/viewer.html?file=/book.pdf");


    public async Task<IInputStream> GetContentBasic(Uri uri, Func<string, Task<IInputStream>> actionLocal, Func<Task<IInputStream>> actionZip)
    {
        if (uri is null) throw new Exception(InvalidPathMessage);
        if (PathReader.IsMatch(uri.LocalPath))
        {
            var pathTail = PathReader.Replace(uri.LocalPath, "");
            pathTail = string.IsNullOrWhiteSpace(pathTail) ? "index.html" : pathTail;
            return await actionLocal(pathTail);
        }
        if (uri.LocalPath.ToLowerInvariant().StartsWith(PathEpub))
        {
            return await actionZip();
        }
        throw new Exception(InvalidPathMessage);
    }

    public static async Task<InMemoryRandomAccessStream> ReadZipFile(ZipArchive zip, string pathFile, System.Threading.SemaphoreSlim semaphore)
    {
        if (zip is null) throw new ArgumentNullException(nameof(zip));
        if (semaphore is null) throw new ArgumentNullException(nameof(semaphore));
        string pathFileLower = pathFile.ToLowerInvariant();
        var entry = zip.Entries.FirstOrDefault(a => a.FullName == pathFile) ?? zip.Entries.FirstOrDefault(a => a.FullName.ToLowerInvariant() == pathFileLower);
        if (entry is null)
        {
            throw new Exception(InvalidPathMessage);
        }
        // Note:
        // 1. At first I was like
        //    ```cs
        //    await stream.ReadAsync(buf, 0, buf.Length);
        //    await ms.WriteAsync(buf.AsBuffer());
        //    ```
        //    but some image seems to be corrupt, which means loading is not complete.
        // 2. Then I did ``while(true){}``. Then some image are complete and others are not.
        // 3. Lastly I use Semaphore. It's good now.
        //    It seems only one thread can access same zip file at one time.
        //    Cons: Progress (like "3/52 Items Loaded.") does not display correctly. But I ignore. 
        await semaphore.WaitAsync();
        try
        {
            var stream = entry.Open();
            if (!stream.CanRead) throw new FileLoadException();
            var ms = new InMemoryRandomAccessStream
            {
                Size = (ulong)entry.Length
            };
            ms.Seek(0);
            const int bufSize = 4096;
            var buf = new byte[bufSize];
            while (true)
            {
                int len = await stream.ReadAsync(buf, 0, bufSize);
                if (len <= 0) break;
                await ms.WriteAsync(buf.AsBuffer(0, len));
            }
            stream.Close();
            stream.Dispose();
            ms.Seek(0);
            return ms;
        }
        finally
        {
            semaphore.Release();
        }
    }

    //private System.Threading.SemaphoreSlim SemaphoreWebResource = new(1, 1);

    public async void WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uri) || uri.Host.ToUpperInvariant() != Host.ToUpperInvariant()) return;
        var deferral = args.GetDeferral();
        IInputStream content;
        try
        {
            content = await GetContent(uri);
        }
        catch
        {
            content = null;
        }

        if (content is null)
        {
            try
            {
                args.Response = sender.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
            }
            catch { }
            finally
            {
                deferral.Complete();
            }
            return;
        }
        else if (content is IRandomAccessStream random)
        {
            try
            {
                var ext = Path.GetExtension(uri.LocalPath);
                var mimetype = MimeTypes.MimeTypeMap.GetMimeType(ext);
                var header = new StringBuilder();
                if (!string.IsNullOrEmpty(mimetype) && !string.IsNullOrEmpty(ext)) header.Append($"Content-Type: {mimetype}");
                args.Response = sender.Environment.CreateWebResourceResponse(random, 200, "OK", header.ToString());
            }
            catch { }
            finally
            {
                deferral.Complete();
            }
            return;
        }
        else
        {
            throw new NotImplementedException();
            //const int bufSize = 4096;
            //var buffer = new Windows.Storage.Streams.Buffer(bufSize);
            //while (true)
            //{
            //    await content.ReadAsync(buffer, bufSize, InputStreamOptions.ReadAhead);
            //}
        }
    }
}


public class EpubResolverZip : EpubResolverBase
{
    public EpubResolverZip(ZipArchive zip, string pathEpub, string pathReader, string pathReaderLocal, string pathHome)
    {
        Zip = zip ?? throw new ArgumentNullException(nameof(zip));
        PathEpub = pathEpub ?? throw new ArgumentNullException(nameof(pathEpub));
        PathReader = new System.Text.RegularExpressions.Regex(pathReader, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? throw new ArgumentNullException(nameof(pathReader));
        PathReaderLocal = pathReaderLocal ?? throw new ArgumentNullException(nameof(pathReaderLocal));
        PathHome = pathHome ?? throw new ArgumentNullException(nameof(pathHome));
    }

    public ZipArchive Zip { get; private set; }

    private System.Threading.SemaphoreSlim Semaphore = new(1, 1);

    protected async override Task<IInputStream> GetContent(Uri uri)
    {
        return await GetContentBasic(uri, async (pathTail) =>
        {
            var f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine(PathReaderLocal, pathTail)));
            return await f.OpenAsync(FileAccessMode.Read);
        }, async () =>
        {
            string pathFile = uri.LocalPath.Substring(PathEpub.Length);
            return await ReadZipFile(Zip, pathFile, Semaphore);
        }
        );
    }
}

public class EpubResolverZipDouble : EpubResolverBase
{
    public EpubResolverZipDouble(ZipArchive zip, ZipArchive zipReader, string pathEpub, string pathReader, string pathHome)
    {
        Zip = zip ?? throw new ArgumentNullException(nameof(zip));
        ZipReader = zipReader ?? throw new ArgumentNullException(nameof(zipReader));
        PathEpub = pathEpub ?? throw new ArgumentNullException(nameof(pathEpub));
        PathReader = new System.Text.RegularExpressions.Regex(pathReader, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? throw new ArgumentNullException(nameof(pathReader));
        PathReaderLocal = "";
        PathHome = pathHome ?? throw new ArgumentNullException(nameof(pathHome));
    }

    public ZipArchive Zip { get; private set; }
    public ZipArchive ZipReader { get; private set; }

    private System.Threading.SemaphoreSlim Semaphore = new(1, 1);
    private System.Threading.SemaphoreSlim SemaphoreReader = new(1, 1);

    protected async override Task<IInputStream> GetContent(Uri uri)
    {
        return await GetContentBasic(uri, async (pathTail) =>
        {
            return await ReadZipFile(ZipReader, pathTail, SemaphoreReader);
        }, async () =>
        {
            string pathFile = uri.LocalPath.Substring(PathEpub.Length);
            return await ReadZipFile(Zip, pathFile, Semaphore);
        }
        );
    }
}

public class EpubResolverStorageAndZip : EpubResolverBase
{
    public EpubResolverStorageAndZip(IStorageFile file, ZipArchive zipReader, string pathEpub, string pathReader, string pathHome)
    {
        File = file ?? throw new ArgumentNullException(nameof(file));
        ZipReader = zipReader ?? throw new ArgumentNullException(nameof(zipReader));
        PathEpub = pathEpub ?? throw new ArgumentNullException(nameof(pathEpub));
        PathReader = new System.Text.RegularExpressions.Regex(pathReader, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? throw new ArgumentNullException(nameof(pathReader));
        PathReaderLocal = "";
        PathHome = pathHome ?? throw new ArgumentNullException(nameof(pathHome));
    }

    public IStorageFile File { get; private set; }
    public ZipArchive ZipReader { get; private set; }

    private System.Threading.SemaphoreSlim SemaphoreReader = new(1, 1);

    protected async override Task<IInputStream> GetContent(Uri uri)
    {
        return await GetContentBasic(uri, async (pathTail) =>
        {
            return await ReadZipFile(ZipReader, pathTail, SemaphoreReader);
        }, async () =>
        {
            return await File.OpenReadAsync();
        }
        );
    }
}

public class EpubResolverFile : EpubResolverBase
{
    public EpubResolverFile(IStorageFile file, string pathEpub, string pathReader, string pathReaderLocal, string pathHome)
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

    public IStorageFile File { get; private set; }

    protected override async Task<IInputStream> GetContent(Uri uri)
    {
        if (uri is null) throw new Exception(InvalidPathMessage);
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
                if (File is null) throw new Exception(InvalidPathMessage);
                return await File.OpenReadAsync();
            }
            throw new Exception(InvalidPathMessage);
        }
        catch (Exception) { throw; }
    }
}

