using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace BookViewerApp
{
    public class BookInfoStorage
    {
        private const string fileName = "Bookinfo.xml";
        private static Windows.Storage.StorageFolder DataFolder { get { return Windows.Storage.ApplicationData.Current.RoamingFolder; } }

        static System.Threading.SemaphoreSlim fileSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        internal static async Task<Windows.Storage.StorageFile> GetDataFileAsync()
        {
            return (Windows.Storage.StorageFile)(await DataFolder.TryGetItemAsync(fileName));
        }

        internal static async Task<BookInfo[]> LoadAsync()
        {

            var f = await GetDataFileAsync();
            if (f == null) return null;

            await fileSemaphore.WaitAsync();
            try
            {
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(BookInfo[]));
                    return (BookInfo[])serializer.Deserialize(s);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                fileSemaphore.Release();
            }
        }

        internal static async Task SaveAsync()
        {
            await SaveDataAsync((await GetBookInfoAsync()).ToArray());
        }

        private static async Task SaveDataAsync(BookInfo[] items)
        {
            await fileSemaphore.WaitAsync();
            try
            {
                var f = await DataFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(BookInfo[]));
                    serializer.Serialize(s, items);
                }
            }
            catch {  }
            finally
            {
                fileSemaphore.Release();
            }
        }

        private static List<BookInfo> BookInfosCache;

        public static async Task<List<BookInfo>> GetBookInfoAsync()
        {
            if (BookInfosCache == null) BookInfosCache = ((await LoadAsync()) ?? new BookInfo[0]).ToList();
            return BookInfosCache;
        }

        public async static Task<BookInfo> GetBookInfoByIDAsync(string id)
        {
            var bis = (await GetBookInfoAsync());
            foreach(var item in bis)
            {
                if (item.ID == id) { return item; }
            }

            var book = new BookInfo() { ID = id };
            bis.Add(book);
            return book;
        }

        public class BookInfo
        {
            public string ID = "";
            public List<BookmarkItem> Bookmarks = new List<BookmarkItem>();

            public BookmarkItem GetLastReadPage()
            {
                return Bookmarks.Find((s) => s.Type == BookmarkItem.BookmarkItemType.LastRead);
            }

            public void SetLastReadPage(uint page)
            {
                var lastread = GetLastReadPage();
                if (lastread != null) Bookmarks.Remove(lastread);
                Bookmarks.Add(new BookmarkItem() { Page = page, Type = BookmarkItem.BookmarkItemType.LastRead });
            }

            public class BookmarkItem
            {
                public uint Page = 0;
                public BookmarkItemType Type = BookmarkItemType.UserDefined;
                public enum BookmarkItemType
                {
                    LastRead, UserDefined
                }
            }
        }

    }
}
