using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

using BookViewerApp.Helper;

namespace BookViewerApp.Storages
{
    public static class LibraryStorage
    {
        private const string fileName = "Library.xml";
        private static Windows.Storage.StorageFolder DataFolderLocal => Functions.GetSaveFolderLocal();
        static System.Threading.SemaphoreSlim fileLocalSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        internal static async Task<Library.library> LoadAsync()
        {
            return Content = await LoadDataLocalAsync() ?? new Library.library();
        }

        public static Library.library Content { get; set; } = null;

        private static async Task<Library.library> LoadDataLocalAsync()
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                if (await DataFolderLocal.TryGetItemAsync(fileName) is Windows.Storage.StorageFile f)
                {
                    using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream())
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Library.library));
                        return ((Library.library)serializer.Deserialize(s));
                    }
                }
                else { return null; }
            }
            catch
            {
                return null;
            }
            finally
            {
                fileLocalSemaphore.Release();
            }
        }

        public static async Task SaveAsync()
        {
            await SaveAsync(Content);
        }

        public static async Task SaveAsync(Library.library library)
        {
            await fileLocalSemaphore.WaitAsync();
            try
            {
                var f = await DataFolderLocal.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Library.library));
                    serializer.Serialize(s, library);
                }
            }
            catch { }
            finally
            {
                fileLocalSemaphore.Release();
            }
        }
    }

    namespace Library
    {
        public partial class library
        {
            public library()
            {
                this.libraries = new libraryLibrary[0];
            }

            //public kurema.FileExplorerControl.Models.ContainerItem AsFileItem()
            //{
            //    //ToDo: Need Translate!
            //    return new kurema.FileExplorerControl.Models.ContainerItem("Library", "Library");
            //}
        }

        public partial class libraryBookmarksContainerBookmark
        {
            public kurema.FileExplorerControl.Models.BookmarkItem AsFileItem(Action<kurema.FileExplorerControl.Models.BookmarkItem> action)
            {
                var result= new kurema.FileExplorerControl.Models.BookmarkItem()
                {
                    DateCreated = this.created,
                    FileName = this.title,
                    Path = this.url,
                };
                result.Opened += (s, e) => action(s);
                return result;
            }
        }

        public partial class libraryLibraryFolder
        {
            public async Task<Windows.Storage.StorageFolder> GetStorageFolderAsync()
            {
                var item = await Managers.BookManager.StorageItemGet(this.token, this.path);
                return item as Windows.Storage.StorageFolder;
            }
        }

        //public partial class libraryFolder
        //{
        //    public libraryFolder()
        //    {
        //    }

        //    public libraryFolder(Windows.Storage.StorageFolder folder)
        //    {
        //        this.token = Managers.BookManager.StorageItemRegister(folder);
        //        this.id = this.token;
        //    }

        //    public void Remove()
        //    {
        //        Managers.BookManager.StorageItemUnregister(this.token);
        //    }

        //    public async Task<Windows.Storage.StorageFolder> GetStorageFolderAsync()
        //    {
        //        var item = await Managers.BookManager.StorageItemGet(this.token);
        //        if (item is Windows.Storage.StorageFolder f) { return f; }
        //        else { return null; }
        //    }
        //}
    }
}
