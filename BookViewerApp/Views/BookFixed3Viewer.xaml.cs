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

using BookViewerApp.ViewModels;
using System.Threading.Tasks;
using Windows.UI;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class BookFixed3Viewer : Page
    {
        private ViewModels.BookViewModel Binding => (BookViewModel)this.DataContext;

        public BookFixed3Viewer()
        {
            this.InitializeComponent();

            if (Binding != null) Binding.ToggleFullScreenCommand = new DelegateCommand((a) => ToggleFullScreen());
            if (Binding != null) Binding.GoToHomeCommand = new DelegateCommand((a) =>
            {
                Binding?.SaveInfo();
                this.Frame.Navigate(typeof(HomePage), null);
            });
         
            Application.Current.Suspending += (s, e) => Binding?.SaveInfo();

            OriginalTitle = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title;

            ((BookViewModel)this.DataContext).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModels.BookViewModel.Title))
                {
                    SetTitle(Binding?.Title);
                }
            };

            var brSet = (double)SettingStorage.GetValue("BackgroundBrightness");
            var br = (byte)((Application.Current.RequestedTheme == ApplicationTheme.Dark ? 1 - brSet : brSet) / 100.0 *
                             255.0);
            //this.Background = new SolidColorBrush(new Windows.UI.Color() { A = 255, B = br, G = br, R = br });
            var color = new Windows.UI.Color() { A = 255, B = br, G = br, R = br };
            this.Background = new AcrylicBrush()
            {
                BackgroundSource=AcrylicBackgroundSource.HostBackdrop,
                TintColor=color,
                FallbackColor=color,
                TintOpacity=0.8
            };

            flipView.UseTouchAnimationsForAllNavigation = (bool)SettingStorage.GetValue("ScrollAnimation");

        }

        private string OriginalTitle;

        public void SetTitle(string title)
        {
            var p = this.Parent;

            if ((this.Parent as Frame)?.Parent is Microsoft.UI.Xaml.Controls.TabViewItem item)
            {
                item.Header = title;
            }
            else
            {
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = title;
            }
        }

        public struct BookAndParentNavigationParamater
        {
            public Books.IBookFixed BookViewerModel;
            public BookShelfViewModels.BookShelfBookViewModel BookShelfModel;
            public string Title;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null) { }
            else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
            {
                var args = (Windows.ApplicationModel.Activation.IActivatedEventArgs)e.Parameter;
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    foreach (var item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
                    {
                        if (item is Windows.Storage.IStorageFile)
                        {
                            Open((Windows.Storage.IStorageFile)item);
                            break;
                        }
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
            else if (e.Parameter is BookAndParentNavigationParamater)
            {
                var param = (BookAndParentNavigationParamater)e.Parameter;
                Open(param.BookViewerModel);
                //SetBookShelfModel(param.BookShelfModel);
                if (Binding != null)
                    Binding.Title = param.Title;
            }

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            //currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested += CurrentView_BackRequested;

            if ((bool)SettingStorage.GetValue("DefaultFullScreen"))
            {
                var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                v.TryEnterFullScreenMode();
            }
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        public void SaveInfo()
        {
            Binding?.SaveInfo();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Binding?.SaveInfo();

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            v.ExitFullScreenMode();

            SetTitle(this.OriginalTitle);

            base.OnNavigatedFrom(e);
        }

        public void ToggleFullScreen()
        {
            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            if (v.IsFullScreenMode)
                v.ExitFullScreenMode();
            else
                v.TryEnterFullScreenMode();
        }

        private void Open(Windows.Storage.IStorageFile file)
        {
            Binding?.Initialize(file, this.flipView);
        }

        private void Open(Books.IBook book)
        {
            if (book is Books.IBookFixed) Binding?.Initialize((Books.IBookFixed)book, this.flipView);
        }

        private void flipView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewerController.SetControlPanelVisibility(true);
        }
    }
}
