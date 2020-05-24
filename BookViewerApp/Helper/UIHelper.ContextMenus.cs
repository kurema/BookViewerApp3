﻿using System.Collections.Generic;
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

namespace BookViewerApp.Helper
{
    public static partial class UIHelper
    {
        public static class ContextMenus
        {
            private static string GetResourceTitle(string key) => Managers.ResourceManager.Loader.GetString("ContextMenu/" + key + "/Title");

            public static MenuCommand[] MenuFolders(IFileItem item)
            {
                var list = new List<MenuCommand>();

                if (Storages.LibraryStorage.Content?.Content?.folders == null) return list.ToArray();

                list.Add(new MenuCommand(GetResourceTitle("Folders/RegisterFolder"), new kurema.FileExplorerControl.Helper.DelegateAsyncCommand(async _ =>
                {
                    var picker = new Windows.Storage.Pickers.FolderPicker();
                    picker.FileTypeFilter.Add("*");
                    var folder = await picker.PickSingleFolderAsync();
                    if (folder == null) return;

                    var foldersTemp = Storages.LibraryStorage.Content.Content.folders.ToList();
                    var folderNew = new Storages.Library.libraryFolder(folder);
                    foldersTemp.Add(folderNew);
                    Storages.LibraryStorage.Content.Content.folders = foldersTemp.ToArray();

                    Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Folders);

                    await Storages.LibraryStorage.Content.SaveAsync();
                })));

                return list.ToArray();
            }

            private async static Task<bool> MenuFolderToken_ShowDialog(Storages.Library.libraryLibrary[] used)
            {
                //Not used.

                var message = Managers.ResourceManager.Loader.GetString("ContextMenu/Folders/UnregisterFolder/MessageDialog/Message");
                var title = Managers.ResourceManager.Loader.GetString("ContextMenu/Folders/UnregisterFolder/MessageDialog/Title");
                var dlg = new Windows.UI.Popups.MessageDialog($"{message}\n{used.Aggregate("", (a, b) => a + "\n" + b.title)}", title);
                dlg.Commands.Add(new Windows.UI.Popups.UICommand(Managers.ResourceManager.Loader.GetString("Word/OK"), null, "ok"));
                dlg.Commands.Add(new Windows.UI.Popups.UICommand(Managers.ResourceManager.Loader.GetString("Word/Cancel"), null, "cancel"));
                dlg.CancelCommandIndex = 1;
                dlg.DefaultCommandIndex = 0;
                var res = await dlg.ShowAsync();
                if (res.Id as string == "ok")
                {
                    return true;
                }
                return false;

            }

            public static MenuCommand[] MenuFolderToken(IFileItem item)
            {
                var list = new List<MenuCommand>();

                if (item is TokenLibraryItem token)
                {
                    if (token.Content == null) return list.ToArray();
                    list.Add(new MenuCommand(GetResourceTitle("Folders/UnregisterFolder"), new kurema.FileExplorerControl.Helper.DelegateAsyncCommand(async _ =>
                    {
                        var used = Storages.LibraryStorage.GetTokenUsedByLibrary(token.Content.token);
                        //参照元が0になってからトークンを削除するように変更したのでダイアログが不要になりました。
                        //if (used.Count() == 0) goto remove;
                        //else
                        //{
                        //    if (! await FolderToken_ShowDialog(used)) return;
                        //}

                        if (Storages.LibraryStorage.Content?.Content?.folders == null) return;

                        {
                            var temp = Storages.LibraryStorage.Content.Content.folders.ToList();
                            temp.Remove(token.Content);
                            Storages.LibraryStorage.Content.Content.folders = temp.ToArray();
                        }
                        if (used?.Length == 0) token.Content.Remove();
                        var brothers = await token.Parent.GetChildren();
                        brothers.Remove(token);

                        await Storages.LibraryStorage.Content.SaveAsync();
                    }
                    )));
                }
                return list.ToArray();
            }

