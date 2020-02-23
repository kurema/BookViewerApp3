using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace BookViewerApp.Books
{
    public class BookManager
    {
        public static async Task<(IBook Book,bool IsEpub)> GetBookFromFile(Windows.Storage.IStorageFile file)
        {
            if (file == null) { return (null,false); }
            else if (Path.GetExtension(file.Path).ToLower() == ".pdf")
            {
                goto Pdf;
            }
            else if (new string[] { ".zip", ".cbz" }.Contains(Path.GetExtension(file.Path).ToLower()))
            {
                goto Zip;
            }
            else if (new string[] { ".rar", ".cbr", ".7z", ".cb7" }.Contains(Path.GetExtension(file.Path).ToLower()))
            {
                goto SharpCompress;
            }
            else if (Path.GetExtension(file.Path).ToLower() == ".epub")
            {
                goto Epub;
            }

            var stream = await file.OpenStreamForReadAsync();
            var buffer = new byte[64];
            stream.Read(buffer, 0, stream.Length < 64 ? (int)stream.Length : 64);
            stream.Close();

            if (buffer.Take(5).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d }))
            {
                //pdf
                goto Pdf;
            }
            else if (buffer.Take(6).SequenceEqual(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }))
            {
                //7zip
                goto SharpCompress;
            }
            else if ((buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 })) ||
                (buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x05, 0x06 })) ||
                (buffer.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x07, 0x08 }))
                )
            {
                //zip
                if (buffer.Skip(0x1e).Take(28).SequenceEqual(Encoding.ASCII.GetBytes("mimetypeapplication/epub+zip")))
                {
                    //epub
                    goto Epub;
                }
                goto Zip;
            }
            else if (buffer.Take(7).SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00 }))
            {
                //rar
                goto SharpCompress;
            }
            else if (buffer.Take(8).SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00 }))
            {
                //rar5
                goto SharpCompress;
            }

            return (null, false);

        Pdf:;
            {
                var book = new Books.PdfBook();
                try
                {
                    await book.Load(file, async (a) => {
                        var dialog = new PasswordRequestContentDialog();
                        var result = await dialog.ShowAsync();
                        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                        {
                            return dialog.Password;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    
                    });
                }
                catch { return (null, false); }
                if (book.PageCount <= 0) { return (null, false); }
                return (book, false);
            }
        Zip:;
            {
                var book = new Books.CbzBook();
                try
                {
                    await book.LoadAsync(WindowsRuntimeStreamExtensions.AsStream(await file.OpenReadAsync()));
                }
                catch
                {
                    return (null, false);
                }
                if (book.PageCount <= 0) { return (null, false); }
                return (book, false);
            }
        SharpCompress:;
            {
                var book = new Books.CompressedBook();
                try
                {
                    await book.LoadAsync(WindowsRuntimeStreamExtensions.AsStream(await file.OpenReadAsync()));
                }
                catch
                {
                    return (null, false);
                }
                if (book.PageCount <= 0) { return (null, false); }
                return (book, false);
            }
        Epub:;
            {
                return (null, true);
            }
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

        public static string StorageItemRegister(Windows.Storage.IStorageItem file)
        {
            var acl= Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            return acl.Add(file);
        }

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string id)
        {
            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            return await acl.GetItemAsync(id);
        }

        public static char FileSplitLetter { get { return '\\'; } }

        public static string PathJoin(string[] Path)
        {
            return String.Join(FileSplitLetter.ToString(), Path);
        }

        public static string[] PathSplit(string Path)
        {
            return Path.Split(FileSplitLetter);
        }

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string token, string Path)
        {
            return await StorageItemGet(token, PathSplit(Path));
        }

        public static async Task<Windows.Storage.IStorageItem> StorageItemGet(string token,string[] Path)
        {
            Windows.Storage.IStorageItem currentFolder = await StorageItemGet(token);
            foreach(var item in Path)
            {
                if (currentFolder == null) return null;
                if (currentFolder is Windows.Storage.StorageFolder)
                {
                    currentFolder = await (currentFolder as Windows.Storage.StorageFolder).TryGetItemAsync(item);
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
            foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
            {
                picker.FileTypeFilter.Add(ext);
            }
            return await picker.PickSingleFileAsync();

        }

        public static async Task<Books.IBook> PickBook()
        {
            return (await GetBookFromFile(await PickFile())).Book;
        }
    }
}
