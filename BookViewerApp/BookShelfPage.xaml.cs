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

        private async void Refresh() {
            //ToDo: Search correct way of doing this.
            var src = await BookShelfViewModels.BookShelfViewModel.GetBookShelfViewModels(false);
            BookShelfList.ItemsSource = src;
            if (src.Count > 0)
                BookShelfList.SelectedIndex = 0;
        }

        private async void BodyControl_ItemClicked(object sender, BookShelfControl.ItemClickedEventArgs e)
        {
            if (e.SelectedItem is BookShelfViewModels.BookViewModel)
            {
                var book = await (e.SelectedItem as BookShelfViewModels.BookViewModel).TryGetBook();
                if (book != null && book is Books.IBookFixed)
                {
                    var param = new BookFixedViewer2.BookAndParentNavigationParamater() { BookViewerModel = book as Books.IBookFixed, BookShelfModel = e.SelectedItem as BookShelfViewModels.BookViewModel };
                    this.Frame.Navigate(typeof(BookFixedViewer2), param);
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker= new Windows.Storage.Pickers.FolderPicker();
            foreach (var ext in Books.BookManager.AvailableExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                var rl = new Windows.ApplicationModel.Resources.ResourceLoader();

                var dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Started/Message"), rl.GetString("LoadingFolder/Title"));
                await dialog.ShowAsync();
                var storage = await BookShelfStorage.GetBookShelves();
                storage.Add(BookShelfStorage.GetFlatBookShelf(await BookShelfStorage.GetFromStorageFolder(folder)));
                //{
                //    var str=((await BookShelfStorage.GetFromStorageFolder(folder)));
                //    var bs=new BookShelfStorage.BookShelf();
                //    bs.Folders = new List<BookShelfStorage.BookContainer>() { str };
                //    storage.Add(bs);
                //}
                dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Completed/Message"), rl.GetString("LoadingFolder/Title"));
                await dialog.ShowAsync();
            }
            await BookShelfStorage.SaveAsync();
            Refresh();
        }

        private async void AppBarButton_Click_ClearBookShelfStorage(object sender, RoutedEventArgs e)
        {
            BookShelfStorage.Clear();
            Refresh();
            await BookShelfStorage.SaveAsync();
        }

        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            foreach (var ext in Books.BookManager.AvailableExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var file = await picker.PickSingleFileAsync();

            this.Frame.Navigate(typeof(BookFixedViewer2), file);
        }

        private void AppBarButton_Click_GoToInfoPage(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(InfoPage));
        }

        private void AppBarButton_Click_GoToSetting(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }
    }
}
