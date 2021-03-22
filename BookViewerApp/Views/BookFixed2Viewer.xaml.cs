using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    [Obsolete]
    public sealed partial class BookFixed2Viewer : Page
    {
        private ViewModels.BookViewModel Binding => (BookViewModel)this.DataContext;

        public BookFixed2Viewer()
        {
            this.InitializeComponent();

            OriginalTitle = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title;

            Application.Current.Suspending += CurrentApplication_Suspending;

            if (!(bool)SettingStorage.GetValue("ShowRightmostAndLeftmost"))
            {
                this.AppBarButtonLeftmost.Visibility = Visibility.Collapsed;
                this.AppBarButtonRightmost.Visibility = Visibility.Collapsed;
            }

            var brSet = (double)SettingStorage.GetValue("BackgroundBrightness");
            var br = (byte)((Application.Current.RequestedTheme == ApplicationTheme.Dark ? 1 - brSet : brSet) / 100.0 *
                             255.0);
            this.Background = new SolidColorBrush(new Windows.UI.Color() { A = 255, B = br, G = br, R = br });

            ((BookViewModel)this.DataContext).PropertyChanged += (s, e) =>
           {
               if (e.PropertyName == nameof(ViewModels.BookViewModel.Title))
               {
                   SetTitle(Binding?.Title);
               }
           };

            flipView.UseTouchAnimationsForAllNavigation = (bool)SettingStorage.GetValue("ScrollAnimation");

            if (Binding != null)
            {
                Binding.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(BookViewModel.Loading))
                    {
                        flipView.Focus(FocusState.Programmatic);
                    }
                };
            }

            //if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                flipView.FocusVisualPrimaryBrush = new SolidColorBrush(Colors.Transparent);
                flipView.FocusVisualSecondaryBrush = new SolidColorBrush(Colors.Transparent);

                AppBarButtonBookmark.AllowFocusOnInteraction = true;
            }
        }

        private string OriginalTitle;

        private void CurrentApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SaveInfo();
        }

        private async void AppBarButton_OpenFile(object sender, RoutedEventArgs e)
        {
            var file = await BookManager.PickFile();
            if (file != null) Open(file);
        }

        private void Open(Windows.Storage.IStorageFile file)
        {
            Binding?.UpdateContainerInfo(file);
            Binding?.Initialize(file, this.flipView);
        }

        private void Open(Books.IBook book)
        {
            if (book is Books.IBookFixed) Binding?.Initialize((Books.IBookFixed)book, this.flipView);
        }

        //private void SetBookShelfModel(BookShelfBookViewModel viewModel)
        //{
        //    if (Binding != null)
        //        Binding.AsBookShelfBook = viewModel;
        //}

        public void SaveInfo()
        {
            Binding?.SaveInfo();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UIHelper.SetTitleByResource(this, "BookViewer");

            if (e?.Parameter is null) { }
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
            currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
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
            //public BookShelfBookViewModel BookShelfModel;
            public string Title;
        }

        public void SetTitle(string title)
        {
            UIHelper.SetTitle(this, title);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SaveInfo();

            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            v.ExitFullScreenMode();

            SetTitle(this.OriginalTitle);

            base.OnNavigatedFrom(e);
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
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
            if (DataContext is ViewModels.BookViewModel && e.ClickedItem != null && e.ClickedItem is ViewModels.BookmarkViewModel)
            {
                ((ViewModels.BookViewModel)this.DataContext).PageSelectedDisplay = ((ViewModels.BookmarkViewModel)e.ClickedItem).Page;
            }
        }

        private void AppBarButton_GoToBookshelf(object sender, RoutedEventArgs e)
        {
            this.SaveInfo();

            this.Frame.Navigate(typeof(BookShelfPage), null);
        }

        private void Scroller_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //This is ugly. I want to use Binding.
            var ui = (Canvas)sender;
            var rate = e.GetPosition(ui).X / ui.ActualWidth;
            if (Binding.Reversed) { rate = 1 - rate; }
            Binding.ReadRate = rate;
        }

        private void UIElement_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var ui = (Canvas)sender;
            var cp = e.GetCurrentPoint(ui);
            if (!cp.IsInContact) return;
            var rate = cp.Position.X / ui.ActualWidth;
            if (Binding.Reversed) { rate = 1 - rate; }
            Binding.ReadRate = rate;// Math.Round(rate * (Binding.PagesCount)) / (Binding.PagesCount);
            e.Handled = true;
        }

        private void FlipView_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            MakeStackPanelZoomVisibleForAWhile();
            MakeCommandBarVisibleForAWhile();
        }

        private int MakeCommandBarVisibleForAWhile_DelayCount = 0;

        private async void MakeCommandBarVisibleForAWhile()
        {
            int visibleTime = (int)((double)SettingStorage.GetValue("CommandBarShowTimespan") * 1000.0);
            if (visibleTime <= 0) return;
            CommandBar1.Visibility = Visibility.Visible;

            //CommandBar1.Foreground = new SolidColorBrush(Application.Current.RequestedTheme == ApplicationTheme.Dark ? RequestedThemeProperty.: Colors.Black);//ad-hoc fix me
            CommandBar1.Foreground = (Brush)App.Current.Resources.ThemeDictionaries["AppBarItemForegroundThemeBrush"];
            MakeCommandBarVisibleForAWhile_DelayCount++;
            await Task.Delay(visibleTime);
            MakeCommandBarVisibleForAWhile_DelayCount--;
            if (MakeCommandBarVisibleForAWhile_DelayCount == 0)
            {
                if (CommandBar1.FocusState != FocusState.Unfocused || CommandBar1.IsOpen)
                    MakeCommandBarVisibleForAWhile();
                else
                    CommandBar1.Foreground = new SolidColorBrush(Colors.Transparent);
            }
        }

        private int MakeStackPanelZoomVisibleForAWhile_DelayCount = 0;
        private async void MakeStackPanelZoomVisibleForAWhile()
        {
            int visibleTime = (int)((double)SettingStorage.GetValue("ZoomButtonShowTimespan") * 1000.0);
            if (visibleTime <= 0) { return; }
            StackPanelZoom.Visibility = Visibility.Visible;
            MakeStackPanelZoomVisibleForAWhile_DelayCount++;
            await Task.Delay(visibleTime);
            MakeStackPanelZoomVisibleForAWhile_DelayCount--;
            if (MakeStackPanelZoomVisibleForAWhile_DelayCount == 0)
                StackPanelZoom.Visibility = Visibility.Collapsed;
        }

        private void ButtonBase_ZoomIn_OnClick(object sender, RoutedEventArgs e)
        {
            if (flipView.SelectedItem is PageViewModel)
            {
                var pvm = (flipView.SelectedItem as PageViewModel);
                pvm.OnZoomRequested(pvm.ZoomFactor * 1.3f);
                MakeStackPanelZoomVisibleForAWhile();
            }
        }
        private void ButtonBase_ZoomOut_OnClick(object sender, RoutedEventArgs e)
        {
            if (flipView.SelectedItem is PageViewModel)
            {
                var pvm = (flipView.SelectedItem as PageViewModel);
                pvm.OnZoomRequested(pvm.ZoomFactor / 1.3f);
                MakeStackPanelZoomVisibleForAWhile();
            }
        }

        private void FlipView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            MakeStackPanelZoomVisibleForAWhile();
            MakeCommandBarVisibleForAWhile();
        }

        private void CommandBar1_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            MakeStackPanelZoomVisibleForAWhile();
            MakeCommandBarVisibleForAWhile();
        }
    }
}
