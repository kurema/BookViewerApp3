using BookViewerApp.Managers;
using BookViewerApp.ViewModels;
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

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    [Obsolete]
    public sealed partial class BookshelfPage : Page
    {
        public BookshelfPage()
        {
            this.InitializeComponent();

            this.BodyControl.ItemClicked += BodyControl_ItemClicked;
            Refresh();

            Application.Current.Suspending += Current_Suspending;

            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title="";
        }

        private bool SecretMode
        {
            get { return _SecretMode; }
            set { _SecretMode = value;
                AppBarButtonToggleSecret.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private static bool _SecretMode = false;

        private void Refresh() {
            UpdateLoadingStatus();

            //var src = await BookshelfViewModels.BookshelfViewModel.GetBookshelfViewModels(SecretMode);
            //BookshelfList.ItemsSource = src;
            //if (src.Count > 0)
            //{
            //    BookshelfList.SelectedIndex = await GetActualCurrentLastSelectedBookshelf();
            //    WelcomeControl1.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            //    WelcomeControl1.Visibility = Visibility.Visible;
            //}
        }

        private const string LastSelectedBookshelfIndexKey = "LastSelectedBookshelfIndex";

        private int LastSelectedBookshelfIndex
        {
            get
            {
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(LastSelectedBookshelfIndexKey) && Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookshelfIndexKey] is int)
                {
                    return (int)Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookshelfIndexKey];
                }
                else { return 0; }
            }
            set
            {
                if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(LastSelectedBookshelfIndexKey))
                {
                    Windows.Storage.ApplicationData.Current.LocalSettings.CreateContainer(LastSelectedBookshelfIndexKey, Windows.Storage.ApplicationDataCreateDisposition.Always);
                }
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookshelfIndexKey] = value;
            }
        }

        private async void BodyControl_ItemClicked(object sender, BookshelfControl.ItemClickedEventArgs e)
        {
            if (e.SelectedItem is BookshelfBookViewModel bookShelf)
            {
                var book = await bookShelf.TryGetBook();
                if (book != null && book is Books.IBookFixed)
                {
                    var param = new BookFixed2Viewer.BookAndParentNavigationParamater() { BookViewerModel = book as Books.IBookFixed,
                        //BookshelfModel = e.SelectedItem as BookshelfBookViewModel, 
                        Title= (e.SelectedItem as BookshelfBookViewModel).Title };
                    this.Frame.Navigate(typeof(BookFixed2Viewer), param);
                }
            }
        }

        private void AppBarButton_Click_AddLocalDirectory(object sender, RoutedEventArgs e)
        {
        //    var picker= new Windows.Storage.Pickers.FolderPicker();
        //    foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
        //    {
        //        picker.FileTypeFilter.Add(ext);
        //    }
        //    var folder = await picker.PickSingleFolderAsync();
        //    if (folder != null)
        //    {
        //        var rl = new Windows.ApplicationModel.Resources.ResourceLoader();

        //        var dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Started/Message"), rl.GetString("LoadingFolder/Title"));
        //        await dialog.ShowAsync();
        //        var storage = await BookshelfStorage.GetBookShelves();
        //        CurrentOperationCount++;
        //        storage.Add(BookshelfStorage.GetFlatBookshelf(await BookshelfStorage.GetFromStorageFolder(folder)));
        //        CurrentOperationCount--;
        //        Refresh();
        //        if (BookshelfList.Items != null) BookshelfList.SelectedIndex = BookshelfList.Items.Count - 1;

        //        dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Completed/Message"), rl.GetString("LoadingFolder/Title"));
        //        await dialog.ShowAsync();
        //    }
        //    await BookshelfStorage.SaveAsync();
        }

        private int CurrentOperationCount
        {
            get { return _CurrentOperationCount; }
            set
            {
                _CurrentOperationCount = value;
                UpdateLoadingStatus();
            }
        }
        private static int _CurrentOperationCount = 0;
        private void UpdateLoadingStatus()
        {
            if (CurrentOperationCount == 0)
            {
                ProgressRing.Visibility = Visibility.Collapsed;
            }
            else
            {
                ProgressRing.Visibility = Visibility.Visible;
            }

        }


        private void AppBarButton_Click_ReloadBookshelf(object sender, RoutedEventArgs e)
        {
            //var cbs = (await GetCurrentBookshelf());
            //if (cbs is null) return;
            //var item= await Books.BookManager.StorageItemGet(cbs.GetFirstAccessToken());
            //var storage = await BookshelfStorage.GetBookShelves();
            //var target = storage[(await GetActualCurrentIndex())];
            //if (item is Windows.Storage.StorageFolder)
            //{
            //    CurrentOperationCount++;
            //    var result= BookshelfStorage.GetFlatBookshelf(await BookshelfStorage.GetFromStorageFolder(item as Windows.Storage.StorageFolder));
            //    result.Secret = target.Secret;
            //    var CurrentIndex = storage.IndexOf(target);
            //    if (CurrentIndex >= 0)
            //    {
            //        storage[CurrentIndex] = result;
            //    }
            //    CurrentOperationCount--;
            //    Refresh();
            //}
        }

        private void AppBarButton_Click_ClearBookshelfStorage(object sender, RoutedEventArgs e)
        {
            //BookshelfStorage.Clear();
            //Refresh();
            //await BookshelfStorage.SaveAsync();
        }

        private async void AppBarButton_Click_OpenLocalFile(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            foreach (var ext in BookManager.AvailableExtensionsArchive)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var file = await picker.PickSingleFileAsync();

            if (file != null) this.Frame.Navigate(typeof(BookFixed2Viewer), file);
        }


        private async void AppBarButton_Click_OpenLocalFile3(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            foreach (var ext in BookManager.AvailableExtensionsArchive)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var file = await picker.PickSingleFileAsync();

            if (file != null) this.Frame.Navigate(typeof(BookFixed3Viewer), file);
        }


        private void AppBarButton_Click_GoToInfoPage(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(InfoPage));
        }

        private void AppBarButton_Click_GoToSetting(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SetActualCurrentLastSelectedBookshelf();

            base.OnNavigatingFrom(e);
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SetActualCurrentLastSelectedBookshelf();
        }

        public System.Threading.Tasks.Task<int> GetActualCurrentIndex()
        {
            //var storage = await BookshelfStorage.GetBookShelves();
            //int result = -1;
            //for (int i = 0; i < storage.Count; i++)
            //{
            //    if (!storage[i].Secret || this.SecretMode) result++;
            //    if (result == BookshelfList.SelectedIndex)
            //    {
            //        return i;
            //    }
            //}
            return System.Threading.Tasks.Task.FromResult(-1);
        }

        private async void SetActualCurrentLastSelectedBookshelf()
        {
            this.LastSelectedBookshelfIndex = Math.Max(0, await GetActualCurrentIndex());
        }

        //private async System.Threading.Tasks.Task<int> GetActualCurrentLastSelectedBookshelf()
        //{
        //    var storage = await BookshelfStorage.GetBookShelves();
        //    int cnt = -1;
        //    int tgt = this.LastSelectedBookshelfIndex;
        //    for (int i = 0; i < storage.Count; i++)
        //    {
        //        if (!storage[i].Secret || this.SecretMode) cnt++;
        //        if (tgt != i) continue;
        //        if (BookshelfList.Items != null && (cnt == -1 && BookshelfList.Items.Count > 0)) { return 0; }
        //        return cnt;
        //    }
        //    if (BookshelfList.Items != null) return Math.Min(0, BookshelfList.Items.Count - 1);
        //    return -1;
        //}

        private void AppBarButton_Click_AppBarButton_Click_ClearBookshelfStorageSingle(object sender, RoutedEventArgs e)
        {
            //    var storage = await BookshelfStorage.GetBookShelves();
            //    if (storage.Count > 0 && BookshelfList.SelectedIndex > -1)
            //    {
            //        storage.RemoveAt(await GetActualCurrentIndex());
            //        await BookshelfStorage.SaveAsync();
            //    }
            //    Refresh();
        }

    //private async System.Threading.Tasks.Task<BookshelfStorage.Bookshelf> GetCurrentBookshelf()
    //{
    //    if (BookshelfList.SelectedIndex < 0) return null;
    //    var storage = await BookshelfStorage.GetBookShelves();
    //    return storage[await GetActualCurrentIndex()];
    //}

    private void AppBarButton_Click_SetThisBookshelfSecret(object sender, RoutedEventArgs e)
        {
            //var bs = (await GetCurrentBookshelf());
            //if (bs != null)
            //{
            //    bs.Secret = !bs.Secret;
            //    await BookshelfStorage.SaveAsync();
            //}
        }

        private void AppBarButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            SetActualCurrentLastSelectedBookshelf();
            SecretMode = !SecretMode;
            Refresh();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested += CurrentView_BackRequested;

            //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = "";
            Helper.UIHelper.SetTitleByResource(this, "Bookshelf");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        private void AppBarButton_Debug_1(object sender, RightTappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HomePage), null);
        }
    }
}
