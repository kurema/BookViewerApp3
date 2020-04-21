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
            private static string GetResourceTitle(string key) => Managers.ResourceManager.Loader.GetString("ContextMenu/"+key+"/Title");

            public static MenuCommand[] Folders(IFileItem item)
            {
                var list = new List<MenuCommand>();

                if (item is ContainerItem container)
                {
                    if (Storages.LibraryStorage.Content?.Content?.folders == null) return list.ToArray();

                    list.Add(new MenuCommand(GetResourceTitle("Folders/AddFolder"), new kurema.FileExplorerControl.Helper.DelegateAsyncCommand(async _ => {
                        var picker = new Windows.Storage.Pickers.FolderPicker();
                        picker.FileTypeFilter.Add("*");
                        var folder = await picker.PickSingleFolderAsync();
                        if (folder == null) return;

                        var foldersTemp = Storages.LibraryStorage.Content.Content.folders.ToList();
                        var folderNew = new Storages.Library.libraryFolder(folder);
                        foldersTemp.Add(folderNew);
                        Storages.LibraryStorage.Content.Content.folders = foldersTemp.ToArray();

                        container.Children.Add(await folderNew.AsFileItem());

                        await Storages.LibraryStorage.Content.SaveAsync();
                    })));

                }

                return list.ToArray();
            }
        }
    }
}
