using System;
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

        public static StorageContent<Library.bookmarks_library> LocalBookmark = new StorageContent<Library.bookmarks_library>(StorageContent<Library.bookmarks_library>.SavePlaces.InstalledLocation, "ms-appx:///res/values/Bookmark.xml", () => new Library.bookmarks_library());

        public enum LibraryKind
        {
            Library, History, Folders, Bookmarks
        }


        public static ContainerDelegateItem GetItemLibrary()
        {
            return new ContainerDelegateItem(GetItem_GetWord("Libraries"), "/Libraries", async (_) =>
            {
                var library = await Content.GetContentAsync();
                if (library?.libraries == null) return Array.Empty<IFileItem>();
                return (await Task.WhenAll(library.libraries.Select(async a => {
                    var result = await a?.AsFileItem();
                    result.MenuCommandsProvider = UIHelper.ContextMenus.GetMenuLibrary(a);
                    return result;
                })))?.Where(a => a != null)?.ToArray() ?? Array.Empty<IFileItem>();
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_l.png"))),
                Tag = LibraryKind.Library,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        public static ContainerDelegateItem GetItemHistory(System.Windows.Input.ICommand PathRequestCommand)
        {
            if (!(bool)Storages.SettingStorage.GetValue("ShowHistories")) return null;
            return new ContainerDelegateItem(GetItem_GetWord("Histories"), "/History", async (_) =>
            {
                var history = await HistoryStorage.Content.GetContentAsync();
                if (history == null || !(bool)Storages.SettingStorage.GetValue("ShowHistories")) return Array.Empty<IFileItem>();
                //一々ファイル取得してると重い…。
                //特に、履歴から開く→履歴更新がファイルを開く処理と同時になったりする。
                //一方、履歴の情報だけだとファイルが既に消えてる場合がある。
                return history.Where(a => !a.CurrentlyInaccessible).Select(a => new HistoryItem(a) { MenuCommandsProvider = UIHelper.ContextMenus.GetMenuHistory(PathRequestCommand) });

                //return (await Task.WhenAll(history.Select(async a => (a, await a.GetFile()))))?.Where(a => a.Item2 != null)?.Select(a => new StorageFileItem(a.Item2)
                //{
                //    DateCreatedOverride = a.a.Date,
                //    RenameCommand = new InvalidCommand(),
                //    DeleteCommand = new DelegateCommand(async c =>
                //    {
                //        await HistoryStorage.DeleteHistory(a.a.Id);
                //    })
                //})?.ToArray() ?? Array.Empty<IFileItem>();
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_l.png"))),
                MenuCommandsProvider = UIHelper.ContextMenus.MenuHistories,
                DeleteCommand = new DelegateCommand(async a =>
                {
                    HistoryStorage.Content.Content = Array.Empty<HistoryStorage.HistoryInfo>();
                    await HistoryStorage.Content.SaveAsync();
                    OnLibraryUpdateRequest(LibraryKind.History);
                }, a => !(a is bool b && b == true)),
                Tag = LibraryKind.History,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        public static ContainerDelegateItem GetItemFolders()
        {
            return new ContainerDelegateItem(GetItem_GetWord("Folders"), "/Folders", async (sender) =>
            {
                var library = await Content.GetContentAsync();
                var list = (await Task.WhenAll(library.folders.Select(async a => await a.AsTokenLibraryItem(UIHelper.ContextMenus.MenuFolderToken, UIHelper.ContextMenus.MenuStorage))))?.Where(a => a != null)?.ToArray() ?? Array.Empty<TokenLibraryItem>();
                foreach (var item in list) item.Parent = sender;
                return list;
            })
            {
                MenuCommandsProvider = UIHelper.ContextMenus.MenuFolders,
                Tag = LibraryKind.Folders,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        public static ContainerDelegateItem GetItemBookmarks(Action<string> bookmarkAction)
        {
            return new ContainerDelegateItem(GetItem_GetWord("Bookmarks"), "/Bookmarks", async (parent) =>
            {
                var library = await Content.GetContentAsync();
                var bookmark_local = await LocalBookmark.GetContentAsync();

                var list = library?.bookmarks?.AsFileItem(bookmarkAction)?.Where(a => a != null)?.ToList() ?? new List<IFileItem>();

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
                                //await parent.GetChildren();
                                //parent?.ChildrenProvided?.Remove(item);
                                LibraryStorage.OnLibraryUpdateRequest(LibraryKind.Bookmarks);
                            };
                            break;
                        case StorageBookmarkItem item2:
                            item2.ActionDelete = () =>
                            {
                                var items = library.bookmarks.Items.ToList();
                                items.Remove(item2.Content);
                                library.bookmarks.Items = items.ToArray();
                                //await parent.GetChildren();
                                //parent?.ChildrenProvided?.Remove(item);
                                LibraryStorage.OnLibraryUpdateRequest(LibraryKind.Bookmarks);
                            };
                            break;
                    }

                }
                if (bookmark_local != null)
                {
                    if ((bool)SettingStorage.GetValue("ShowPresetBookmarks"))
                    {
                        var bookmarksCulture = bookmark_local?.GetBookmarksForCulture(System.Globalization.CultureInfo.CurrentCulture);
                        var local = bookmarksCulture?.AsFileItem(bookmarkAction, true);
                        if (bookmarksCulture != null) list.Insert(0, new ContainerItem(bookmarksCulture.title ?? "App Bookmark", LocalBookmark.FileName, local)
                        {
                            FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/PresetBookmark"),
                            MenuCommandsProvider = UIHelper.ContextMenus.MenuBookmarkPreset,
                        });
                    }
                }
                if (list.Count == 0) return null;
                return list;
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async a => (null, null), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_l.png"))),
                Tag = LibraryKind.Bookmarks,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
                MenuCommandsProvider = UIHelper.ContextMenus.MenuBookmarks,
            };
        }

        public static ContainerItem GetItem(Action<string> bookmarkAction, System.Windows.Input.ICommand PathRequestCommand)
        {
            var result = new ObservableCollection<IFileItem>();

            var itemFolder = GetItemFolders();
            var itemLibrary = GetItemLibrary();
            var itemHistory = GetItemHistory(PathRequestCommand);
            var itemBookmark = GetItemBookmarks(bookmarkAction);

            result.Add(itemFolder);
            result.Add(itemLibrary);
            result.Add(itemHistory);
            result.Add(itemBookmark);

            foreach (var item in result.ToArray()) { if (item == null) result.Remove(item); }

            LibraryUpdateRequest += async (s, e) =>
            {
                switch (e)
                {
                    case LibraryKind.Bookmarks:
                        await itemBookmark.GetChildren();
                        break;
                    case LibraryKind.Folders:
                        await itemFolder.GetChildren();
                        break;
                    case LibraryKind.History:
                        await itemHistory.GetChildren();
                        var entryHistory = result.FirstOrDefault(a => a.Tag is LibraryKind kind && kind == LibraryStorage.LibraryKind.History);
                        if (!(bool)Storages.SettingStorage.GetValue("ShowHistories") && entryHistory != null)
                        {
                            result.Remove(entryHistory);
                        }
                        else if ((bool)Storages.SettingStorage.GetValue("ShowHistories") && entryHistory == null)
                        {
                            result.Insert(2, itemHistory);
                        }
                        break;
                    case LibraryKind.Library:
                        await itemLibrary.GetChildren();
                        break;
                    default: break;
                }
            };

            return new ContainerItem(GetItem_GetWord("PC"), "/PC", result)
            {
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        private static string GetItem_GetWord(string s) => Managers.ResourceManager.Loader.GetString("ExplorerContainer/" + s);

        public static Library.libraryLibrary[] GetTokenUsedByLibrary(string token)
        {
            var result = new List<Library.libraryLibrary>();
            if (Content?.Content?.libraries == null) return Array.Empty<Library.libraryLibrary>();
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

        public static Library.libraryFolder[] GetTokenUsedByFolders(string token) => Content?.Content?.folders.Where(a => a.token == token).ToArray() ?? Array.Empty<Library.libraryFolder>();
    }

    namespace Library
    {
        [Serializable()]
        [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Libra" +
    "ry.xsd")]
        [System.Xml.Serialization.XmlRoot(Namespace = "https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Libra" +
        "ry.xsd", IsNullable = false)]
        public partial class bookmarks_library
        {
            [System.Xml.Serialization.XmlArrayItem("bookmarks", IsNullable = false)]
            [System.Xml.Serialization.XmlArray(ElementName = "multilingual")]
            public libraryBookmarks[] bookmarks;

            public libraryBookmarks GetBookmarksForCulture(System.Globalization.CultureInfo culture)
            {
                if (bookmarks == null) return null;
                libraryBookmarks defaultBookmarks = null;
                libraryBookmarks languageMatchedBookmarks = null;
                foreach (var item in bookmarks)
                {
                    if (item.@default) defaultBookmarks = item;
                    if (item.language == culture.Name) return item;
                    if (item.language == culture.TwoLetterISOLanguageName) languageMatchedBookmarks = item;
                }
                return languageMatchedBookmarks ?? defaultBookmarks;
            }
        }

        public partial class library
        {
            public library()
            {
                this.libraries = Array.Empty<libraryLibrary>();
                this.folders = Array.Empty<libraryFolder>();
            }
        }

        public partial class libraryBookmarks
        {
            public IFileItem[] AsFileItem(Action<string> action, bool isReadOnly = false)
            {
                var list = new List<IFileItem>();
                foreach (var item in this.Items)
                {
                    switch (item)
                    {
                        case libraryBookmarksContainer container:
                            list.Add(container.AsFileItem(action, isReadOnly));
                            break;
                        case libraryBookmarksContainerBookmark bookmark:
                            list.Add(bookmark.AsFileItem(action, isReadOnly));
                            break;
                    }
                }
                return list.ToArray();
            }
        }

        public partial class libraryBookmarksContainer
        {
            public StorageBookmarkContainer AsFileItem(Action<string> action, bool isReadOnly = false)
            {
                return new StorageBookmarkContainer(this) { ActionOpen = action, IsReadOnly = isReadOnly };
            }
        }

        public partial class libraryBookmarksContainerBookmark
        {
            public StorageBookmarkItem AsFileItem(Action<string> action, bool isReadOnly = false)
            {
                return new StorageBookmarkItem(this)
                {
                    ActionOpen = action,
                    IsReadOnly = isReadOnly
                };
            }
        }

        public partial class libraryLibraryFolder
        {
            public async Task<IFileItem> AsFileItem()
            {
                var storage = await GetStorageFolderAsync() as Windows.Storage.StorageFolder;
                if (storage == null) return null;
                return new StorageFileItem(storage);
            }

            public async Task<Windows.Storage.IStorageItem> GetStorageFolderAsync()
            {
                var item = await Managers.BookManager.StorageItemGet(this.token, this.path);
                return item;
            }

            public bool Compare(libraryLibraryFolder folder)
            {
                return this.Compare(folder.token, folder.path);
            }

            public bool Compare(string token, string path)
            {
                return token == this.token && String.Compare(this.path, path, StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        public partial class libraryLibrary
        {
            public async Task<CombinedItem> AsFileItem()
            {
                string libraryFileType = Managers.ResourceManager.Loader.GetString("ItemType/LibraryItem");
                if (this.Items == null || this.Items.Length == 0) return new CombinedItem(Array.Empty<IFileItem>()) { Name = this.title, FileTypeDescription = libraryFileType };

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
                var returnValue = new CombinedItem(result.ToArray())
                {
                    Name = this.title,
                    FileTypeDescription = libraryFileType
                };
                returnValue.RenameCommand = new DelegateCommand(a =>
                {
                    this.title = a?.ToString() ?? this.title;
                    returnValue.Name = this.title;
                });

                return returnValue;
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
                if (fileItem == null) return null;
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
