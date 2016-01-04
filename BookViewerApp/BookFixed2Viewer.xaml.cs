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
    public sealed partial class BookFixedViewer2 : Page
    {
        public BookFixedViewer2()
        {
            this.InitializeComponent();

            Application.Current.Suspending += CurrentApplication_Suspending;
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
            if (e.Parameter != null && e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
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
            else if (e.Parameter != null && e.Parameter is Books.IBookFixed)
            {
                Open((Books.IBookFixed)e.Parameter);
            }
            else if (e.Parameter != null && e.Parameter is Windows.Storage.IStorageFile)
            {
                Open((Windows.Storage.IStorageFile)e.Parameter);
            }

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Frame.CanGoBack ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed; currentView.BackRequested += CurrentView_BackRequested;
            currentView.BackRequested += CurrentView_BackRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SaveInfo();

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

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
    }
}
