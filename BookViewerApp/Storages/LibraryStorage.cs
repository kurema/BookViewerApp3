﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

using BookViewerApp.Helper;
using kurema.FileExplorerControl.Models.FileItems;
using kurema.FileExplorerControl.Models;

namespace BookViewerApp.Storages
{
    public static class LibraryStorage
    {
        public static StorageContent<Library.library> Content = new StorageContent<Library.library>(StorageContent<Library.library>.SavePlaces.Local, "Library.xml", () => new Library.library());

        public static event Windows.Foundation.TypedEventHandler<object, LibraryKind> LibraryUpdateRequest;
        public static void OnLibraryUpdateRequest(LibraryKind kind) => LibraryUpdateRequest?.Invoke(null, kind);

        public enum LibraryKind
        {
            Library, History, Folders, Bookmarks
        }


        public static async Task<ContainerItem> GetItemLibrary()
        {
            var library = await Content.GetContentAsync();
            if (library?.libraries != null)
            {
                var list = (await Task.WhenAll(library.libraries.Select(async a => await a.AsFileItem())))?.Where(a => a != null)?.ToArray() ?? new IFileItem[0];
                return new ContainerItem(GetItem_GetWord("Libraries"), "/Libraries", list)
                {
                    IIconProvider = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_l.png"))),
                };
            }
            return null;
        }

        public static async Task<ContainerItem> GetItemHistory()
        {
            var history = await HistoryStorage.Content.GetContentAsync();
            if (history != null)
            {
                var list = (await Task.WhenAll(history.Select(async a => await a.GetFile())))?.Where(a => a != null)?.Select(a => new StorageFileItem(a))?.ToArray() ?? new IFileItem[0];
                if (list.Length != 0) return new ContainerItem(GetItem_GetWord("Histories"), "/History", list)
                {
                    IIconProvider = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_l.png")))
                };
            }
            return null;
        }

        public static async Task<ContainerItem> GetItemFolders()
        {
            var library = await Content.GetContentAsync();
            if (library?.folders != null)
            {
                var list = (await Task.WhenAll(library.folders.Select(async a => await a.AsTokenLibraryItem(UIHelper.ContextMenus.MenuFolderToken))))?.Where(a => a != null)?.ToArray() ?? new TokenLibraryItem[0];
                var container = new ContainerItem(GetItem_GetWord("Folders"), "/Folders", list) { MenuCommandsProvider = UIHelper.ContextMenus.MenuFolders };
                foreach (var item in list) item.Parent = container;
                return container;
            }
            return null;
        }

        public static async Task<ContainerItem> GetItemBookmarks(Action<string> bookmarkAction)
        {
            var library = await Content.GetContentAsync();
            if (library?.bookmarks != null)
            {
                var list = library?.bookmarks?.AsFileItem(bookmarkAction)?.Where(a => a != null)?.ToArray() ?? new IFileItem[0];
                var container = new ContainerItem(GetItem_GetWord("Bookmarks"), "/Bookmarks", list)
                {
                    IIconProvider = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_l.png")))
                };
                foreach (var item in list)
                {
                    switch (item)
                    {
                        case StorageBookmarkContainer item1:
                            item1.ActionDelete = () =>
                            {
                                var items = library.bookmarks.Items.ToList();
                                items.Remove(item1.Content);
                                library.bookmarks.Items = items.ToArray();
                                container?.Children?.Remove(item);
                            };
                            break;
                        case StorageBookmarkItem item2:
                            item2.ActionDelete = () =>
                            {
                                var items = library.bookmarks.Items.ToList();
                                items.Remove(item2.Content);
                                library.bookmarks.Items = items.ToArray();
                                container?.Children?.Remove(item);
                            };
                            break;
                    }
                }
                return container;
            }
            return null;
        }

        public static async Task<ContainerItem> GetItem(Action<string> bookmarkAction)
        {

            var library = await Content.GetContentAsync();
            var history = await HistoryStorage.Content.GetContentAsync();
            var result = new List<ContainerItem>();

            result.Add(await GetItemFolders());
            result.Add(await GetItemLibrary());
            result.Add(await GetItemHistory());
            result.Add(await GetItemBookmarks(bookmarkAction));

            return new ContainerItem(GetItem_GetWord("PC"), "/PC", result.Where(a => a != null).ToArray());
        }

        private static string GetItem_GetWord(string s) => Managers.ResourceManager.Loader.GetString("ExplorerContainer/" + s);

        public static Library.libraryLibrary[] GetTokenUsedByLibrary(string token)
        {
            var result = new List<Library.libraryLibrary>();
            if (Content?.Content?.libraries == null) return new Library.libraryLibrary[0];
            foreach (var item in Content.Content.libraries)
            {
                foreach (var item2 in item.Items)
                {
                    switch (item2)
                    {
                        case Library.libraryLibraryArchive arc:
                            if (arc.token == token)
                            {
                                result.Add(item);
                                goto end;
                            }
                            break;
                        case Library.libraryLibraryFolder fol:
                            if (fol.token == token)
                            {
                                result.Add(item);
                                goto end;
                            }
                            break;
                    }
                }
            end:;
            }

            return result.ToArray();
        }

        public static Library.libraryFolder[] GetTokenUsedByFolders(string token) => Content?.Content?.folders.Where(a => a.token == token).ToArray() ?? new Library.libraryFolder[0];
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
                foreach (var item in this.Items)
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

            public async Task<StorageFileItem> AsStorageFileItem()
            {
                var storage = await GetStorageFolderAsync();
                if (storage == null) return null;
                return new StorageFileItem(storage);
            }

            public async Task<TokenLibraryItem> AsTokenLibraryItem(Func<IFileItem, MenuCommand[]> menuCommand = null, Func<IFileItem, MenuCommand[]> menuCommandCascade = null)
            {
                var fileItem = await this.AsStorageFileItem();
                if (menuCommandCascade != null)
                {
                    fileItem.MenuCommandsProviderCascade = menuCommandCascade;
                    fileItem.MenuCommandsProvider = menuCommandCascade;
                }
                return new TokenLibraryItem(this, fileItem) { MenuCommandsProvider = menuCommand };
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
