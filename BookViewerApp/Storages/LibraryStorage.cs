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

#nullable enable
namespace BookViewerApp.Storages
{
    public static class LibraryStorage
    {
        public static StorageContent<Library.library> Content = new(SavePlaces.Local, "Library.xml", () => new Library.library());

        public static event Windows.Foundation.TypedEventHandler<object?, LibraryKind>? LibraryUpdateRequest;
        public static void OnLibraryUpdateRequest(LibraryKind kind) => LibraryUpdateRequest?.Invoke(null, kind);

        public static StorageContent<Library.bookmarks_library> LocalBookmarks = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/Bookmark.xml", () => new Library.bookmarks_library());

        public static StorageContent<Library.bookmarks> RoamingBookmarks = new(SavePlaces.Roaming, "Bookmarks.xml", () => new Library.bookmarks());

        public enum LibraryKind
        {
            Library, History, Folders, Bookmarks
        }


        public static ContainerDelegateItem GetItemLibrary()
        {
            return new ContainerDelegateItem(GetItem_GetWord("Libraries"), "/Libraries", async (_) =>
            {
                var library = await Content.GetContentAsync();
                if (library?.libraries is null) return Array.Empty<IFileItem>();
                return (await Task.WhenAll(library.libraries.Where(a => a != null).Select(async a =>
                    {
                        var result = await a.AsFileItem();
                        result.MenuCommandsProvider = UIHelper.ContextMenus.GetMenuLibrary(a);
                        return result;
                    })))?.Where(a => a != null)?.ToArray() ?? Array.Empty<IFileItem>();
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(
                    kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate.NullResult
                    , () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_library_l.png"))),
                Tag = LibraryKind.Library,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
                MenuCommandsProvider = UIHelper.ContextMenus.MenuLibraryContainer
            };
        }

        public static ContainerDelegateItem? GetItemHistoryMRU(System.Windows.Input.ICommand PathRequestCommand, Func<Views.TabPage> tabPageProvider)
        {
            if (!(bool)SettingStorage.GetValue("ShowHistories")) return null;
            return new ContainerDelegateItem(GetItem_GetWord("Histories"), "/History", (_) =>
            {
                //Issue:
                //同じファイルが重複登録されるバグを確認。
                //一度のみ。原因不明。
                return Task.FromResult<IEnumerable<IFileItem>>(
                    (bool)SettingStorage.GetValue(SettingStorage.SettingKeys.ShowHistories) ?
                    Managers.HistoryManager.List.Entries.Select(a => new HistoryMRUItem(a)
                    {
                        MenuCommandsProvider = UIHelper.ContextMenus.GetMenuHistoryMRU(PathRequestCommand, tabPageProvider)
                    }).OrderByDescending(a => a.DateCreated) : Array.Empty<IFileItem>());
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(
                    kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate.NullResult
                    , () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_l.png"))),
                MenuCommandsProvider = UIHelper.ContextMenus.MenuHistories,
                Tag = LibraryKind.History,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
                DeleteCommand = new DelegateCommand(a =>
                {
                    Managers.HistoryManager.List.Clear();
                    Managers.HistoryManager.OnUpdated();
                }),
            };
        }

        [Obsolete]
        public static ContainerDelegateItem? GetItemHistory(System.Windows.Input.ICommand PathRequestCommand)
        {
            if (!(bool)SettingStorage.GetValue("ShowHistories")) return null;
            return new ContainerDelegateItem(GetItem_GetWord("Histories"), "/History", async (_) =>
            {
                var history = await HistoryStorage.Content.GetContentAsync();
                if (history == null || !(bool)SettingStorage.GetValue("ShowHistories")) return Array.Empty<IFileItem>();
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
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(
                    kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate.NullResult
                    , () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_clock_l.png"))),
                MenuCommandsProvider = UIHelper.ContextMenus.MenuHistories,
                DeleteCommand = new DelegateCommand(async a =>
                {
                    HistoryStorage.Content.Content = Array.Empty<HistoryStorage.HistoryInfo>();
                    await HistoryStorage.Content.SaveAsync();
                    OnLibraryUpdateRequest(LibraryKind.History);
                    GarbageCollectToken();
                }, a => !(a is bool b && b == true)),
                Tag = LibraryKind.History,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        public static ContainerDelegateItem GetItemFolders(Func<Views.TabPage> tabPageProvider)
        {
            return new ContainerDelegateItem(GetItem_GetWord("Folders"), "/Folders", async (sender) =>
            {
                var library = await Content.GetContentAsync();
                var list = (await Task.WhenAll(library.folders.Select(async a => await a.AsTokenLibraryItem(UIHelper.ContextMenus.MenuFolderToken, UIHelper.ContextMenus.MenuStorage(tabPageProvider)))))?.Where(a => a != null)?.ToArray() ?? Array.Empty<TokenLibraryItem>();
                foreach (var item in list) item.Parent = sender;
                return list;
            })
            {
                MenuCommandsProvider = UIHelper.ContextMenus.MenuFolders,
                Tag = LibraryKind.Folders,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
            };
        }

        public static ContainerDelegateItem GetItemBookmarks(Action<string, BookmarkActionType> bookmarkAction, Func<Views.TabPage> tabPageProvider)
        {
            var menuCommandsPr = UIHelper.ContextMenus.MenuBookmarks(bookmarkAction, tabPageProvider);
            return new ContainerDelegateItem(GetItem_GetWord("Bookmarks"), "/Bookmarks", async (parent) =>
            {
                var library = await Content.GetContentAsync();
                var bookmark_local = await LocalBookmarks.GetContentAsync();
                var bookmark_roaming = await RoamingBookmarks.GetContentAsync();

                var list = library?.bookmarks?.AsFileItem(bookmarkAction)?.Where(a => a != null)?.ToList() ?? new List<IFileItem>();
                list.AddRange(bookmark_roaming?.AsFileItem(bookmarkAction)?.Where(a => a != null) ?? Array.Empty<IFileItem>());

                foreach (var item in list)
                {
                    switch (item)
                    {
                        case StorageBookmarkContainer item1:
                            item1.ActionDelete = () =>
                            {
                                if (library?.bookmarks != null) library.bookmarks.Items = Functions.GetArrayRemoved(library.bookmarks.Items, item1.Content)?.ToArray();
                                if (bookmark_roaming != null) bookmark_roaming.Items = Functions.GetArrayRemoved(bookmark_roaming.Items, item1.Content)?.ToArray();
                                //await parent.GetChildren();
                                //parent?.ChildrenProvided?.Remove(item);
                                OnLibraryUpdateRequest(LibraryKind.Bookmarks);
                            };
                            item1.MenuCommandsProvider = menuCommandsPr;
                            break;
                        case StorageBookmarkItem item2:
                            item2.ActionDelete = () =>
                            {
                                if (library?.bookmarks != null) library.bookmarks.Items = Functions.GetArrayRemoved(library.bookmarks?.Items, item2.Content)?.ToArray();
                                if (bookmark_roaming != null) bookmark_roaming.Items = Functions.GetArrayRemoved(bookmark_roaming?.Items, item2.Content)?.ToArray();
                                //await parent.GetChildren();
                                //parent?.ChildrenProvided?.Remove(item);
                                OnLibraryUpdateRequest(LibraryKind.Bookmarks);
                            };
                            item2.MenuCommandsProvider = menuCommandsPr;
                            break;
                    }

                }
                if (bookmark_local != null)
                {
                    if ((bool)SettingStorage.GetValue("ShowPresetBookmarks"))
                    {
                        var bookmarksCulture = bookmark_local?.GetBookmarksForCulture(System.Globalization.CultureInfo.CurrentCulture);
                        var local = bookmarksCulture?.AsFileItem(bookmarkAction, true, menuCommandsPr);
                        if (bookmarksCulture != null) list.Insert(0, new ContainerItem(bookmarksCulture.title ?? "App Bookmark", LocalBookmarks.FileName, local)
                        {
                            FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/PresetBookmark"),
                            MenuCommandsProvider = UIHelper.ContextMenus.MenuBookmarkPreset,
                            SearchReusltsProvider = (w) => StorageBookmarkContainer.GetBasicSearchResults(w, a => bookmarkAction?.Invoke(a, BookmarkActionType.Auto)),
                        });
                    }
                }
                if (list.Count == 0) return null;
                return list;
            })
            {
                Icon = new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(
                    kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate.NullResult,
                    () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_s.png")), () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_star_l.png"))),
                Tag = LibraryKind.Bookmarks,
                FileTypeDescription = Managers.ResourceManager.Loader.GetString("ItemType/SystemFolder"),
                MenuCommandsProvider = menuCommandsPr,
                SearchReusltsProvider = (w) => StorageBookmarkContainer.GetBasicSearchResults(w, a => bookmarkAction?.Invoke(a, BookmarkActionType.Auto)),
            };
        }

        public enum BookmarkActionType
        {
            Auto, Internal, External
        }

        public static ContainerItem GetItem(Action<string, BookmarkActionType> bookmarkAction, System.Windows.Input.ICommand PathRequestCommand, Func<Views.TabPage> tabPageProvider)
        {
            var result = new ObservableCollection<IFileItem>();

            var itemFolder = GetItemFolders(tabPageProvider);
            var itemLibrary = GetItemLibrary();
            //var itemHistory = GetItemHistory(PathRequestCommand);
            var itemHistory = GetItemHistoryMRU(PathRequestCommand, tabPageProvider);
            var itemBookmark = GetItemBookmarks(bookmarkAction, tabPageProvider);

            result.Add(itemFolder);
            result.Add(itemLibrary);
            if (itemHistory != null) result.Add(itemHistory);
            result.Add(itemBookmark);

            Managers.HistoryManager.Updated += (s, e) =>
            {
                OnLibraryUpdateRequest(LibraryKind.History);
            };

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
                         if (itemHistory is null) break;
                         await itemHistory.GetChildren();
                         var entryHistory = result.FirstOrDefault(a => a.Tag is LibraryKind kind && kind == LibraryKind.History);
                         if (!(bool)SettingStorage.GetValue("ShowHistories") && entryHistory != null)
                         {
                             result.Remove(entryHistory);
                         }
                         else if ((bool)SettingStorage.GetValue("ShowHistories") && entryHistory is null)
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
            if (Content?.Content?.libraries is null) return Array.Empty<Library.libraryLibrary>();
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

        public static async void OperateBookmark(Func<List<object>?, Task> action)
        {
            var bookmarksRoaming = await RoamingBookmarks.GetContentAsync();
            if (bookmarksRoaming is null)
            {
                await action(null);
                return;
            }
            bookmarksRoaming ??= new Library.bookmarks();
            var bookmarks = (bookmarksRoaming.Items ?? new object[0]).ToList();
            await action(bookmarks);
            bookmarksRoaming.Items = bookmarks.ToArray();
            OnLibraryUpdateRequest(LibraryKind.Bookmarks);
            await RoamingBookmarks.SaveAsync();
        }

        public static Dictionary<string, int>? GetTokenUsedCount()
        {
            var result = new Dictionary<string, int>();

            void CountUp(string token)
            {
                if (result.ContainsKey(token)) result[token]++;
                else result[token] = 1;
            }

            if (Content?.Content is null) return null;

            foreach (var item in Content?.Content?.folders ?? new Library.libraryFolder[0])
            {
                CountUp(item.token);
            }
            foreach (var item in Content?.Content?.libraries ?? new Library.libraryLibrary[0])
            {
                if (item.Items is null) continue;
                foreach (Library.libraryLibraryFolder itemFolder in item.Items)
                {
                    CountUp(itemFolder.token);
                }
            }
            return result;
        }

        [Obsolete]
        public static Dictionary<string, int>? GetTokenUsedCountHistory()
        {
            var result = new Dictionary<string, int>();

            if (HistoryStorage.Content?.Content is null) return null;
            foreach (var item in HistoryStorage.Content.Content)
            {
                if (result.ContainsKey(item.Token)) result[item.Token]++;
                else result[item.Token] = 1;
            }
            return result;
        }

        public static void GarbageCollectToken()
        {
            var stats = GetTokenUsedCount();
            if (stats is null) return;
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            var stats2 = GetTokenUsedCountHistory();
#pragma warning restore CS0612 // 型またはメンバーが旧型式です

            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            if (fal == null || fal.Entries is null) return;
            foreach (var item in fal.Entries)
            {
                if (!stats.ContainsKey(item.Token) && stats2?.ContainsKey(item.Token) != true)
                {
                    fal.Remove(item.Token);
                }
            }
        }
    }
#nullable disable
    namespace Library
    {
        //    [Serializable()]
        //    [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Libra" +
        //"ry.xsd")]
        //    [System.Xml.Serialization.XmlRoot(Namespace = "https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Libra" +
        //    "ry.xsd", IsNullable = false)]
        //    public partial class bookmarks_library
        //    {
        //        [System.Xml.Serialization.XmlArrayItem("bookmarks", IsNullable = false)]
        //        [System.Xml.Serialization.XmlArray(ElementName = "multilingual")]
        //        public libraryBookmarks[] bookmarks;

        //        public libraryBookmarks GetBookmarksForCulture(System.Globalization.CultureInfo culture)
        //        {
        //            if (bookmarks is null) return null;
        //            libraryBookmarks defaultBookmarks = null;
        //            libraryBookmarks languageMatchedBookmarks = null;
        //            foreach (var item in bookmarks)
        //            {
        //                if (item.@default) defaultBookmarks = item;
        //                if (item.language == culture.Name) return item;
        //                if (item.language == culture.TwoLetterISOLanguageName) languageMatchedBookmarks = item;
        //            }
        //            return languageMatchedBookmarks ?? defaultBookmarks;
        //        }
        //    }

        public partial class bookmarks_library
        {

            public bookmarks GetBookmarksForCulture(System.Globalization.CultureInfo culture)
            {
                if (multilingual is null) return null;
                bookmarks defaultBookmarks = null;
                bookmarks languageMatchedBookmarks = null;
                foreach (var item in multilingual)
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

        public partial class bookmarks
        {
            public IFileItem[] AsFileItem(Action<string, LibraryStorage.BookmarkActionType> action, bool isReadOnly = false, Func<IFileItem, MenuCommand[]> menuCommandProvider = null)
            {
                var list = new List<IFileItem>();
                if (Items is null) return Array.Empty<IFileItem>();
                foreach (var item in this.Items)
                {
                    switch (item)
                    {
                        case bookmarksContainer container:
                            list.Add(container.AsFileItem((a) => action?.Invoke(a, LibraryStorage.BookmarkActionType.Auto), isReadOnly, menuCommandProvider));
                            break;
                        case bookmarksContainerBookmark bookmark:
                            list.Add(bookmark.AsFileItem((a) => action?.Invoke(a, LibraryStorage.BookmarkActionType.Auto), isReadOnly, menuCommandProvider));
                            break;
                    }
                }
                return list.OrderByDescending(a => a.IsFolder).ToArray();
            }

            public IEnumerable<StorageBookmarkItem> GetSearchItems(string word, Action<string> actionOpen)
            {
                foreach (var item in this.Items)
                {
                    switch (item)
                    {
                        case bookmarksContainerSearch search:
                            yield return new StorageBookmarkItem(new bookmarksContainerBookmark()
                            {
                                createdSpecified = false,
                                created = DateTime.Now,
                                title = string.Format(search.title, word),
                                url = string.Format(search.url, System.Web.HttpUtility.UrlEncode(word)),
                            })
                            {
                                ActionOpen = actionOpen,
                                IsReadOnly = true,
                            };
                            break;
                    }
                }
            }
        }

        public partial class bookmarksContainer
        {
            public StorageBookmarkContainer AsFileItem(Action<string> action, bool isReadOnly = false, Func<IFileItem, MenuCommand[]> menuCommandProvider = null)
            {
                return new StorageBookmarkContainer(this) { ActionOpen = action, IsReadOnly = isReadOnly, MenuCommandsProvider = menuCommandProvider };
            }
        }

        public partial class bookmarksContainerBookmark
        {
            public StorageBookmarkItem AsFileItem(Action<string> action, bool isReadOnly = false, Func<IFileItem, MenuCommand[]> menuCommandProvider = null)
            {
                return new StorageBookmarkItem(this)
                {
                    ActionOpen = action,
                    IsReadOnly = isReadOnly,
                    MenuCommandsProvider = menuCommandProvider,
                };
            }
        }

        public partial class libraryLibraryFolder
        {
            public async Task<IFileItem> AsFileItem()
            {
                var storage = await GetStorageFolderAsync() as Windows.Storage.StorageFolder;
                if (storage is null) return null;
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


        public interface IlibraryLibraryItem
        {
            string path { get; }
        }

        public partial class libraryLibraryArchive : IlibraryLibraryItem { }
        public partial class libraryLibraryFolder : IlibraryLibraryItem { }
        public partial class libraryLibraryNetwork : IlibraryLibraryItem { }

        public partial class libraryFolder
        {
            public libraryFolder()
            {
            }

            public async static Task<libraryFolder> GetLibraryFolderFromStorageAsync(Windows.Storage.StorageFolder folder)
            {
                //仕様変更:2020/06/12
                //これ以前はフォルダを登録すると無条件でFutureAccessListに登録していました。
                //これ以降は既に親または同一フォルダが登録済みの場合、相対パスで記録するようにしました。
                //その結果、子フォルダの側を移動などさせると追従できません。
                //最初は新規にトークンを発行するのが良いと思いましたが、確認すると結構汚くなったのでやめました。
                //補足：Storeリリース前なので仕様変更は大した問題になりません。保存データにも影響ないし。

                var result = new libraryFolder();
                var registered = await Managers.BookManager.GetTokenFromPathOrRegister(folder);
                result.token = registered.token;
                result.path = registered.path;
                return result;
            }

            public async Task<StorageFileItem> AsStorageFileItem()
            {
                var storage = await GetStorageFolderAsync();
                if (storage is null) return null;
                return new StorageFileItem(storage);
            }

            public async Task<TokenLibraryItem> AsTokenLibraryItem(Func<IFileItem, MenuCommand[]> menuCommand = null, Func<IFileItem, MenuCommand[]> menuCommandCascade = null)
            {
                var fileItem = await this.AsStorageFileItem();
                if (fileItem is null) return null;
                if (menuCommandCascade != null)
                {
                    fileItem.MenuCommandsProviderCascade = menuCommandCascade;
                    fileItem.MenuCommandsProvider = menuCommandCascade;
                }
                return new TokenLibraryItem(this, fileItem) { MenuCommandsProvider = menuCommand };
            }

            public async Task<Windows.Storage.StorageFolder> GetStorageFolderAsync()
            {
                var item = await Managers.BookManager.StorageItemGet(this.token, this.path);
                if (item is Windows.Storage.StorageFolder f) { return f; }
                else { return null; }
            }
        }
    }
}
