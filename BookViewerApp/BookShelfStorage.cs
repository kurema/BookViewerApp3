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

        private static async Task<List<BookShelf>> LoadDataLocalAsync()
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                var f = await GetDataFileLocalAsync();
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<BookShelf>));
                    return (List<BookShelf>)serializer.Deserialize(s);
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

        private static async Task SaveAsync(List<BookShelf> items)
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                var f = await DataFolderLocal.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<BookShelf>));
                    serializer.Serialize(s, items);
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

        public interface IBookShelfItem
        {
             string Title { get; set; }
        }

        public class BookShelf:IBookShelfItem
        {
            public List<IBookShelfItem> Contents = new List<IBookShelfItem>();
            public string Title { get; set; }

            public BookShelf()
            {
                var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                Title = rl.GetString("BookShelfTitleNew");
            }

            public class BookShelfBook:IBookShelfItem
            {
                public string ID = "";
                private BookInfoStorage.BookInfo BookInfoCache;
                public string Title { get; set; }

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
    }
}
