using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Linq;

using kurema.FileExplorerControl.Models;
using kurema.FileExplorerControl.Models.FileItems;

using System.Threading.Tasks;
using System;

namespace BookViewerApp.Helper;

public static partial class UIHelper
{
    public static class ContextMenus
    {
        private static string GetResourceTitle(string key) => Managers.ResourceManager.Loader.GetString("ContextMenu/" + key + "/Title");

        public static MenuCommand[] MenuFolders(IFileItem item)
        {
            var list = new List<MenuCommand>();

            if (Storages.LibraryStorage.Content?.Content?.folders is null) return list.ToArray();

            list.Add(new MenuCommand(GetResourceTitle("Folders/RegisterFolder"), new kurema.FileExplorerControl.Helper.DelegateAsyncCommand(async _ =>
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder is null) return;

                var foldersTemp = Storages.LibraryStorage.Content.Content.folders.ToList();
                var folderNew = await Storages.Library.libraryFolder.GetLibraryFolderFromStorageAsync(folder);
                foldersTemp.Add(folderNew);
                Storages.LibraryStorage.Content.Content.folders = foldersTemp.ToArray();

                Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Folders);

                await Storages.LibraryStorage.Content.SaveAsync();
            })));

            return list.ToArray();
        }

        public static MenuCommand[] MenuFolderToken(IFileItem item)
        {
            var list = new List<MenuCommand>();

            if (item is TokenLibraryItem token && token.Content is not null)
            {
                if (token.Content is null) return list.ToArray();
                list.Add(new MenuCommand(GetResourceTitle("Folders/UnregisterFolder"), new kurema.FileExplorerControl.Helper.DelegateAsyncCommand(async _ =>
                {
                    await Storages.LibraryStorage.Content.GetContentAsync();
                    if (Storages.LibraryStorage.Content?.Content?.folders is null) return;

                    {
                        var temp = Storages.LibraryStorage.Content.Content.folders.ToList();
                        temp.Remove(token.Content);
                        Storages.LibraryStorage.Content.Content.folders = temp.ToArray();
                    }

                    var brothers = await token.Parent.GetChildren();
                    brothers.Remove(token);

                    await Storages.LibraryStorage.Content.SaveAsync();
                    Storages.LibraryStorage.GarbageCollectToken();
                }
                )));

                var result = GetAddLibraryMenu(() => Task.FromResult(new Storages.Library.libraryLibraryFolder()
                {
                    path = token.Content.path ?? "",
                    token = token.Content.token
                }), token.Name);
                if (result is not null) list.Add(result);

            }
            return list.ToArray();
        }

        public static MenuCommand GetAddLibraryMenu(Windows.Storage.IStorageItem content, string path)
        {
            return GetAddLibraryMenu(async () => await Managers.BookManager.GetTokenFromPathOrRegister(content), System.IO.Path.GetFileName(path));
        }

        public static MenuCommand GetAddLibraryMenu(Func<Task<Storages.Library.libraryLibraryFolder>> tokenLfGetter, string newLibraryName)
        {
            var libs = Storages.LibraryStorage.Content?.Content?.libraries;
            if (libs is null) return null;

            var libsToAdd = new List<MenuCommand>();
            var commandsToAdd = libs.Select(a => new MenuCommand(a.title, new DelegateCommand(async b =>
            {
                var tokenLf = await tokenLfGetter?.Invoke();
                //await Managers.BookManager.GetTokenFromPathOrRegister(content);
                if (a.Items is null) a.Items = new object[0];
                if (tokenLf != null && a.Items.Any(c => (c as Storages.Library.libraryLibraryFolder)?.Compare(tokenLf) == true))
                {
                    var message = Managers.ResourceManager.Loader.GetString("ContextMenu/StorageFolder/AddToLibrary/AlreadyRegistered/MessageDialog/Message");
                    var title = Managers.ResourceManager.Loader.GetString("ContextMenu/StorageFolder/AddToLibrary/AlreadyRegistered/MessageDialog/Title");
                    var dlg = new Windows.UI.Popups.MessageDialog($"{message}", title);
                    dlg.Commands.Add(new Windows.UI.Popups.UICommand(Managers.ResourceManager.Loader.GetString("Word/OK"), null, "ok"));
                    dlg.DefaultCommandIndex = 0;
                    try
                    {
                        var res = await dlg.ShowAsync();
                    }
                    catch { }
                    return;
                }
                {
                    var items = a.Items;
                    Array.Resize(ref items, items.Length + 1);
                    items[items.Length - 1] = tokenLf;
                    a.Items = items;
                }
                Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                await Storages.LibraryStorage.Content.SaveAsync();
            })));
            foreach (var t in commandsToAdd) libsToAdd.Add(t);

            libsToAdd.Add(new MenuCommand(GetResourceTitle("Library/New"), new DelegateCommand(async a =>
            {
                var tokenLf = await tokenLfGetter?.Invoke();
                {
                    var currentLibs = Storages.LibraryStorage.Content.Content.libraries;
                    Array.Resize(ref currentLibs, currentLibs.Length + 1);
                    currentLibs[currentLibs.Length - 1] = new Storages.Library.libraryLibrary()
                    {
                        Items = new object[] { tokenLf },
                        title = newLibraryName,
                    };
                    Storages.LibraryStorage.Content.Content.libraries = currentLibs;
                }
                Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                await Storages.LibraryStorage.Content.SaveAsync();
            })));

            return new MenuCommand(GetResourceTitle("StorageFolder/AddToLibrary"), libsToAdd.ToArray());
        }

        public static Func<IFileItem, MenuCommand[]> MenuStorage(Func<Views.TabPage> tabPageProvider)
        {
            return (item) =>
            {
                var list = new List<MenuCommand>();

                if (item is StorageFileItem file)
                {
                    if (file.IsFolder)
                    {
                        var result = GetAddLibraryMenu(file?.Content, file.Path);
                        if (result is not null) list.Add(result);
                    }
                    else
                    {
                        var bookType = Managers.BookManager.GetBookTypeByPath(file.Path);
                        if (bookType != null && bookType != Managers.BookManager.BookType.Epub)
                        {
                            if (file.Content is Windows.Storage.IStorageFile sfile)
                            {
                                list.Add(new MenuCommand(GetResourceTitle("Book/SelectThumbnail"), new DelegateCommand(async (_) =>
                                {
                                    var dialog = new ContentDialog();
                                    var book = await Managers.BookManager.GetBookFromFile(sfile) as Books.IBookFixed;//XamlRoot should be specified here...
                                    if (book is null) return;
                                    var page = new Views.ThumbnailSelectionPage();
                                    page.Book = book;
                                    dialog.Content = page;
                                    dialog.CloseButtonText = Managers.ResourceManager.Loader.GetString("Word/Close");
                                    try
                                    {
                                        await dialog.ShowAsync();
                                    }
                                    catch
                                    {
                                    }
                                    file.OnUpdate();
                                })));
                            }
                        }
                        {
                            list.Add(new MenuCommand(Managers.ResourceManager.LoaderFileExplorer.GetString("ContextMenu/OpenWith/Title"),
                                GetOpenWithOptions(file.Path, tabPageProvider, () => Task.FromResult(file?.Content as Windows.Storage.IStorageFile)).ToArray()));
                        }
                    }
                }

                return list.ToArray();
            };
        }

        public static MenuCommand[] MenuHistories(IFileItem item)
        {
            var result = new List<MenuCommand>();
            if ((bool)Storages.SettingStorage.GetValue("ShowHistories"))
            {
                result.Add(new MenuCommand(GetResourceTitle("Histories/HideHistores"), new DelegateCommand(a =>
                {
                    Storages.SettingStorage.SetValue("ShowHistories", false);
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.History);
                })));
            }
            else
            {
                result.Add(new MenuCommand(GetResourceTitle("Histories/ShowHistores"), new DelegateCommand(a =>
                {
                    Storages.SettingStorage.SetValue("ShowHistories", true);
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.History);
                })));
            }
            //result.Add(new MenuCommand(GetResourceTitle("Histories/ClearHistores"), new Helper.DelegateCommand(async a =>
            //{
            //    Storages.HistoryStorage.Content.Content = new Storages.HistoryStorage.HistoryInfo[0];
            //    await Storages.HistoryStorage.Content.SaveAsync();
            //    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.History);
            //})));
            return result.ToArray();
        }

        public static Func<IFileItem, MenuCommand[]> MenuBookmarks(Action<string, Storages.LibraryStorage.BookmarkActionType> bookmarkAction)
        {
            return (item) =>
            {
                var result = new List<MenuCommand>();
                if (item is StorageBookmarkContainer container && !container.IsReadOnly)
                {
                    result.Add(new MenuCommand(GetResourceTitle("Word/New"),
                        new MenuCommand(Managers.ResourceManager.Loader.GetString("Word/Folder"), new DelegateCommand(async a =>
                        {
                            var items = container?.Content?.Items?.ToList() ?? new List<object>();
                            items.Add(new Storages.Library.bookmarksContainer() { created = DateTime.Now, title = Managers.ResourceManager.Loader.GetString("ContextMenu/Word/NewContaiener/Title") });
                            container.Content.Items = items.ToArray();
                            await container.GetChildren();
                        })),
                        new MenuCommand(GetResourceTitle("Word/New/Bookmark"), new DelegateCommand(async a =>
                        {
                            var dialog = new Views.BookmarkContentDialog();
                            ContentDialogResult result;
                            try
                            {
                                result = await dialog.ShowAsync();
                            }
                            catch { return; }
                            if (result == ContentDialogResult.Primary)
                            {
                                var items = container?.Content?.Items?.ToList() ?? new List<object>();
                                items.Add(dialog.GetLibraryBookmark());
                                container.Content.Items = items.ToArray();
                                await container.GetChildren();
                            }
                        })
                        ))
                        );
                }
                else if (item is StorageBookmarkItem bookmarkItem)
                {
                    if (bookmarkItem.Content != null)
                    {
                        if (!bookmarkItem.IsReadOnly)
                        {
                            result.Add(new MenuCommand(GetResourceTitle("Bookmarks/Edit"), async a =>
                            {
                                var dialog = new Views.BookmarkContentDialog()
                                {
                                    AddressBookmark = bookmarkItem.Content.url,
                                    TitleBookmark = bookmarkItem.Content.title,
                                };

                                ContentDialogResult result;
                                try
                                {
                                    result = await dialog.ShowAsync();
                                }
                                catch { return; }
                                if (result == ContentDialogResult.Primary)
                                {
                                    bookmarkItem.Content.url = dialog.AddressBookmark;
                                    bookmarkItem.Content.title = dialog.TitleBookmark;

                                    bookmarkItem.OnUpdate();
                                }
                            }));
                        }
                        if ((bool)Storages.SettingStorage.GetValue("DefaultBrowserExternal"))
                        {
                            result.Add(new MenuCommand(GetResourceTitle("Bookmarks/OpenInternal"), a =>
                            {
                                bookmarkAction(bookmarkItem.TargetUrl, Storages.LibraryStorage.BookmarkActionType.Internal);
                            }));
                        }
                        else
                        {
                            result.Add(new MenuCommand(GetResourceTitle("Bookmarks/OpenExternal"), async a =>
                            {
                                await OpenWebExternal(bookmarkItem.TargetUrl);
                            }));
                        }
                    }
                }
                else if (item.Tag is Storages.LibraryStorage.LibraryKind kind && kind == Storages.LibraryStorage.LibraryKind.Bookmarks)
                {
                    result.Add(GetMenuBookmarkShowPreset());
                    result.Add(new MenuCommand(GetResourceTitle("Word/New"),
                        new MenuCommand(Managers.ResourceManager.Loader.GetString("Word/Folder"), new DelegateCommand(a =>
                        {
                            Storages.LibraryStorage.OperateBookmark(b =>
                            {
                                b?.Add(new Storages.Library.bookmarksContainer() { created = DateTime.Now, title = Managers.ResourceManager.Loader.GetString("ContextMenu/Word/NewContaiener/Title") });
                                return Task.CompletedTask;
                            });
                        })),
                        new MenuCommand(GetResourceTitle("Word/New/Bookmark"), new DelegateCommand(async a =>
                        {
                            var dialog = new Views.BookmarkContentDialog();
                            ContentDialogResult result;
                            try
                            {
                                result = await dialog.ShowAsync();
                            }
                            catch { return; }
                            if (result == ContentDialogResult.Primary)
                            {
                                Storages.LibraryStorage.OperateBookmark(b =>
                                {
                                    b?.Add(dialog.GetLibraryBookmark());
                                    return Task.CompletedTask;
                                });
                            }
                        })
                        ))
                        );
                }

                return result.ToArray();
            };
        }

        public static MenuCommand[] MenuBookmarkPreset(IFileItem item)
        {
            var result = new List<MenuCommand>();
            result.Add(GetMenuBookmarkShowPreset());
            return result.ToArray();
        }

        public static MenuCommand GetMenuBookmarkShowPreset()
        {
            if ((bool)Storages.SettingStorage.GetValue("ShowPresetBookmarks"))
            {
                return new MenuCommand(GetResourceTitle("Bookmarks/HidePreset"), new DelegateCommand(a =>
                {
                    Storages.SettingStorage.SetValue("ShowPresetBookmarks", false);
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Bookmarks);
                }));
            }
            else
            {
                return new MenuCommand(GetResourceTitle("Bookmarks/ShowPreset"), new DelegateCommand(a =>
                {
                    Storages.SettingStorage.SetValue("ShowPresetBookmarks", true);
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Bookmarks);
                }));
            }

        }

        public static Func<IFileItem, MenuCommand[]> GetMenuHistoryMRU(System.Windows.Input.ICommand pathRequestCommand, Func<Views.TabPage> tabPageProvider)
        {
            return (item) =>
            {
                var result = new List<MenuCommand>();
                if (item is HistoryMRUItem item1)
                {
                    if (item1.IsParentAccessible)
                    {
                        var command = new MenuCommand(GetResourceTitle("Histories/OpenParent"), new DelegateCommand(async a =>
                        {
                            try
                            {
                                var file = await item1.GetFile();
                                if (file is null)
                                {
                                    item1.IsParentAccessible = false;
                                    return;
                                }
                                var parent = await file.GetParentAsync();
                                if (parent is null)
                                {
                                    item1.IsParentAccessible = false;
                                    return;
                                }
                                if (pathRequestCommand?.CanExecute(parent.Path) == true) pathRequestCommand.Execute(parent.Path);
                            }
                            catch { }
                        }));
                        result.Add(command);
                    }
                    {
                        result.Add(new MenuCommand(Managers.ResourceManager.LoaderFileExplorer.GetString("ContextMenu/OpenWith/Title"),
                            GetOpenWithOptions(item1.Name, tabPageProvider, async () => await item1.GetFile()).ToArray()));
                    }
                }
                return result.ToArray();
            };
        }

        public static IEnumerable<MenuCommand> GetOpenWithOptions(string fileName, Func<Views.TabPage> tabPageProvider, Func<Task<Windows.Storage.IStorageFile>> getFile)
        {
            var ext = System.IO.Path.GetExtension(fileName).ToUpperInvariant();
            if (getFile is null) yield break;
            if (tabPageProvider is null) yield break;
            if (ext == ".PDF")
            {
                if (kurema.BrowserControl.Helper.Functions.IsWebView2Supported)
                {
                    yield return new MenuCommand(Managers.ResourceManager.Loader.GetString("TabHeader/PdfJs"), new DelegateCommand(async _ =>
                    {
                        var tab = tabPageProvider?.Invoke();
                        if (tab is null) return;
                        var file = await getFile();
                        if (file is null) return;
                        tab.OpenTabPdfJs(file);
                    }));
                    yield return new MenuCommand(Managers.ResourceManager.Loader.GetString("TabHeader/Browser"), new DelegateCommand(async _ =>
                    {
                        var tab = tabPageProvider?.Invoke();
                        if (tab is null) return;
                        var file = await getFile();
                        if (file is null) return;
                        tab.OpenTabBrowser(file);
                    }));
                }
            }
            else if (ext == ".EPUB")
            {
                yield return new MenuCommand(Managers.ResourceManager.Loader.GetString("Setting_EpubViewerType/Enums/EpubViewerType/Bibi/Title"), new DelegateCommand(async _ =>
                {
                    var tab = tabPageProvider?.Invoke();
                    if (tab is null) return;
                    var file = await getFile();
                    if (file is null) return;
                    tab.OpenTabBook(file, Storages.SettingStorage.SettingEnums.EpubViewerType.Bibi);
                }));
                yield return new MenuCommand(Managers.ResourceManager.Loader.GetString("Setting_EpubViewerType/Enums/EpubViewerType/EpubJsReader/Title"), new DelegateCommand(async _ =>
                {
                    var tab = tabPageProvider?.Invoke();
                    if (tab is null) return;
                    var file = await getFile();
                    if (file is null) return;
                    tab.OpenTabBook(file, Storages.SettingStorage.SettingEnums.EpubViewerType.EpubJsReader);
                }));
            }
            {
                yield return new MenuCommand(Managers.ResourceManager.LoaderFileExplorer.GetString("ContextMenu/OpenWith/Choose"), new DelegateCommand(async _ =>
                {
                    var file = await getFile();
                    if (file is null) return;
                    await Windows.System.Launcher.LaunchFileAsync(file, new Windows.System.LauncherOptions() { DisplayApplicationPicker = true });
                }));
            }
        }

        public static Func<IFileItem, MenuCommand[]> GetMenuHistory(System.Windows.Input.ICommand pathRequestCommand)
        {
            return (item) =>
            {
                var result = new List<MenuCommand>();
                if (!string.IsNullOrWhiteSpace(item?.Path) && pathRequestCommand?.CanExecute(System.IO.Directory.GetParent(item.Path)) == true)
                {
                    result.Add(new MenuCommand(GetResourceTitle("Histories/OpenParent"), new DelegateCommand(a =>
                     {
                         var parent = System.IO.Directory.GetParent(item.Path);
                         if (pathRequestCommand?.CanExecute(parent) == true) pathRequestCommand.Execute(parent);
                     }, a =>
                     {
                         var parent = System.IO.Directory.GetParent(item.Path);
                         return pathRequestCommand?.CanExecute(parent) == true;
                     }
                     )));
                }

                return result.ToArray();
            };
        }

        public static MenuCommand[] MenuLibraryContainer(IFileItem item)
        {
            if (item is ContainerDelegateItem && item.Tag is Storages.LibraryStorage.LibraryKind kind && kind == Storages.LibraryStorage.LibraryKind.Library)
            {
                var result = new List<MenuCommand>();

                result.Add(new MenuCommand(GetResourceTitle("Library/AddNew"), new Helper.DelegateCommand(async (a) =>
                {
                    Storages.Library.libraryLibrary library;
                    {
                        var currentLibs = Storages.LibraryStorage.Content.Content.libraries;
                        Array.Resize(ref currentLibs, currentLibs.Length + 1);
                        currentLibs[currentLibs.Length - 1] = library = new Storages.Library.libraryLibrary()
                        {
                            Items = new object[0],
                            title = GetResourceTitle("Library/New")
                        };
                        Storages.LibraryStorage.Content.Content.libraries = currentLibs;
                    }
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);

                    var dialog = new ContentDialog()
                    {
                        PrimaryButtonText = Managers.ResourceManager.Loader.GetString("Word/OK"),
                    };
                    var vm = new ViewModels.LibraryMemberViewModel();
                    await vm.LoadContent(library);
                    dialog.Content = new Views.LibraryManagerControl()
                    {
                        DataContext = vm
                    };
                    try
                    {
                        await dialog.ShowAsync();
                    }
                    catch
                    {
                        return;
                    }
                    await Storages.LibraryStorage.Content.SaveAsync();
                })));

                return result.ToArray();
            }
            else
            {
                return new MenuCommand[0];
            }
        }

        public static Func<IFileItem, MenuCommand[]> GetMenuLibrary(Storages.Library.libraryLibrary library)
        {
            return (item) =>
            {
                var result = new List<MenuCommand>();
                result.Add(new MenuCommand(GetResourceTitle("Library/Unregister"), new DelegateCommand(async a =>
                {
                    var libs = Storages.LibraryStorage.Content?.Content?.libraries?.ToList();
                    if (libs is null) return;
                    libs.Remove(library);
                    Storages.LibraryStorage.Content.Content.libraries = libs.ToArray();
                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                    Storages.LibraryStorage.GarbageCollectToken();
                    await Storages.LibraryStorage.Content.SaveAsync();
                }, a => Storages.LibraryStorage.Content?.Content?.libraries != null && Storages.LibraryStorage.Content.Content.libraries.Contains(library))));

                result.Add(new MenuCommand(GetResourceTitle("Library/Manage"), new DelegateCommand(async a =>
                {
                    var dialog = new ContentDialog()
                    {
                        PrimaryButtonText = Managers.ResourceManager.Loader.GetString("Word/OK"),
                    };
                    var vm = new ViewModels.LibraryMemberViewModel();
                    await vm.LoadContent(library);
                    dialog.Content = new Views.LibraryManagerControl()
                    {
                        DataContext = vm,
                    };
                    try
                    {
                        await dialog.ShowAsync();
                    }
                    catch { }
                    await Storages.LibraryStorage.Content.SaveAsync();
                })));

                return result.ToArray();
            };
        }
    }
}
