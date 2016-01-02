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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class BookShelfPage : Page
    {
        public BookShelfPage()
        {
            this.InitializeComponent();

            this.BodyControl.ItemClicked += BodyControl_ItemClicked;
            Refresh();
        }

        public BookShelfPage(params BookShelfViewModels.BookShelfViewModel[] vm)
        {
            this.InitializeComponent();

            this.BodyControl.ItemClicked += BodyControl_ItemClicked;
            SetViewModel(vm);
        }


        private async void BodyControl_ItemClicked(object sender, BookShelfControl.ItemClickedEventArgs e)
        {
            if (e.SelectedItem is BookShelfViewModels.BookViewModel)
            {
                var book = await (e.SelectedItem as BookShelfViewModels.BookViewModel).TryGetBook();
                if (book != null && book is Books.IBookFixed)
                {
                    this.Frame.Navigate(typeof(BookFixedViewerPage),book as Books.IBookFixed);
                }
            }
            else if(e.SelectedItem is BookShelfViewModels.BookShelfViewModel){
                this.Frame.Navigate(typeof(BookFixedViewerPage), (e.SelectedItem as BookShelfViewModels.BookShelfViewModel));
            }
        }

        private async void Refresh()
        {
            var storage = await BookShelfStorage.GetBookShelves();
            await BodyControl.OpenAsync(storage.ToArray());
        }

        private void SetViewModel(params BookShelfViewModels.BookShelfViewModel[] vm)
        {
            BodyControl.SetSource(vm);
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker= new Windows.Storage.Pickers.FolderPicker();
            foreach (var ext in Books.BookManager.AvailableExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var folder = await picker.PickSingleFolderAsync();
            var storage = await BookShelfStorage.GetBookShelves();
            storage.Add(await BookShelfStorage.GetFromStorageFolder(folder));
            await BodyControl.OpenAsync(storage.ToArray());
        }
    }
}
