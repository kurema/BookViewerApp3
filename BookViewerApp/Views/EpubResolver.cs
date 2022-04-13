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

public abstract class EpubResolverAbstract : Windows.Web.IUriToStreamResolver
{
    public event EventHandler Loaded;
    private void OnLoaded(EventArgs e)
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
}

public class EpubResolverZip : EpubResolverAbstract
{
    public EpubResolverZip(ZipArchive zip, string pathEpub, string pathReader, string pathReaderLocal, string pathHome)
    {
        Zip = zip ?? throw new ArgumentNullException(nameof(zip));
        PathEpub = pathEpub ?? throw new ArgumentNullException(nameof(pathEpub));
        PathReader = new System.Text.RegularExpressions.Regex(pathReader, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? throw new ArgumentNullException(nameof(pathReader));
        PathReaderLocal = pathReaderLocal ?? throw new ArgumentNullException(nameof(pathReaderLocal));
        PathHome = pathHome ?? throw new ArgumentNullException(nameof(pathHome));
    }

    public static async Task<EpubResolverZip> GetResolverBibi(IStorageFile file) => new EpubResolverZip(
        new ZipArchive((await file.OpenReadAsync()).AsStream())
        , "/bibi-bookshelf/book/", "^/bibi/", "ms-appx:///res/bibi/bibi/", "/bibi/index.html?book=book");

    public ZipArchive Zip { get; private set; }

    protected async override Task<IInputStream> GetContent(Uri uri)
    {
        var invalid = "Invalid Path";
        if (uri is null) throw new Exception(invalid);
        try
        {
            if (PathReader.IsMatch(uri.LocalPath))
            {
                var pathTail = PathReader.Replace(uri.LocalPath, "");
                pathTail = string.IsNullOrWhiteSpace(pathTail) ? "index.html" : pathTail;
                var f = await StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine(PathReaderLocal, pathTail)));
                return await f.OpenAsync(FileAccessMode.Read);
            }
            if (uri.LocalPath.ToLowerInvariant().StartsWith(PathEpub))
            {
                if (Zip is null) throw new NullReferenceException(nameof(Zip));
                string pathFile = uri.LocalPath.Substring(PathEpub.Length);
                string pathFileLower = uri.LocalPath.Substring(PathEpub.Length).ToLowerInvariant();
                // Does Zip has "abc.html" and "AbC.HtMl" entries at the same time. I don't know but just in case.
                var entry = Zip.Entries.FirstOrDefault(a => a.FullName == pathFile)
                    ?? Zip.Entries.FirstOrDefault(a => a.FullName.ToLowerInvariant() == pathFile);
                if (entry is null)
                {
                    // Should I avoid *.ttf file not exist error?
                    // It's easy. Just return random ttf file.
                    // No. Because the error only occur only when zip is decoded by javascript.
                    throw new Exception(invalid);
                }

                var stream = entry.Open();
                var ms = new InMemoryRandomAccessStream();
                var buf = new byte[entry.Length];
                await stream.ReadAsync(buf, 0, buf.Length);
                stream.Close();
                stream.Dispose();
                await ms.WriteAsync(buf.AsBuffer());
                ms.Seek(0);

                return ms;
                //using (var stream = entry.Open())
                //{
                //    int lenBuf = 4096;
                //    var buf = new byte[lenBuf];
                //    var ms = new InMemoryRandomAccessStream();
                //    while (stream.CanRead)
                //    {
                //        int len = await stream.ReadAsync(buf, 0, lenBuf);
                //        await ms.WriteAsync(buf.AsBuffer(0, len, len));
                //    }

                //    //ms.Seek(0);
                //    //using (var s = ms.AsStream())
                //    //using (var sr = new StreamReader(s))
                //    //{
                //    //    var stt = sr.ReadToEnd();
                //    //}

                //    ms.Seek(0);

                //    return ms;
                //}
            }
            throw new Exception(invalid);
        }
        catch (Exception) { throw; }
    }
}

public class EpubResolverFile : EpubResolverAbstract
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

    public static EpubResolverFile GetResolverBasic(IStorageFile file) => new(file, "/contents/book.epub", "^/reader/", "ms-appx:///res/reader/", "/reader/index.html");
    public static EpubResolverFile GetResolverBibi(IStorageFile file) => new(file, "/bibi-bookshelf/book.epub", "^/bibi/", "ms-appx:///res/bibi/bibi/", "/bibi/index.html?book=book.epub");

    protected override async Task<IInputStream> GetContent(Uri uri)
    {
        var invalid = "Invalid Path";
        if (uri is null) throw new Exception(invalid);
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
                if (File is null) throw new Exception(invalid);
                return await File.OpenReadAsync();
            }
            throw new Exception(invalid);
        }
        catch (Exception) { throw; }
    }
}

