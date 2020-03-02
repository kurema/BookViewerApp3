using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

using BookViewerApp.Helper;
using kurema.FileExplorerControl.Models.FileItems;

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

        public static async Task<ContainerItem> GetItem(Action<string> bookmarkAction)
        {
            var library = await LoadDataLocalAsync();
            var result = new List<ContainerItem>();
            if (library?.folders != null)
            {
                var list = (await Task.WhenAll(library.folders.Select(async a => await a.AsFileItem())))?.Where(a => a != null)?.ToArray() ?? new IFileItem[0];
                result.Add(new ContainerItem("Folders", "", list));
            }
            if(library?.libraries!=null)
            {
                var list = (await Task.WhenAll(library.libraries.Select(async a => await a.AsFileItem())))?.Where(a => a != null)?.ToArray() ?? new IFileItem[0];
                result.Add(new ContainerItem("Libraries", "", list));
            }
            if (library?.bookmarks != null)
            {
                var list = library.bookmarks.AsFileItem(bookmarkAction).Where(a => a != null).ToArray() ?? new IFileItem[0];
                result.Add(new ContainerItem("Bookmarks", "", list));
            }

            return new ContainerItem("PC", "", result.ToArray());
        }
    }

    namespace Library
    {
        public partial class library
        {
            public library()
            {
                this.libraries = new libraryLibrary[0];
                this.folders = new libraryFolder[0];
            }
        }

        public partial class libraryBookmarks
        {
            public IFileItem[] AsFileItem(Action<string> action)
            {
                var list = new List<IFileItem>();
                foreach(var item in this.Items)
                {
                    switch (item)
                    {
                        case libraryBookmarksContainer container:
                            list.Add(container.AsFileItem(action));
                            break;
                        case libraryBookmarksContainerBookmark bookmark:
                            list.Add(bookmark.AsFileItem(action));
                            break;
                    }
                }
                return list.ToArray();
            }
        }

        public partial class libraryBookmarksContainer
        {
            public StorageBookmarkContainer AsFileItem(Action<string> action)
            {
                return new StorageBookmarkContainer(this) { ActionOpen = action };
            }
        }

        public partial class libraryBookmarksContainerBookmark
        {
            public StorageBookmarkItem AsFileItem(Action<string> action)
            {
                var result = new StorageBookmarkItem(this);
                result.ActionOpen = action;
                return result;
            }
        }

        public partial class libraryLibraryFolder
        {
            public async Task<IFileItem> AsFileItem()
            {
                var storage = await GetStorageFolderAsync();
                if (storage == null) return null;
                return new StorageFileItem(storage);
            }

            public async Task<Windows.Storage.StorageFolder> GetStorageFolderAsync()
            {
                var item = await Managers.BookManager.StorageItemGet(this.token, this.path);
                return item as Windows.Storage.StorageFolder;
            }
        }

        public partial class libraryLibrary
        {
            public async Task<IFileItem> AsFileItem()
            {
                if (this.Items == null || this.Items.Length == 0) return new CombinedItem(new IFileItem[0]) { Name = this.title };

                var result = new List<IFileItem>();
                foreach (var item in this.Items)
                {
                    switch (item)
                    {
                        case libraryLibraryFolder lf:
                            {
                                var fileItem = await lf.AsFileItem();
                                if (fileItem != null) result.Add(fileItem);
                            }
                            break;
                        case libraryLibraryArchive la:
                            break;
                        case libraryLibraryNetwork ln:
                            break;
                        default: break;
                    }
                }
                return new CombinedItem(result.ToArray());
            }
        }


        public partial class libraryFolder
        {
            public libraryFolder()
            {
            }

            public libraryFolder(Windows.Storage.StorageFolder folder)
            {
                this.token = Managers.BookManager.StorageItemRegister(folder);
            }

            public void Remove()
            {
                Managers.BookManager.StorageItemUnregister(this.token);
            }

            public async Task<IFileItem> AsFileItem()
            {
                var storage = await GetStorageFolderAsync();
                if (storage == null) return null;
                return new StorageFileItem(storage);
            }

            public async Task<Windows.Storage.StorageFolder> GetStorageFolderAsync()
            {
                var item = await Managers.BookManager.StorageItemGet(this.token);
                if (item is Windows.Storage.StorageFolder f) { return f; }
                else { return null; }
            }
        }
    }
}
