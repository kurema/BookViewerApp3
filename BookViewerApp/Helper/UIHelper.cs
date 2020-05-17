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
            else if(targetElement is winui.Controls.TabViewItem item3)
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
            if ((ui?.XamlRoot?.Content as Frame).Content is TabPage tab)
            {
                return tab;
            }
            else if (ui?.XamlRoot.Content is TabPage tab3)
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
            if(item is Windows.Storage.StorageFolder f)
            {
                return kurema.FileExplorerControl.Application.ResourceLoader.Loader.GetString("FileType/Folder");
            }
            return kurema.FileExplorerControl.Models.FileItems.StorageFileItem.GetGeneralFileType(item.Path);
        }
    }
}
