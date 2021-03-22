﻿using BookViewerApp.Managers;
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
    public sealed partial class BookShelfPage : Page
    {
        public BookShelfPage()
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

            //var src = await BookShelfViewModels.BookShelfViewModel.GetBookShelfViewModels(SecretMode);
            //BookShelfList.ItemsSource = src;
            //if (src.Count > 0)
            //{
            //    BookShelfList.SelectedIndex = await GetActualCurrentLastSelectedBookShelf();
            //    WelcomeControl1.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            //    WelcomeControl1.Visibility = Visibility.Visible;
            //}
        }

        private const string LastSelectedBookShelfIndexKey = "LastSelectedBookShelfIndex";

        private int LastSelectedBookShelfIndex
        {
            get
            {
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(LastSelectedBookShelfIndexKey) && Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookShelfIndexKey] is int)
                {
                    return (int)Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookShelfIndexKey];
                }
                else { return 0; }
            }
            set
            {
                if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(LastSelectedBookShelfIndexKey))
                {
                    Windows.Storage.ApplicationData.Current.LocalSettings.CreateContainer(LastSelectedBookShelfIndexKey, Windows.Storage.ApplicationDataCreateDisposition.Always);
                }
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[LastSelectedBookShelfIndexKey] = value;
            }
        }

        private async void BodyControl_ItemClicked(object sender, BookShelfControl.ItemClickedEventArgs e)
        {
            if (e.SelectedItem is BookShelfBookViewModel bookShelf)
            {
                var book = await bookShelf.TryGetBook();
                if (book != null && book is Books.IBookFixed)
                {
                    var param = new BookFixed2Viewer.BookAndParentNavigationParamater() { BookViewerModel = book as Books.IBookFixed,
                        //BookShelfModel = e.SelectedItem as BookShelfBookViewModel, 
                        Title= (e.SelectedItem as BookShelfBookViewModel).Title };
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
        //        var storage = await BookShelfStorage.GetBookShelves();
        //        CurrentOperationCount++;
        //        storage.Add(BookShelfStorage.GetFlatBookShelf(await BookShelfStorage.GetFromStorageFolder(folder)));
        //        CurrentOperationCount--;
        //        Refresh();
        //        if (BookShelfList.Items != null) BookShelfList.SelectedIndex = BookShelfList.Items.Count - 1;

        //        dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Completed/Message"), rl.GetString("LoadingFolder/Title"));
        //        await dialog.ShowAsync();
        //    }
        //    await BookShelfStorage.SaveAsync();
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


        private void AppBarButton_Click_ReloadBookShelf(object sender, RoutedEventArgs e)
        {
            //var cbs = (await GetCurrentBookShelf());
            //if (cbs is null) return;
            //var item= await Books.BookManager.StorageItemGet(cbs.GetFirstAccessToken());
            //var storage = await BookShelfStorage.GetBookShelves();
            //var target = storage[(await GetActualCurrentIndex())];
            //if (item is Windows.Storage.StorageFolder)
            //{
            //    CurrentOperationCount++;
            //    var result= BookShelfStorage.GetFlatBookShelf(await BookShelfStorage.GetFromStorageFolder(item as Windows.Storage.StorageFolder));
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

        private void AppBarButton_Click_ClearBookShelfStorage(object sender, RoutedEventArgs e)
        {
            //BookShelfStorage.Clear();
            //Refresh();
            //await BookShelfStorage.SaveAsync();
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
            SetActualCurrentLastSelectedBookShelf();

            base.OnNavigatingFrom(e);
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SetActualCurrentLastSelectedBookShelf();
        }

        public System.Threading.Tasks.Task<int> GetActualCurrentIndex()
        {
            //var storage = await BookShelfStorage.GetBookShelves();
            //int result = -1;
            //for (int i = 0; i < storage.Count; i++)
            //{
            //    if (!storage[i].Secret || this.SecretMode) result++;
            //    if (result == BookShelfList.SelectedIndex)
            //    {
            //        return i;
            //    }
            //}
            return System.Threading.Tasks.Task.FromResult(-1);
        }

        private async void SetActualCurrentLastSelectedBookShelf()
        {
            this.LastSelectedBookShelfIndex = Math.Max(0, await GetActualCurrentIndex());
        }

        //private async System.Threading.Tasks.Task<int> GetActualCurrentLastSelectedBookShelf()
        //{
        //    var storage = await BookShelfStorage.GetBookShelves();
        //    int cnt = -1;
        //    int tgt = this.LastSelectedBookShelfIndex;
        //    for (int i = 0; i < storage.Count; i++)
        //    {
        //        if (!storage[i].Secret || this.SecretMode) cnt++;
        //        if (tgt != i) continue;
        //        if (BookShelfList.Items != null && (cnt == -1 && BookShelfList.Items.Count > 0)) { return 0; }
        //        return cnt;
        //    }
        //    if (BookShelfList.Items != null) return Math.Min(0, BookShelfList.Items.Count - 1);
        //    return -1;
        //}

        private void AppBarButton_Click_AppBarButton_Click_ClearBookShelfStorageSingle(object sender, RoutedEventArgs e)
        {
            //    var storage = await BookShelfStorage.GetBookShelves();
            //    if (storage.Count > 0 && BookShelfList.SelectedIndex > -1)
            //    {
            //        storage.RemoveAt(await GetActualCurrentIndex());
            //        await BookShelfStorage.SaveAsync();
            //    }
            //    Refresh();
        }

    //private async System.Threading.Tasks.Task<BookShelfStorage.BookShelf> GetCurrentBookShelf()
    //{
    //    if (BookShelfList.SelectedIndex < 0) return null;
    //    var storage = await BookShelfStorage.GetBookShelves();
    //    return storage[await GetActualCurrentIndex()];
    //}

    private void AppBarButton_Click_SetThisBookShelfSecret(object sender, RoutedEventArgs e)
        {
            //var bs = (await GetCurrentBookShelf());
            //if (bs != null)
            //{
            //    bs.Secret = !bs.Secret;
            //    await BookShelfStorage.SaveAsync();
            //}
        }

        private void AppBarButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            SetActualCurrentLastSelectedBookShelf();
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
