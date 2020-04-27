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

                if (item is ContainerItem container)
                {
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

                        var token = await folderNew.AsTokenLibraryItem(MenuFolderToken);
                        container.Children.Add(token);
                        token.Parent = container;

                        await Storages.LibraryStorage.Content.SaveAsync();
                    })));


                }

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
                        if (used.Count() == 0) token.Content.Remove();
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
                            libs.Select(a => new MenuCommand(a.title, new Helper.DelegateCommand(b =>
                            {

                            })));

                            libsToAdd.Add(new MenuCommand(GetResourceTitle("Library/New"), new Helper.DelegateCommand(a =>
                            {
                                
                            }
                                )));

                            list.Add(new MenuCommand(GetResourceTitle("StorageFolder/AddToLibrary")));
                        }
                    }
                    else
                    {

                    }
                }

                return list.ToArray();
            }
        }
    }
}
