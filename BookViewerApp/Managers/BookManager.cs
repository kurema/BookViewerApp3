using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using BookViewerApp.Books;
using Windows.Storage;
using Windows.Storage.AccessCache;
using BookViewerApp.Storages;
using System.Threading;

namespace BookViewerApp.Managers;

public static class BookManager
{
	public static BookType? GetBookTypeByPath(string path)
	{
		if (path is null) { return null; }

		var ext = Path.GetExtension(path).ToUpperInvariant();
		switch (ext)
		{
			case ".PDF": return BookType.Pdf;
			case ".ZIP": case ".CBZ": return BookType.Zip;
			case ".RAR": case ".CBR": return BookType.Rar;
			case ".7Z": case ".CB7": return BookType.SevenZip;
			case ".EPUB": return BookType.Epub;
			default: return null;
		}
	}

	public static BookType? GetBookTypeByStream(Stream stream)
	{
		var buffer = new byte[64];
		stream.Read(buffer, 0, stream.Length < 64 ? (int)stream.Length : 64);
		stream.Close();
		stream.Dispose();

		if (buffer.Take(5).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d })) return BookType.Pdf;
		else if (buffer.Take(6).SequenceEqual(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C })) return BookType.SevenZip;
		else if (buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) ||
			buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x05, 0x06 }) ||
			buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x07, 0x08 })
			)
		{
			if (buffer.Skip(0x1e).Take(28).SequenceEqual(Encoding.ASCII.GetBytes("mimetypeapplication/epub+zip")))
			{
				return BookType.Epub;
			}
			return BookType.Zip;
		}
		else if (buffer.Take(7).SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00 })) return BookType.Rar;
		else if (buffer.Take(8).SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00 })) return BookType.Rar;

		return null;
	}

	public async static Task<BookType?> GetBookTypeByStorageFile(IStorageFile file)
	{
		if (file is null) return null;
		return GetBookTypeByPath(file.Path) ?? GetBookTypeByStream(await file.OpenStreamForReadAsync());
	}

	public static async Task<IBook> GetBookFromFile(IStorageFile file, Windows.UI.Xaml.XamlRoot xamlRoot = null, bool skipPasswordEntryPdf = false, CancellationTokenSource cancellationTokenSource = null)
	{
		var type = await GetBookTypeByStorageFile(file);
		if (type is null) return null;
		return await GetBookFromFile(file, (BookType)type, xamlRoot, skipPasswordEntryPdf, cancellationTokenSource);
	}


	public static async Task<IBook> GetBookPdf(Windows.Storage.Streams.IRandomAccessStream stream, string path, string fileName, Windows.UI.Xaml.XamlRoot xamlRoot = null, bool skipPasswordEntry = false, CancellationTokenSource cancellationTokenSource = null)
	{
		var book = new PdfBook();
		try
		{
			//We don't know ID of PDF before opening. Remembered passwords doesn't make sense. That's why I forgot to implement.
			//So for now, we try all remembered password for now.
			//It's not good for performance. But I don't think it takes only a moment.
			IEnumerable<string> allPw;
			{
				await PasswordDictionaryManager.LoadAsync();
				var id = Storages.PathStorage.GetIdFromPath(path);
				var infos = await Storages.BookInfoStorage.GetBookInfoAsync();
				var currentInfo = infos.FirstOrDefault(a => a.ID == id);
				var infoPaths = (currentInfo is null ? new Storages.BookInfoStorage.BookInfo[0] : new BookInfoStorage.BookInfo[] { currentInfo })
					.Concat(infos).Select(a => a.Password).Where(a => !string.IsNullOrEmpty(a)).Distinct().ToArray();
				if ((bool)Storages.SettingStorage.GetValue(Storages.SettingStorage.SettingKeys.PdfPasswordDictionaryAttack))
				{
					allPw = infoPaths.Concat(Managers.PasswordDictionaryManager.ListCombined);
				}
				else
				{
					allPw = infoPaths.Concat(Managers.PasswordDictionaryManager.ListCombinedBasic);
				}
				var a2 = allPw.ToArray();
			}

			await book.Load(stream, fileName, async (a) =>
			{
				if (skipPasswordEntry) throw new Exception("Password entry is skipped.");
				var dialog = new Views.PasswordRequestContentDialog() { XamlRoot = xamlRoot };
				if (!string.IsNullOrWhiteSpace(fileName)) dialog.Title = fileName;
				Windows.UI.Xaml.Controls.ContentDialogResult result;
				try
				{
					result = await dialog.ShowAsync();
				}
				catch
				{
					throw;
				}
				if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
				{
					return (dialog.Password, dialog.Remember);
				}
				else
				{
					throw new Exception();
				}
			}, async (message) =>
			{
				var dialog = new Windows.UI.Popups.MessageDialog(message, fileName);
				await dialog.ShowAsync();
				return;
			}, allPw, cancellationTokenSource, skipPasswordEntry);
		}
		catch { return null; }
		if (book.PageCount <= 0) { return null; }
		return book;
	}

	public static async Task<IBook> GetBookZip(Func<Task<Stream>> streamProvider)
	{
		var book = new CbzBook();
		try
		{
			await book.LoadAsync(streamProvider);
		}
		catch
		{
			return null;
		}
		if (book.PageCount <= 0) { return null; }
		return book;
	}

	public static async Task<IBook> GetBookSharpCompress(Func<Task<Stream>> streamProvider)
	{
		var book = new CompressedBook();
		try
		{
			await book.LoadAsync(streamProvider);
		}
		catch
		{
			return null;
		}
		if (book.PageCount <= 0) { return null; }
		return book;
	}

	public static async Task<IBook> GetBookFromFile(IStorageFile file, BookType type, Windows.UI.Xaml.XamlRoot xamlRoot = null, bool skipPasswordEntryPdf = false, CancellationTokenSource cancellationTokenSource = null)
	{
		switch (type)
		{
			case BookType.Epub: goto Epub;
			case BookType.Zip: goto Zip;
			case BookType.Rar: goto SharpCompress;
			case BookType.SevenZip: goto SharpCompress;
			case BookType.Pdf: goto Pdf;
		}


	Pdf:;
		return await GetBookPdf(await file.OpenReadAsync(), file.Path, file.Name, xamlRoot, skipPasswordEntryPdf, cancellationTokenSource);
	Zip:;
		return await GetBookZip(async () => (await file.OpenReadAsync()).AsStream());
	SharpCompress:;
		return await GetBookSharpCompress(async () => (await file.OpenReadAsync()).AsStream());
	Epub:;
		{
			return new BookEpub(file);
		}
	}

	public enum BookType
	{
		Epub, Zip, Pdf, Rar, SevenZip
	}

	public static string[] AvailableExtensionsArchive { get { return new string[] { ".pdf", ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7", ".epub" }; } }

	public static bool IsEpub(IStorageFile file)
	{
		if (file is null) return false;
		if (file.FileType.ToLowerInvariant() == ".epub") { return true; }
		return false;
	}

	public static bool IsEpub(string path) => path != null && Path.GetExtension(path).ToLowerInvariant() == ".epub";

	public static bool IsFileAvailabe(IStorageFile file)
	{
		return IsFileAvailabe(file.Path);
	}

	public static bool IsArchive(string path)
	{
		var ext = Path.GetExtension(path);
		switch (ext.ToUpperInvariant())
		{
			case ".ZIP":
			case ".CBZ":
			case ".TAR":
			case ".GZ":
			case ".RAR":
			case ".CBR":
			case "7z":
			case "cb7":
			case ".BZ2":
			case ".LZ":
			case ".XZ":
				return true;
			default: return false;
		}
	}

	public static bool IsFileAvailabe(string path)
	{
		if (path is null) return false;
		return AvailableExtensionsArchive.Contains(Path.GetExtension(path).ToLowerInvariant());
	}


	public static void StorageItemUnregister(string token)
	{
		var acl = StorageApplicationPermissions.FutureAccessList;
		acl.Remove(token);
	}

	public static string StorageItemRegister(IStorageItem file)
	{
		var acl = StorageApplicationPermissions.FutureAccessList;
		return acl.Add(file);
	}

	public static Dictionary<string, StorageFolder> GetApplicationFolders()
	{
		var current = ApplicationData.Current;
		return new Dictionary<string, StorageFolder>()
			{
				{ "{Special:" + nameof(current.LocalFolder) + "}",  current.LocalFolder },
				{ "{Special:" + nameof(current.LocalCacheFolder) + "}", current.LocalCacheFolder},
				{ "{Special:" + nameof(current.RoamingFolder) + "}", current.RoamingFolder },
				{ "{Special:" + nameof(current.TemporaryFolder) + "}", current.TemporaryFolder},
                //{ "{Special:" + nameof(current.SharedLocalFolder) + "}", current.SharedLocalFolder },
            };
	}

	public static async Task<IStorageItem> StorageItemGet(string token)
	{
		{
			var dic = GetApplicationFolders();
			if (dic.ContainsKey(token)) return dic[token];
		}

		var acl = StorageApplicationPermissions.FutureAccessList;
		try
		{
			if (acl.ContainsItem(token)) return await acl.GetItemAsync(token);
			else return null;
		}
		catch (FileNotFoundException)
		{
			//本来は削除するのが正しいかもしれないけど、共有フォルダがアクセスできないとか普通にあるので放置。
			return null;
		}
	}

	public static char FileSplitLetter { get { return Path.DirectorySeparatorChar; } }

	public static string PathJoin(string[] Path)
	{
		return string.Join(FileSplitLetter.ToString(), Path);
	}

	public static string[] PathSplit(string Path)
	{
		return Path.Split(FileSplitLetter);
	}

	public static async Task<IStorageItem> StorageItemGet(string token, string Path)
	{
		return await StorageItemGet(token, PathSplit(String.IsNullOrWhiteSpace(Path) ? "." : Path));
	}

	public static async Task<IStorageItem> StorageItemGet(string token, string[] Path)
	{
		IStorageItem currentFolder = await StorageItemGet(token);
		if (Path is null) return currentFolder;
		foreach (var item in Path)
		{
			if (currentFolder is null) return null;
			if (string.IsNullOrEmpty(item) || item.Trim() == ".") { }
			else if (currentFolder is StorageFolder f)
			{
				if (item.Trim() == "..") currentFolder = await f.GetParentAsync();
				else currentFolder = await f.TryGetItemAsync(item);
			}
			else
			{
				return null;
			}
		}
		return currentFolder;
	}

	public static async Task<StorageFile> PickFile()
	{
		var picker = new Windows.Storage.Pickers.FileOpenPicker();
		foreach (var ext in AvailableExtensionsArchive)
		{
			picker.FileTypeFilter.Add(ext);
		}
		return await picker.PickSingleFileAsync();
	}

	public static async Task<IBook> PickBook()
	{
		return (await GetBookFromFile(await PickFile()));
	}

	public async static Task<Storages.Library.libraryLibraryFolder> GetTokenFromPathOrRegister(IStorageItem file)
	{
		if (file is null) return null;
		return await GetTokenFromPath(file.Path) ?? new Storages.Library.libraryLibraryFolder()
		{
			token = StorageItemRegister(file),
			path = ""
		};
	}

	public async static Task<Storages.Library.libraryLibraryFolder> GetTokenFromPath(string path)
	{
		if (path is null) return null;

		path = Path.GetFullPath(path);
		//var tokens = (await Task.WhenAll(Content.Content.folders.Select(async a => KeyValuePair.Create(a, await a.GetStorageFolderAsync()))));//.ToDictionary(a => a.Key, a => a.Value);
		var acl = StorageApplicationPermissions.FutureAccessList;
		var tokens = (await Task.WhenAll(acl.Entries.Select(async a => (a.Token, await StorageItemGet(a.Token))))).Where(a => a.Item2 != null).ToList();
		tokens.AddRange(GetApplicationFolders().Select(a => (a.Key, a.Value as IStorageItem)));

		string currentPath = path;
		while (true)
		{
			foreach (var item in tokens)
			{
				if (item.Item2 != null && Path.GetRelativePath(item.Item2.Path, currentPath) == ".") return new Storages.Library.libraryLibraryFolder()
				{
					token = item.Token,
					path = Path.GetRelativePath(item.Item2.Path, path)
				};
			}
			currentPath = Path.GetDirectoryName(currentPath);
			if (String.IsNullOrEmpty(currentPath)) return null;
		}
	}

}