            public static MenuCommand[] MenuStorage(IFileItem item)
            {
                var list = new List<MenuCommand>();

                if (item is StorageFileItem file)
                {
                    if (file.IsFolder)
                    {
                        var libs = Storages.LibraryStorage.Content?.Content?.libraries;
                        if (libs != null)
                        {
                            var libsToAdd = new List<MenuCommand>();
                            var commandsToAdd = libs.Select(a => new MenuCommand(a.title, new Helper.DelegateCommand(async b =>
                             {
                                 var tokenLf = await Managers.BookManager.GetTokenFromPathOrRegister(file?.Content);
                                 if (a.Items == null) a.Items = new object[0];
                                 if (tokenLf != null && a.Items.Any(c => (c as Storages.Library.libraryLibraryFolder)?.Compare(tokenLf) == true))
                                 {
                                     var message = Managers.ResourceManager.Loader.GetString("ContextMenu/StorageFolder/AddToLibrary/AlreadyRegistered/MessageDialog/Message");
                                     var title = Managers.ResourceManager.Loader.GetString("ContextMenu/StorageFolder/AddToLibrary/AlreadyRegistered/MessageDialog/Title");
                                     var dlg = new Windows.UI.Popups.MessageDialog($"{message}", title);
                                     dlg.Commands.Add(new Windows.UI.Popups.UICommand(Managers.ResourceManager.Loader.GetString("Word/OK"), null, "ok"));
                                     dlg.DefaultCommandIndex = 0;
                                     var res = await dlg.ShowAsync();
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

                            libsToAdd.Add(new MenuCommand(GetResourceTitle("Library/New"), new Helper.DelegateCommand(async a =>
                            {
                                var tokenLf = await Managers.BookManager.GetTokenFromPathOrRegister(file?.Content);
                                {
                                    var currentLibs = Storages.LibraryStorage.Content.Content.libraries;
                                    Array.Resize(ref currentLibs, currentLibs.Length + 1);
                                    currentLibs[currentLibs.Length - 1] = new Storages.Library.libraryLibrary()
                                    {
                                        Items = new object[] { tokenLf },
                                        title = System.IO.Path.GetFileName(file.Path),
                                    };
                                    Storages.LibraryStorage.Content.Content.libraries = currentLibs;
                                }
                                Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                                await Storages.LibraryStorage.Content.SaveAsync();
                            })));

                            list.Add(new MenuCommand(GetResourceTitle("StorageFolder/AddToLibrary"), libsToAdd.ToArray()));
                        }
                    }
                    else
                    {
                        //file
                    }
                }

                return list.ToArray();
            }

            public static MenuCommand[] MenuHistories(IFileItem item)
            {
                var result = new List<MenuCommand>();
                if ((bool)Storages.SettingStorage.GetValue("ShowHistories"))
                {
                    result.Add(new MenuCommand(GetResourceTitle("Histories/HideHistores"), new Helper.DelegateCommand(a =>
                    {
                        Storages.SettingStorage.SetValue("ShowHistories", false);
                        Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.History);
                    })));
                }
                else
                {
                    result.Add(new MenuCommand(GetResourceTitle("Histories/ShowHistores"), new Helper.DelegateCommand(a =>
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

            public static MenuCommand[] MenuBookmarks(IFileItem item)
            {
                var result = new List<MenuCommand>();
                result.Add(GetMenuBookmarkShowPreset());
                return result.ToArray();
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
                    return new MenuCommand(GetResourceTitle("Bookmarks/HidePreset"), new Helper.DelegateCommand(a =>
                    {
                        Storages.SettingStorage.SetValue("ShowPresetBookmarks", false);
                        Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Bookmarks);
                    }));
                }
                else
                {
                    return new MenuCommand(GetResourceTitle("Bookmarks/ShowPreset"), new Helper.DelegateCommand(a =>
                    {
                        Storages.SettingStorage.SetValue("ShowPresetBookmarks", true);
                        Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Bookmarks);
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
                        result.Add(new MenuCommand(GetResourceTitle("Histories/OpenParent"), new Helper.DelegateCommand(a =>
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

            public static Func<IFileItem, MenuCommand[]> GetMenuLibrary(Storages.Library.libraryLibrary library)
            {
                return (item) =>
                {
                    var result = new List<MenuCommand>();
                    result.Add(new MenuCommand(GetResourceTitle("Library/Unregister"), new DelegateCommand(async a =>
                    {
                        var libs = Storages.LibraryStorage.Content?.Content?.libraries?.ToList();
                        if (libs == null) return;
                        libs.Remove(library);
                        Storages.LibraryStorage.Content.Content.libraries = libs.ToArray();
                        Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);

                        foreach (Storages.Library.libraryLibraryFolder itemFolder in library.Items)
                        {
                            var used1 = Storages.LibraryStorage.GetTokenUsedByLibrary(itemFolder.token);
                            var used2 = Storages.LibraryStorage.GetTokenUsedByFolders(itemFolder.token);
                            if (used1.Length + used2.Length == 0)
                            {
                                Managers.BookManager.StorageItemUnregister(itemFolder.token);
                                await Storages.HistoryStorage.DeleteHistoryByToken(itemFolder.token);
                            }
                        }
                        await Storages.LibraryStorage.Content.SaveAsync();
                    }, a => Storages.LibraryStorage.Content?.Content?.libraries != null && Storages.LibraryStorage.Content.Content.libraries.Contains(library))));

                    result.Add(new MenuCommand("Manage",new DelegateCommand(async a=> {
                        var dialog = new ContentDialog() { 
                            PrimaryButtonText="OK",
                        };
                        dialog.Content = new Views.LibraryManagerControl()
                        {
                            DataContext = new ViewModels.LibraryMemberViewModel(library),
                        };
                        await dialog.ShowAsync();
                    })));

                    return result.ToArray();
                };
            }
        }
    }
}
