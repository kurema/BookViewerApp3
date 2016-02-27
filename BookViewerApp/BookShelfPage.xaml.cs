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

            Application.Current.Suspending += Current_Suspending;
        }

        private bool SecretMode
        {
            get { return _SecretMode; }
            set { _SecretMode = value;
                AppBarButtonToggleSecret.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        static private bool _SecretMode = false;

        private async void Refresh() {
            UpdateLoadingStatus();

            var src = await BookShelfViewModels.BookShelfViewModel.GetBookShelfViewModels(SecretMode);
            BookShelfList.ItemsSource = src;
            if (src.Count > 0)
            {
                BookShelfList.SelectedIndex = await GetActualCurrentLastSelectedBookShelf();
            }
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
            if (e.SelectedItem is BookShelfViewModels.BookViewModel)
            {
                var book = await (e.SelectedItem as BookShelfViewModels.BookViewModel).TryGetBook();
                if (book != null && book is Books.IBookFixed)
                {
                    var param = new BookFixed2Viewer.BookAndParentNavigationParamater() { BookViewerModel = book as Books.IBookFixed, BookShelfModel = e.SelectedItem as BookShelfViewModels.BookViewModel };
                    this.Frame.Navigate(typeof(BookFixed2Viewer), param);
                }
            }
        }

        private async void AppBarButton_Click_AddLocalDirectory(object sender, RoutedEventArgs e)
        {
            var picker= new Windows.Storage.Pickers.FolderPicker();
            foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
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
                CurrentOperationCount++;
                storage.Add(BookShelfStorage.GetFlatBookShelf(await BookShelfStorage.GetFromStorageFolder(folder)));
                CurrentOperationCount--;
                Refresh();
                BookShelfList.SelectedIndex = BookShelfList.Items.Count - 1;

                dialog = new Windows.UI.Popups.MessageDialog(rl.GetString("LoadingFolder/Completed/Message"), rl.GetString("LoadingFolder/Title"));
                await dialog.ShowAsync();
            }
            await BookShelfStorage.SaveAsync();
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


        private async void AppBarButton_Click_ReloadBookShelf(object sender, RoutedEventArgs e)
        {
            var cbs = (await GetCurrentBookShelf());
            if (cbs == null) return;
            var item= await Books.BookManager.StorageItemGet(cbs.GetFirstAccessToken());
            var storage = await BookShelfStorage.GetBookShelves();
            var target = storage[(await GetActualCurrentIndex())];
            if (item is Windows.Storage.StorageFolder)
            {
                CurrentOperationCount++;
                var result= BookShelfStorage.GetFlatBookShelf(await BookShelfStorage.GetFromStorageFolder(item as Windows.Storage.StorageFolder));
                var CurrentIndex = storage.IndexOf(target);
                if (CurrentIndex >= 0)
                {
                    storage[CurrentIndex] = result;
                }
                CurrentOperationCount--;
                Refresh();
            }
        }

        private async void AppBarButton_Click_ClearBookShelfStorage(object sender, RoutedEventArgs e)
        {
            BookShelfStorage.Clear();
            Refresh();
            await BookShelfStorage.SaveAsync();
        }

        private async void AppBarButton_Click_OpenLocalFile(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var file = await picker.PickSingleFileAsync();

            if (file != null) this.Frame.Navigate(typeof(BookFixed2Viewer), file);
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

        public async System.Threading.Tasks.Task<int> GetActualCurrentIndex()
        {
            var storage = await BookShelfStorage.GetBookShelves();
            int result = -1;
            for (int i = 0; i < storage.Count; i++)
            {
                if (!storage[i].Secret || this.SecretMode) result++;
                if (result == BookShelfList.SelectedIndex)
                {
                    return i;
                }
            }
            return -1;
        }

        private async void SetActualCurrentLastSelectedBookShelf()
        {
            this.LastSelectedBookShelfIndex = Math.Max(0, await GetActualCurrentIndex());
        }

        private async System.Threading.Tasks.Task<int> GetActualCurrentLastSelectedBookShelf()
        {
            var storage = await BookShelfStorage.GetBookShelves();
            int cnt = -1;
            int tgt = this.LastSelectedBookShelfIndex;
            for (int i = 0; i < storage.Count; i++)
            {
                if (!storage[i].Secret || this.SecretMode) cnt++;
                if (tgt == i)
                {
                    if (cnt == -1 && BookShelfList.Items.Count > 0) { return 0; }
                    return cnt;
                }
            }
            return Math.Min(0, BookShelfList.Items.Count - 1);
        }

        private async void AppBarButton_Click_AppBarButton_Click_ClearBookShelfStorageSingle(object sender, RoutedEventArgs e)
        {
            var storage = await BookShelfStorage.GetBookShelves();
            storage.RemoveAt(await GetActualCurrentIndex());
            await BookShelfStorage.SaveAsync();
            Refresh();
        }

        private async System.Threading.Tasks.Task<BookShelfStorage.BookShelf> GetCurrentBookShelf()
        {
            if (BookShelfList.SelectedIndex < 0) return null;
            var storage = await BookShelfStorage.GetBookShelves();
            return storage[await GetActualCurrentIndex()];
        }

        private async void AppBarButton_Click_SetThisBookShelfSecret(object sender, RoutedEventArgs e)
        {
            var bs = (await GetCurrentBookShelf());
            bs.Secret = !bs.Secret;
            await BookShelfStorage.SaveAsync();
        }

        private void AppBarButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            SetActualCurrentLastSelectedBookShelf();
            SecretMode = !SecretMode;
            Refresh();
        }
    }
}
