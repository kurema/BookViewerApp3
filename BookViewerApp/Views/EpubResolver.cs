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
using SharpCompress.Archives;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Email;
using BookViewerApp.Views.BrowserTools.DirectoryInfos;
using Microsoft.Toolkit.Uwp.UI.Controls;

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
	protected abstract Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri);

	public class GetContentInfo
	{
		public string MimetypeOverride = null;
	}

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

	protected static int ComparePath(string a, string b)
	{
		var a1 = a.AsSpan();
		var b1 = b.AsSpan();
		while (a1.Length > 0 && a1[0] is '\\' or '/') a1 = a1.Slice(1);
		while (b1.Length > 0 && b1[0] is '\\' or '/') b1 = b1.Slice(1);
		int result = 2;
		while (true)
		{
			if (a1.Length == 0 && b1.Length == 0) return result;
			var ai = a1.IndexOfAny('\\', '/');
			var bi = b1.IndexOfAny('\\', '/');
			var ac = ai >= 0 ? a1.Slice(0, ai) : a1;
			var bc = bi >= 0 ? b1.Slice(0, bi) : b1;
			if (ac.CompareTo(bc, StringComparison.InvariantCulture) == 0) { }
			else if (ac.CompareTo(bc, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				result = 1;
			}
			else return 0;
			if (ai < 0 && bi < 0) return result;
			a1 = ai >= 0 ? a1.Slice(ai + 1) : a1;
			b1 = bi >= 0 ? b1.Slice(bi + 1) : b1;
		}
	}

	public static async Task<InMemoryRandomAccessStream> ReadSharpCompressFile(IArchiveEntry entry, System.Threading.SemaphoreSlim semaphore)
	{
		if (semaphore is null) throw new ArgumentNullException(nameof(semaphore));
		if (entry is null) throw new ArgumentNullException(nameof(entry));
		await semaphore.WaitAsync();
		try
		{
			var stream = entry.OpenEntryStream();
			if (!stream.CanRead) throw new FileLoadException();
			var ms = new InMemoryRandomAccessStream
			{
				Size = (ulong)entry.Size
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
		////For security reason, we should deny access from other host.
		////But the problem is, sender.Source is not updated when first page is requested.
		//if (Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri uriSource))
		//{
		//    if (!string.IsNullOrEmpty(uriSource.Host) && !uriSource.Host.Equals(Host, StringComparison.InvariantCultureIgnoreCase)) return;
		//}
		{
			//Referer is easy option. But the problem is,
			//1. Referer may be disabled for privacy in the future.
			//2. First page do not have Referer but that's fine. Just ignore and continue.
			//3. If there's link to the PathHome, it's denied. It's unlikely to happen but against the intention.
			//But that's fine for now. And it looks like working fine. It's better than nothing.
			var hRef = args.Request.Headers.FirstOrDefault(a => a.Key == "Referer").Value;
			if (!string.IsNullOrEmpty(hRef) && Uri.TryCreate(hRef, UriKind.Absolute, out var uriRef))
			{
				if (!uriRef.Host.Equals(Host, StringComparison.InvariantCultureIgnoreCase)) return;
			}
		}
		if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uri) || !uri.Host.Equals(Host, StringComparison.InvariantCultureIgnoreCase)) return;
		var deferral = args.GetDeferral();
		IInputStream content;
		GetContentInfo info = null;
		try
		{
			(content, info) = await GetContentWithInfo(uri);
		}
		catch
		{
			content = null;
		}
		info ??= new();

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
				var mimetype = info.MimetypeOverride ?? MimeTypes.MimeTypeMap.GetMimeType(ext);
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

	protected override async Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri)
	{
		return (await GetContent(uri), new GetContentInfo());
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

	protected override async Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri)
	{
		return (await GetContent(uri), new GetContentInfo());
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

	protected override async Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri)
	{
		return (await GetContent(uri), new GetContentInfo());
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

	protected override async Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri)
	{
		return (await GetContent(uri), new GetContentInfo());
	}
}

