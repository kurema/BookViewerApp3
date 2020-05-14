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

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using BookViewerApp.Views;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
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

            if (Binding != null) Binding.ToggleFullScreenCommand = new Helper.DelegateCommand((a) => ToggleFullScreen());
            if (Binding != null) Binding.GoToHomeCommand = new Helper.DelegateCommand((a) =>
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
                BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                TintColor = color,
                FallbackColor = color,
                TintOpacity = 0.8
            };

            flipView.UseTouchAnimationsForAllNavigation = (bool)SettingStorage.GetValue("ScrollAnimation");

            flipView.SelectionChanged += (s, e) =>
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is ViewModels.PageViewModel vm)
                {
                    int current = Binding?.Pages.IndexOf(vm) ?? -1;
                    var prevPage = current >= 1 ? Binding.Pages[current - 1] : null;
                    var prevPrevPage = current >= 2 ? Binding.Pages[current - 2] : null;

                    switch (vm.SpreadDisplayedStatus)
                    {
                        case SpreadPagePanel.DisplayedStatusEnum.Spread:
                            break;
                    }
                }

                //ToDo:見開き対応。
                //
                //1. 以前選択されていたページのイベントを取り消す。
                //2. 全ページリストアする？
                //3. 選択中のページの見開き状態から次ページを表示するか切り替える。
                //4. 選択中のページの見開き状態の変化イベントを登録する。
                //5. 前のページの見開き状態が、
                // a) 1ページなら何もしない。
                // b) 半ページなら前に半ページ後半を挿入する。
                // c) 2ページかつ2ページ前の見開き状態が2ページなら前ページを削除する。
                // d) 2ページかつ2ページ前の見開き状態が1ページなら前ページを強制1ページ表示にする。
                //6. 前ページの見開き状態の変化イベントを登録する。

                //うわしんど。
            };
        }

        private string OriginalTitle;

        public void SetTitle(string title)
        {
            UIHelper.SetTitle(this, title);
        }

        public struct BookAndParentNavigationParamater
        {
            public Books.IBookFixed BookViewerModel;
            public BookShelfBookViewModel BookShelfModel;
            public string Title;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Helper.UIHelper.SetTitleByResource(this, "BookViewer");

            if (e?.Parameter == null) { }
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
            }else if(e.Parameter is Stream stream)
            {
                throw new NotImplementedException();
            }

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            //currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested += CurrentView_BackRequested;

            if ((bool)SettingStorage.GetValue("DefaultFullScreen"))
            {
                TrySetFullScreenMode(true);
            }
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                TrySetFullScreenMode(false);
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
            TrySetFullScreenMode(false);

            SetTitle(this.OriginalTitle);

            base.OnNavigatedFrom(e);
        }

        public void ToggleFullScreen()
        {
            var p = UIHelper.GetCurrentTabPage(this);
            if (p?.RootAppWindow != null)
            {
                TrySetFullScreenMode(p.RootAppWindow.Presenter.GetConfiguration().Kind != Windows.UI.WindowManagement.AppWindowPresentationKind.FullScreen);
            }
            else
            {
                var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                TrySetFullScreenMode(!v.IsFullScreenMode);
            }
        }

        public void TrySetFullScreenMode(bool fullscreen)
        {
            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            //One line is better? Or lots of if should do?
            //var result = fullscreen && v.TryEnterFullScreenMode() && BasicFullScreenFrame == null && this.Parent is Frame && (BasicFullScreenFrame = Window.Current.Content as Frame) != null && (Window.Current.Content = this.Parent as Frame) != null;
            var p = UIHelper.GetCurrentTabPage(this);
            if (fullscreen)
            {
                if (p?.RootAppWindow != null)
                {
                    p.RootAppWindow.Presenter.RequestPresentation(Windows.UI.WindowManagement.AppWindowPresentationKind.FullScreen);

                    if (BasicFullScreenFrame == null && this.Parent is Frame)
                    {
                        //if ((BasicFullScreenFrame = p.RootAppWindow.Content as Frame) != null)
                        //    Window.Current.Content = this.Parent as Frame;
                    }

                    return;
                }

                if (v.TryEnterFullScreenMode())
                {
                    if (BasicFullScreenFrame == null && this.Parent is Frame f)
                    {
                        if ((BasicFullScreenFrame = Window.Current.Content as Frame) != null)
                            Window.Current.Content = f;
                    }
                    return;
                }
            }
            else
            {
                if (p?.RootAppWindow != null)
                {
                    p.RootAppWindow.Presenter.RequestPresentation(Windows.UI.WindowManagement.AppWindowPresentationKind.Default);
                }
                else
                {
                    v.ExitFullScreenMode();
                    RestoreFullScreenFrame();
                }
            }
        }

        Frame BasicFullScreenFrame = null;

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

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RestoreFullScreenFrame();
        }

        public void RestoreFullScreenFrame()
        {
            bool isfull = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().IsFullScreenMode;
            if (!isfull && BasicFullScreenFrame != null)
            {
                Window.Current.Content = BasicFullScreenFrame;
                BasicFullScreenFrame = null;
            }
        }

        private async void MenuFlyoutItem_Click_OpenFile(object sender, RoutedEventArgs e)
        {
            var file = await BookManager.PickFile();
            if (file != null) Binding?.Initialize(file, this.flipView);
        }

        private void flipView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var option = new FlyoutShowOptions();
            if (args.TryGetPosition(this, out Point p)) option.Position = p;
            if (sender is FrameworkElement fe && fe.Resources["ContextFlyout"] is MenuFlyout menuFlyout)
            {
                menuFlyout.ShowAt(this, option);
            }
            args.Handled = true;

        }
    }
}
