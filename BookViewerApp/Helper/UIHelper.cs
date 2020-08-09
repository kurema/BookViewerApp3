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

using winui = Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;

using System.Threading.Tasks;
using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Views;

using System;
using System.Linq;
using System.IO;

namespace BookViewerApp.Helper
{
    public static partial class UIHelper
    {
        public static void SetTitle(FrameworkElement targetElement, string title)
        {
            if (((targetElement as Page)?.Frame)?.Parent is winui.Controls.TabViewItem item2)
            {
                item2.Header = title;
            }
            else if ((targetElement.Parent as Frame)?.Parent is winui.Controls.TabViewItem item)
            {
                item.Header = title;
            }
            else if (targetElement is winui.Controls.TabViewItem item3)
            {
                item3.Header = title;
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = title;
            }
        }

        public static void SetTitleByResource(FrameworkElement targetElement, string id) => SetTitle(targetElement, GetTitleByResource(id));

        public static string GetTitleByResource(string id) => ResourceManager.Loader.GetString("TabHeader/" + id);

        public static Views.TabPage GetCurrentTabPage(UIElement ui)
        {
            if ((ui?.XamlRoot?.Content as Frame)?.Content is TabPage tab)
            {
                return tab;
            }
            else if (ui?.XamlRoot?.Content is TabPage tab3)
            {
                return tab3;
            }
            //else if (Window.Current?.Content is Frame f && f.Content is TabPage tab2)
            //{
            //    return tab2;
            //}
            return null;
        }

        public static string GetFileTypeDescription(Windows.Storage.IStorageItem item)
        {
            if (item == null) return null;
            var ext = System.IO.Path.GetExtension(item.Path);
            if (item is Windows.Storage.StorageFolder f)
            {
                return kurema.FileExplorerControl.Application.ResourceLoader.Loader.GetString("FileType/Folder");
            }
            return kurema.FileExplorerControl.Models.FileItems.StorageFileItem.GetGeneralFileType(item.Path);
        }

        public async static Task RequestPurchaseAsync(Windows.ApplicationModel.Store.ProductListing product)
        {
            var loader = Managers.ResourceManager.Loader;

            var owned = LicenseManager.IsActive(product);
            if (owned)
            {
                await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/AlreadyPurchased/Message"), loader.GetString("Info/Purchase/Message/AlreadyPurchased/Title"))).ShowAsync();
            }
            else
            {
                try
                {
                    //https://docs.microsoft.com/en-us/answers/questions/3971/windowsapplicationmodelstorecurrentappsimulatorreq.html
                    //It always return NotPurchased
                    var result = await LicenseManager.RequestPurchaseAsync(product);
                    switch (result.Status)
                    {
                        case Windows.ApplicationModel.Store.ProductPurchaseStatus.Succeeded:
                            await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/Purchased/Message"), loader.GetString("Info/Purchase/Message/Purchased/Title"))).ShowAsync();
                            break;
                        case Windows.ApplicationModel.Store.ProductPurchaseStatus.AlreadyPurchased:
                            await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/AlreadyPurchased/Message"), loader.GetString("Info/Purchase/Message/AlreadyPurchased/Title"))).ShowAsync();
                            break;
                        case Windows.ApplicationModel.Store.ProductPurchaseStatus.NotFulfilled:
                            break;
                        case Windows.ApplicationModel.Store.ProductPurchaseStatus.NotPurchased:
//#if DEBUG
//                            await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/Purchased/Message"), loader.GetString("Info/Purchase/Message/Purchased/Title"))).ShowAsync();
//#endif
                            return;
                        default:
                            break;
                    }
                }
                catch
                {
                }
            }
        }

        public async static Task<kurema.FileExplorerControl.ViewModels.FileItemViewModel> GetFileItemViewModelFromRoot(string address, IEnumerable<kurema.FileExplorerControl.ViewModels.FileItemViewModel> root)
        {
            if (root == null) return null;
            var folders = root?.FirstOrDefault(a => a.Content.Tag is Storages.LibraryStorage.LibraryKind kind && kind == Storages.LibraryStorage.LibraryKind.Folders);
            if (folders == null) return null;
            if (folders.Children == null) await folders.UpdateChildren();
            var currentDir = address;
            kurema.FileExplorerControl.ViewModels.FileItemViewModel result = null;

            var pathList = new Stack<string>();
            while (true)
            {
                foreach (var item in folders.Children)
                {
                    var rel = Path.GetRelativePath(item.Path, currentDir.ToString());
                    if (Path.GetRelativePath(item.Path, currentDir.ToString()) == ".")
                    {
                        result = item;
                        goto outofwhile;
                    }
                }

                if (!string.IsNullOrEmpty(Path.GetFileName(currentDir))) pathList.Push(Path.GetFileName(currentDir));

                currentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(currentDir)) return null;
            }
        outofwhile:;

            foreach (var item in pathList)
            {
                if (result.Children == null) await result.UpdateChildren();
                result = result.Children.FirstOrDefault(a => a.Title == item);
                if (result == null) return null;
            }
            return result;
        }
    }
}
