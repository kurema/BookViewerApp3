using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using kurema.FileExplorerControl.Models.FileItems;
using System.Threading.Tasks;
using BookViewerApp.ViewModels;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views.Bookshelf
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class BookshelfPageFileItem : Page
    {
        public BookshelfPageFileItem()
        {
            this.InitializeComponent();
        }

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is IFileItem vm)) return;
            if (!vm.IsFolder) return;
            {
                ProgressBarMain.Visibility = Visibility.Visible;
                ProgressBarMain.IsAccessKeyScope = true;
            }
            try
            {
                vm.GetChildren();
            }
            finally
            {
                ProgressBarMain.Visibility = Visibility.Collapsed;
                ProgressBarMain.IsAccessKeyScope = false;
            }
        }

        private async Task ListUpChildren(List<Bookshelf2BookViewModel[]> shelfs, IFileItem fileItem, int level)
        {
            if (!fileItem.IsFolder) return;
            try
            {
                var children = await fileItem.GetChildren();
            }
            catch
            {
                return;
            }
        }
    }
}
