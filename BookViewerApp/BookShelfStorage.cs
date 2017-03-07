using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;


namespace BookViewerApp
{
    public class BookShelfStorage
    {
        private const string fileName = "BookShelf.xml";
        private static Windows.Storage.StorageFolder DataFolderLocal { get { return Functions.GetSaveFolderLocal(); } }
        static System.Threading.SemaphoreSlim fileLocalSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private static async Task<Windows.Storage.StorageFile> GetDataFileLocalAsync()
        {
            return (Windows.Storage.StorageFile)(await DataFolderLocal.TryGetItemAsync(fileName));
        }

        internal static async Task<List<BookShelf>> LoadAsync()
        {
            return BookShelvesCache = await LoadDataLocalAsync() ?? new List<BookShelf>();
        }

        private static List<BookShelf> BookShelvesCache;

        public static void Clear()
        {
            BookShelvesCache = new List<BookShelf>();
        }

        private static async Task<List<BookShelf>> LoadDataLocalAsync()
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                var f = await GetDataFileLocalAsync();
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(BookShelf[]));
                    return ((BookShelf[])serializer.Deserialize(s)).ToList();
                }
            }
            catch {
                return null;
            }
            finally
            {
                fileLocalSemaphore.Release();
            }
        }

        public static async Task SaveAsync()
        {
            await SaveAsync(await GetBookShelves());
        }

        private static async Task SaveAsync(List<BookShelf> items)
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                var f = await DataFolderLocal.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(BookShelf[]));
                    serializer.Serialize(s, items.ToArray());
                }
            }
            catch { }
            finally
            {
                fileLocalSemaphore.Release();
            }
        }

        public static async Task<List<BookShelf>> GetBookShelves(){
            return BookShelvesCache ?? await LoadAsync();
        }

        public async static Task<BookContainer> GetFromStorageFolder(Windows.Storage.StorageFolder folder, string Token = null, string[] Path = null)
        {
            var result = new BookContainer();
            result.Title = folder.DisplayName;

            List<string> path = (Path ?? new string[0]).ToList();
            if (Token == null)
            {
                Token = Books.BookManager.StorageItemRegister(folder);
            }
            else
            {
                path.Add(folder.Name);
            }

            foreach (var item in await folder.GetFoldersAsync())
            {
                var f = await GetFromStorageFolder(item, Token, path.ToArray());
                if (!f.IsEmpty())
                {
                    result.Folders.Add(f);
                }
            }

            foreach (var item in await folder.GetFilesAsync())
            {
                var f = await GetFromStorageFile(item, Token, path.ToArray());
                if (f != null)
                {
                    result.Files.Add(f);
                }
            }

            return result;
        }

        public static BookShelf GetFlatBookShelf(BookContainer shelf)
        {
            var result = new BookShelf();
            if (shelf.Files.Count > 0)
            {
                result.Folders.Add(shelf);
            }
            result.Title = shelf.Title;

            foreach (var item in shelf.Folders)
            {
                result.Folders.AddRange(GetFlatBookShelf(item).Folders);
            }
            shelf.Folders = new List<BookContainer>();
            return result;
        }

        public async static Task<BookContainer.BookShelfBook> GetFromStorageFile(Windows.Storage.StorageFile file, string Token=null,string[] Path=null) {
            if (!Books.BookManager.IsFileAvailabe(file)) return null;

            var book = await Books.BookManager.GetBookFromFile(file);
            var bookBS= new BookContainer.BookShelfBook() { ID = book.ID };
            bookBS.Title = file.DisplayName;
            if(book is Books.IBookFixed)
            {
                bookBS.Size = (int)(book as Books.IBookFixed).PageCount;
                await ThumbnailManager.SaveImageAsync(book);
            }

            List<string> path = Path.ToList();
            if (Token == null)
            {
                Token = Books.BookManager.StorageItemRegister(file);
            }
            else
            {
                path.Add(file.Name);
            }
            bookBS.Access = new BookAccessInfo() { AccessToken = Token, Path = Books.BookManager.PathJoin(path.ToArray()) };
            return bookBS;
        }

        public class BookShelf
        {
            public List<BookContainer> Folders = new List<BookContainer>();
            public string Title;
            public bool Secret = false;

            public BookShelf()
            {
                var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                Title = rl.GetString("BookShelfTitleNew");
            }

            public string GetFirstAccessToken() {
                foreach (var f in this.Folders)
                {
                    if (f.Files.Count > 0)
                    {
                        return f.Files[0].Access.AccessToken;
                    }
                }
                return null;
            }
        }

        public class BookContainer
        {
            public List<BookContainer> Folders = new List<BookContainer>();
            public List<BookShelfBook> Files = new List<BookShelfBook>();

            public string Title { get; set; }

            public BookContainer()
            {
            }

            public bool IsEmpty()
            {
                if(Files.Count() > 0) { return false; }
                if (Folders.Count() == 0) { return true; }
                foreach(var item in Folders)
                {
                    if (!item.IsEmpty()) return false;
                }
                return true;
            }

            public class BookShelfBook
            {
                public string ID = "";
                private BookInfoStorage.BookInfo BookInfoCache;
                public string Title { get; set; }
                public int Size;//読んだ割合を出すのに使います。
                public BookAccessInfo Access;

                public async Task<BookInfoStorage.BookInfo> GetBookInfoAsync()
                {
                    if (ID != "")
                    {
                        if (BookInfoCache == null || (BookInfoCache != null && BookInfoCache.ID != this.ID))
                        {
                            BookInfoCache = await BookInfoStorage.GetBookInfoByIDAsync(this.ID);
                        }
                    }
                    return BookInfoCache;
                }
            }
        }

        public class BookAccessInfo
        {
            public string AccessToken;
            public string Path;

            public async Task<Windows.Storage.IStorageItem> TryGetItem()
            {
                var item = await Books.BookManager.StorageItemGet(AccessToken, Path);
                if (item is Windows.Storage.StorageFile) { return item; }
                else { return null; }
            }

            public async Task<bool> IsAccessible()
            {
                var item = await Books.BookManager.StorageItemGet(AccessToken, Path);
                return (item is Windows.Storage.StorageFile);
            }
        }
    }
}