public class GeneralResolverSharpCompress : EpubResolverBase
{
	private const string NotFoundHtml = "<html><body>Not found!</body></html>";

	//We only use SharpCompress because it's easy.
	private System.Threading.SemaphoreSlim SemaphoreReader = new(1, 1);

	private static string HtmlURLCache = null;

	public string Session { get; }

	public GeneralResolverSharpCompress(IArchive archive)
	{
		Archive = archive ?? throw new ArgumentNullException(nameof(archive));
		Session = Guid.NewGuid().ToString();
	}

	public override string Host { get; set; } = "cresolver.example";

	SharpCompress.Archives.IArchive Archive { get; set; }

	List<ArchiveWithInfo> EmbeddedArchives { get; } = new();

	class ArchiveWithInfo(IArchive archive, string path = null)
	{
		public string Path { get; } = path ?? string.Empty;
		public IArchive Archive { get; } = archive;

		public void Deconstruct(out IArchive archive, out string path) => (archive, path) = (this.Archive, this.Path);
	}

	class ArchiveEntryWithInfo(IArchive archive, IArchiveEntry entry, string path)
	{
		public IArchive Archive { get; } = archive;
		public IArchiveEntry Entry { get; } = entry;
		public string Path { get; } = path;

		public void Deconstruct(out IArchive archive, out IArchiveEntry entry, out string path) => (archive, entry, path) = (this.Archive, this.Entry, this.Path);

		string _KeyNormalized = null;

		public string KeyNormalized
		{
			get
			{
				if (_KeyNormalized is not null) return _KeyNormalized;
				string key = Entry.Key;
				if (!string.IsNullOrEmpty(Path)) key = System.IO.Path.Combine(Path, key);
				key = key.Replace("\\", "/");
				if (key.EndsWith('/')) key = key.Substring(0, key.Length - 1);
				return _KeyNormalized ??= key;
			}
		}
	}

	private async Task<InMemoryRandomAccessStream> StringToStream(string input)
	{
		var text = Encoding.UTF8.GetBytes(input);
		var ms = new InMemoryRandomAccessStream() { Size = (ulong)text.Length };
		await ms.WriteAsync(text.AsBuffer());
		return ms;
	}

