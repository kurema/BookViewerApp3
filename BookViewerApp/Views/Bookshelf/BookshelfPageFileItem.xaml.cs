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

        private async void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            StackPanelMain.Children.Clear();

            if (!(args.NewValue is IFileItem vm)) return;
            if (!vm.IsFolder) return;
            {
                ProgressBarMain.Visibility = Visibility.Visible;
                ProgressBarMain.IsActive = true;
            }
            try
            {
                var result = new List<Bookshelf2BookViewModel[]>();
                await ListUpChildren(result, vm, 5);
                foreach(var item in result)
                {
                    var row = new BookRow();
                    row.LoadItems(item);
                    StackPanelMain.Children.Add(row);
                }
            }
            finally
            {
                ProgressBarMain.Visibility = Visibility.Collapsed;
                ProgressBarMain.IsActive = false;
            }
        }

        private async Task ListUpChildren(List<Bookshelf2BookViewModel[]> shelfs, IFileItem fileItem, int level)
        {
            if (!fileItem.IsFolder) return;
            if (level < 0) return;
            System.Collections.ObjectModel.ObservableCollection<IFileItem> children;
            try { children = await fileItem.GetChildren(); } catch { return; }
            var result = new List<Bookshelf2BookViewModel>();
            foreach (var item in children.Where(a => !a.IsFolder && Managers.BookManager.IsFileAvailabe(a.Path)))
            {
                var vm = new Bookshelf2BookViewModel();
                await vm.Load(new kurema.FileExplorerControl.ViewModels.FileItemViewModel(fileItem));
                result.Add(vm);
            }
            if (result.Count > 0) shelfs?.Add(result.ToArray());
            foreach (var item in children.Where(a => a.IsFolder))
            {
                await ListUpChildren(shelfs, item, level - 1);
            }
        }
    }
}
