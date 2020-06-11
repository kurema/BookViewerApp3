﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using BookViewerApp.Books;

namespace BookViewerApp.Managers
{
    public class BookManager
    {
        public static BookType? GetBookTypeByPath(string path)
        {
            if (path == null) { return null; }

            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".pdf": return BookType.Pdf;
                case ".zip": case ".cbz": return BookType.Zip;
                case ".rar": case ".cbr": return BookType.Rar;
                case ".7z": case ".cb7": return BookType.SevenZip;
                case ".epub": return BookType.Epub;
                default: return null;
            }
        }

        public static BookType? GetBookTypeByStream(Stream stream)
        {
            var buffer = new byte[64];
            stream.Read(buffer, 0, stream.Length < 64 ? (int)stream.Length : 64);
            stream.Close();

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

        public static async Task<IBook> GetBookFromFile(Windows.Storage.IStorageFile file)
        {
            if (file == null) return null;
            var type = GetBookTypeByPath(file.Path) ?? GetBookTypeByStream(await file.OpenStreamForReadAsync());
            switch (type)
            {
                case BookType.Epub: goto Epub;
                case BookType.Zip: goto Zip;
                case BookType.Rar: goto SharpCompress;
                case BookType.SevenZip: goto SharpCompress;
                case BookType.Pdf: goto Pdf;
                default: return null;
            }


        Pdf:;
            {
                var book = new PdfBook();
                try
                {
                    await book.Load(file, async (a) =>
                    {
                        var dialog = new Views.PasswordRequestContentDialog();
                        var result = await dialog.ShowAsync();
                        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                        {
                            return (dialog.Password, dialog.Remember);
                        }
                        else
                        {
                            throw new Exception();
                        }

                    });
                }
                catch { return null; }
                if (book.PageCount <= 0) { return null; }
                return book;
            }
        Zip:;
            {
                var book = new CbzBook();
                try
                {
                    await book.LoadAsync((await file.OpenReadAsync()).AsStream());
                }
                catch
                {
                    return null;
                }
                if (book.PageCount <= 0) { return null; }
                return book;
            }
        SharpCompress:;
            {
                var book = new CompressedBook();
                try
                {
                    await book.LoadAsync((await file.OpenReadAsync()).AsStream());
                }
                catch
                {
                    return null;
                }
                if (book.PageCount <= 0) { return null; }
                return book;
            }
        Epub:;
            {
                return new BookEpub() { File = file };
            }
        }

        public enum BookType
        {
            Epub, Zip, Pdf, Rar, SevenZip
        }

        public static string[] AvailableExtensionsArchive { get { return new string[] { ".pdf", ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7", ".epub" }; } }

        public static string[] AvailableExtensionsImage { get { return new string[] { ".jpg", ".jpeg", ".gif", ".png", ".bmp", ".tiff", ".tif", ".hdp", ".wdp", ".jxr" }; } }

        public static bool IsEpub(Windows.Storage.IStorageFile file)
        {
            if (file.FileType.ToLower() == ".epub") { return true; }
            return false;
        }


        public static bool IsFileAvailabe(Windows.Storage.IStorageFile file)
        {
            return AvailableExtensionsArchive.Contains(Path.GetExtension(file.Path).ToLower());
        }

        public static void StorageItemUnregister(string token)
        {
            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            acl.Remove(token);
        }

        public static string StorageItemRegister(Windows.Storage.IStorageItem file)
        {
            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            return acl.Add(file);
        }

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string token)
        {
            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
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

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string token, string Path)
        {
            return await StorageItemGet(token, PathSplit(String.IsNullOrWhiteSpace(Path) ? "." : Path));
        }

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string token, string[] Path)
        {
            Windows.Storage.IStorageItem currentFolder = await StorageItemGet(token);
            if (Path == null) return currentFolder;
            foreach (var item in Path)
            {
                if (currentFolder == null) return null;
                if (string.IsNullOrEmpty(item) || item.Trim() == ".") { }
                else if (currentFolder is Windows.Storage.StorageFolder f)
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

        public static async Task<Windows.Storage.StorageFile> PickFile()
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

        public async static Task<Storages.Library.libraryLibraryFolder> GetTokenFromPathOrRegister(Windows.Storage.IStorageItem file)
        {
            if (file == null) return null;
            return await Managers.BookManager.GetTokenFromPath(file.Path) ?? new Storages.Library.libraryLibraryFolder()
            {
                token = Managers.BookManager.StorageItemRegister(file),
                path = ""
            };
        }

        public async static Task<Storages.Library.libraryLibraryFolder> GetTokenFromPath(string path)
        {
            if (path == null) return null;

            path = Path.GetFullPath(path);
            //var tokens = (await Task.WhenAll(Content.Content.folders.Select(async a => KeyValuePair.Create(a, await a.GetStorageFolderAsync()))));//.ToDictionary(a => a.Key, a => a.Value);
            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            var tokens = (await Task.WhenAll(acl.Entries.Select(async a => (a.Token, await StorageItemGet(a.Token))))).Where(a => a.Item2 != null);

            string currentPath = path;
            while (true)
            {
                foreach (var item in tokens)
                {
                    if (Path.GetRelativePath(item.Item2.Path, currentPath) == ".") return new Storages.Library.libraryLibraryFolder()
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
}
