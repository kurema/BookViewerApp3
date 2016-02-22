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
    public sealed partial class BookFixed2Viewer : Page
    {
        public BookFixed2Viewer()
        {
            this.InitializeComponent();

            Application.Current.Suspending += CurrentApplication_Suspending;

            if (!(bool)SettingStorage.GetValue("ShowRightmostAndLeftmost"))
            {
                this.AppBarButtonLeftmost.Visibility = Visibility.Collapsed;
                this.AppBarButtonRightmost.Visibility = Visibility.Collapsed;
            }
        }

        private void CurrentApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SaveInfo();
        }

        private async void AppBarButton_OpenFile(object sender, RoutedEventArgs e)
        {
            var book = await Books.BookManager.PickBook();
            Open(book);
        }

        private void Open(Books.IBook book)
        {
            if (book != null && book is Books.IBookFixed) (this.DataContext as BookFixed2ViewModels.BookViewModel).Initialize(book as Books.IBookFixed, this.flipView);
        }

        private void SetBookShelfModel(BookShelfViewModels.BookViewModel ViewModel)
        {
            (this.DataContext as BookFixed2ViewModels.BookViewModel).AsBookShelfBook = ViewModel;
        }

        private async void Open(Windows.Storage.IStorageFile file)
        {
            Open(await Books.BookManager.GetBookFromFile(file));
        }

        public void SaveInfo()
        {
            (this.DataContext as BookFixed2ViewModels.BookViewModel).SaveInfo();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null) { }
            if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
            {
                var args = (Windows.ApplicationModel.Activation.IActivatedEventArgs)e.Parameter;
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    foreach (Windows.Storage.IStorageFile item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
                    {
                        Open(item);
                        break;
                    }
                }
            }
            else if (e.Parameter is Books.IBookFixed)
            {
                Open((Books.IBookFixed)e.Parameter);
            }
            else if (e.Parameter is Windows.Storage.IStorageFile)
            {
                Open((Windows.Storage.IStorageFile)e.Parameter);
            }
            else if(e. Parameter is BookAndParentNavigationParamater)
            {
                Open(((BookAndParentNavigationParamater)e.Parameter).BookViewerModel);
                SetBookShelfModel(((BookAndParentNavigationParamater)e.Parameter).BookShelfModel);
            }

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Frame.CanGoBack ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed; currentView.BackRequested += CurrentView_BackRequested;
            currentView.BackRequested += CurrentView_BackRequested;

            if ((bool)SettingStorage.GetValue("DefaultFullScreen"))
            {
                var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                v.TryEnterFullScreenMode();
            }
        }

        public struct BookAndParentNavigationParamater
        {
            public Books.IBookFixed BookViewerModel;
            public BookShelfViewModels.BookViewModel BookShelfModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SaveInfo();

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            v.ExitFullScreenMode();

            base.OnNavigatedFrom(e);
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        public void ToggleFullScreen()
        {
            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            if (v.IsFullScreenMode)
                v.ExitFullScreenMode();
            else
                v.TryEnterFullScreenMode();
        }

        private void AppBarButton_ToggleFullScreen(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void BookmarkClicked(object sender, ItemClickEventArgs e)
        {
            if (this.DataContext != null && this.DataContext is BookFixed2ViewModels.BookViewModel && e.ClickedItem != null && e.ClickedItem is BookFixed2ViewModels.BookmarkViewModel)
            {
                (this.DataContext as BookFixed2ViewModels.BookViewModel).PageSelected = (e.ClickedItem as BookFixed2ViewModels.BookmarkViewModel).Page;
            }
        }
    }
}