	protected override async Task<(IInputStream Stream, GetContentInfo Info)> GetContentWithInfo(Uri uri)
	{
		if (uri is null) throw new Exception(InvalidPathMessage);

		var fn = Path.GetFileName(uri.LocalPath);
		var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
		var ui = q.Get("ui");
		var path = q.Get("path");

		//if (q.Get("session") != this.Session) goto native;
		switch (ui?.ToUpperInvariant())
		{
			case "INDEX": goto index;
			case "URL": goto url;
			case "NATIVE": goto native;
			case "BOOK": goto viewer;
		}

		if (string.IsNullOrEmpty(fn))
		{
			goto index;
		}
		goto native;

	url:;
		var arc = Archive.Entries.FirstOrDefault(a => ComparePath(a.Key, uri.LocalPath) != 0);
		if (arc is null) throw new Exception(InvalidPathMessage);
		await SemaphoreReader.WaitAsync();
		string text;
		try
		{
			using var s = arc.OpenEntryStream();
			using var sr1 = new StreamReader(s);
			text = await sr1.ReadToEndAsync();
		}
		finally
		{
			SemaphoreReader.Release();
		}
		{
			using var sr = new StringReader(text);
			var reg = new Regex(@"^URL\s*\=\s*(.+)$");
			bool isIs = false;

			while (true)
			{
				var t = await sr.ReadLineAsync();
				if (t == "[InternetShortcut]") isIs = true;
				if (t is null) break;
				if (!isIs) continue;
				var match = reg.Match(t);
				if (match.Success)
				{
					if (HtmlURLCache is null)
					{
						var st = (await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///res/archive_viewer/cushion.html"))).OpenReadAsync()).AsStream();
						using var str = new StreamReader(st, true);
						HtmlURLCache = await str.ReadToEndAsync();
					}
					var json = Views.BrowserTools.CushionInfos.Serialize.ToJson(new BrowserTools.CushionInfos.CushionInfo() { Content = text, Filename = fn, Url = match.Groups[1].Value });
					return (await StringToStream(HtmlURLCache.Replace("{info.json}", System.Web.HttpUtility.HtmlEncode(json))), new GetContentInfo() { MimetypeOverride = "text/html" });
				}
			}
		}
	native:;
		if (PathDictionary.TryGetValue(EliminateInitialSlash(uri.LocalPath), out var aewi))
		{
			return (await ReadSharpCompressFile(aewi.Entry, SemaphoreReader) ?? throw new Exception(InvalidPathMessage), new());
		}
		else
		{
			return (await StringToStream(NotFoundHtml), new GetContentInfo() { MimetypeOverride = "text/html" });
		}
	index:;
		return (await StringToStream(await GetIndexHtml(path ?? uri.LocalPath)), new GetContentInfo() { MimetypeOverride = "text/html" });
	viewer:;
		return (await StringToStream(await GetViewerHtml(uri.LocalPath)), new GetContentInfo() { MimetypeOverride = "text/html" });
	}

	SortedList<string, ArchiveEntryWithInfo> _PathMap = null;

	SortedList<string, ArchiveEntryWithInfo> PathDictionary
	{
		get
		{
			if (_PathMap is not null) return _PathMap;
			var dic = Archive.Entries.Select(a => new ArchiveEntryWithInfo(Archive, a, string.Empty))
				.Concat(EmbeddedArchives.SelectMany(a => a.Archive.Entries.Select(b => new ArchiveEntryWithInfo(a.Archive, b, a.Path))))
				.ToDictionary(a => a.KeyNormalized, a => a);
			try
			{
				return _PathMap = new SortedList<string, ArchiveEntryWithInfo>(dic, StringComparer.OrdinalIgnoreCase);
			}
			catch
			{
				return _PathMap = new SortedList<string, ArchiveEntryWithInfo>(dic, StringComparer.Ordinal);
			}
		}

		set => _PathMap = value;
	}

	static BrowserTools.DirectoryInfos.DirectoryInfo GetDirectoryInfo(IEnumerable<ArchiveEntryWithInfo> list, IEnumerable<string> additionalFolders)
	{
		return new BrowserTools.DirectoryInfos.DirectoryInfo
		{
			Entries = list.OrderBy(a => !a.Entry.IsDirectory).ThenBy(a => new Helper.NaturalSort.NaturalList(a.Entry.Key))
			.Select(aewi =>
			{
				var folder = Path.GetDirectoryName(aewi.KeyNormalized).Replace("\\", "/");
				if (folder != "" && !folder.StartsWith("/")) { folder = "/" + folder; }
				var entry = aewi.Entry;
				return new Entry()
				{
					IsFolder = entry.IsDirectory || (additionalFolders?.Contains(aewi.KeyNormalized) ?? false),
					Name = Path.GetFileName(aewi.KeyNormalized),
					Size = entry.IsDirectory ? null : entry.Size,
					Updated = entry.LastModifiedTime,
					Folder = folder
				};
			}).ToList(),
			//}).Concat(additionalFolders?.Select(a => new Entry()
			//{
			//	IsFolder = true,
			//	Name = Path.GetFileName(a),
			//	Size = null,
			//	Updated = null,
			//	Folder = EnsureInitialSlash(Path.GetDirectoryName(a).Replace("\\", "/"))
			//}
			//) ?? Array.Empty<Entry>()).ToList(),
			BasePath = "",
		};
	}

	protected override async Task<IInputStream> GetContent(Uri uri)
	{
		return (await GetContentWithInfo(uri)).Stream;
	}

	string HtmlIndexCache = null;
	string HtmlViewerCache = null;

	protected async Task<string> GetViewerHtml(string image)
	{
		if (HtmlViewerCache is null)
		{
			var st = (await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///res/archive_viewer/bookviewer.html"))).OpenReadAsync()).AsStream();
			using var str = new StreamReader(st, true);
			HtmlViewerCache = await str.ReadToEndAsync();
		}
		//Fix it to use correct archive!
		if (!PathDictionary.TryGetValue(EliminateInitialSlash(image), out var aewi)) return NotFoundHtml;
		var info = GetDirectoryInfo(PathDictionary.Select(a => a.Value).Where(a => a.Archive == aewi.Archive), null);
		info.RootName = "";
		info.CurrentDirectory = "";
		info.PreviewFile = (aewi.KeyNormalized.StartsWith("/") ? "" : "/") + aewi.KeyNormalized;
		//Security note: '<' is not valid character in Windows but I'm not sure about archive files. This is quick and dirty fix.
		var html = HtmlViewerCache.Replace("{info.json}", info.ToJson().Replace("</script>", "<\"+\"/script>", StringComparison.InvariantCultureIgnoreCase));

		return html;
	}

	static string EnsureInitialSlash(string text)
	{
		return text.StartsWith("/") ? text : $"/{text}";
	}

	static string EliminateInitialSlash(string text)
	{
		int h = 0;
		while (h < text.Length && text[h] == '/') h++;
		return h == 0 ? text : text.Substring(h);
	}


	void LoadArchives(string path)
	{
		if (path.Length == 0) return;
		path = EliminateInitialSlash(path);
		var paths = path.Split('/');
		var sb = new StringBuilder();
		var sb2 = new StringBuilder();
		var entries = Archive.Entries;
		foreach (var item in paths)
		{
			sb.Append(item);
			sb2.Append(item);
			if (Managers.BookManager.IsArchive(item))
			{
				var f = entries.FirstOrDefault(a => a.Key == sb.ToString());
				if (f is null) return;
				if (f.IsDirectory) continue;
				if (!LoadArchive(sb.ToString())) continue;
				sb.Clear();
				sb2.Append("/");
				continue;
			}
			sb.Append("/");
			sb2.Append("/");
		}
		return;
	}

	bool LoadArchive(string path)
	{
		path = EliminateInitialSlash(path);
		if (!PathDictionary.TryGetValue(path, out var aewi)) return false;
		if (EmbeddedArchives.Any(a => path.Equals(a.Path, StringComparison.InvariantCultureIgnoreCase))) return true;
		try
		{
			using var s = aewi.Entry.OpenEntryStream();
			var ms = new MemoryStream();
			s.CopyTo(ms);
			ms.Seek(0, SeekOrigin.Begin);
			var os = ArchiveFactory.Open(ms);
			EmbeddedArchives.Add(new ArchiveWithInfo(os, path));
			PathDictionary = null;
			return true;
		}
		catch
		{
			return false;
		}
	}

	protected async Task<string> GetIndexHtml(string folder)
	{
		if (folder.StartsWith("/")) folder = folder.Substring(1);
		LoadArchives(folder);
		if (HtmlIndexCache is null)
		{
			var st = (await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///res/archive_viewer/index.html"))).OpenReadAsync()).AsStream();
			using var str = new StreamReader(st, true);
			HtmlIndexCache = await str.ReadToEndAsync();
		}
		var info = GetDirectoryInfo(PathDictionary.Select(a => a.Value), EmbeddedArchives.Select(a => a.Path));
		info.RootName = "";
		info.CurrentDirectory = folder;
		//Security note: '<' is not valid character in Windows but I'm not sure about archive files. This is quick and dirty fix.
		var html = HtmlIndexCache.Replace("{info.json}", info.ToJson().Replace("</script>", "<\"+\"/script>", StringComparison.InvariantCultureIgnoreCase));

		return html;
	}
}
