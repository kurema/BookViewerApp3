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
                var result = new List<(IFileItem, Bookshelf2BookViewModel[])>();
                await ListUpChildren(AddChildren, vm, () => StackPanelMain.Children.Count < 1);
            }
            finally
            {
                ProgressBarMain.Visibility = Visibility.Collapsed;
                ProgressBarMain.IsActive = false;
            }
        }

        private void AddChildren(IFileItem file, Bookshelf2BookViewModel[] vms)
        {
            if (file is null || vms is null || vms.Length == 0) return;

            var row = new BookRow()
            {
                Margin = new Thickness(30),
                Spacing = new Size(10, 10),
                Header = file.Name,
                SubHeader = file.Path,
            };
            row.LoadItems(vms, 300,500);
            StackPanelMain.Children.Add(row);
        }

        private async Task ListUpChildren(Action<IFileItem, Bookshelf2BookViewModel[]> AddShelfAction, IFileItem fileItem, Func<bool> checkContinue, int level = 2)
        {
            if (!fileItem.IsFolder) return;
            if (AddShelfAction is null) return;
            if (checkContinue?.Invoke() == false) return;
            //if (shelfs.Count >= maxItem) return;

            System.Collections.ObjectModel.ObservableCollection<IFileItem> children;
            try { children = await fileItem.GetChildren(); } catch { return; }
            var result = new List<Bookshelf2BookViewModel>();
            foreach (var item in children.Where(a => !a.IsFolder && Managers.BookManager.IsFileAvailabe(a.Path)))
            {
                var vm = new Bookshelf2BookViewModel();
                var fivm = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(item);
                fivm.IconProviders = new System.Collections.ObjectModel.ObservableCollection<kurema.FileExplorerControl.Models.IconProviders.IIconProvider>() {
                    new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async (f,cancel)=>{
                        return await Helper.UIHelper.IconProviderHelper.BookIconsBookshelf(f,cancel,this.Dispatcher);
                    }),
                };
                await vm.Load(fivm);
                result.Add(vm);
            }
            if (result.Count > 0) AddShelfAction(fileItem, result.ToArray());
            if (level <= 0) return;
            foreach (var item in children.Where(a => a.IsFolder).OrderBy((_) => Guid.NewGuid().ToString()))
            {
                await ListUpChildren(AddShelfAction, item, checkContinue, level - 1);
                if (checkContinue?.Invoke() == false) return;
                //if (shelfs.Count >= maxItem) return;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is IFileItem vm) { this.DataContext = vm; return; }

            base.OnNavigatedTo(e);
        }
    }
}
