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

using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.UI;
using Windows.Foundation.Metadata;
using static BookViewerApp.Storages.SettingStorage;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class BookFixed3Viewer : Page
{
    private BookViewModel Binding => (BookViewModel)this.DataContext;

    public FlipView FlipViewControl => this.flipView;

    public BookFixed3Viewer()
    {
        this.InitializeComponent();

        if (Binding != null) Binding.ToggleFullScreenCommand = new DelegateCommand((a) => ToggleFullScreen());
        if (Binding != null) Binding.GoToHomeCommand = new DelegateCommand((a) =>
        {
            Binding?.SaveInfo();
            Frame.Navigate(typeof(Bookshelf.NavigationPage), null);
        });

        Application.Current.Suspending += (s, e) => Binding?.SaveInfo();

        OriginalTitle = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title;

        ((BookViewModel)this.DataContext).PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(BookViewModel.Title))
            {
                SetTitle(Binding?.Title);
            }
        };

        if (ThemeManager.IsMica)
        {
            this.Background = new SolidColorBrush(Colors.Transparent);
            Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(this, true);

            //BackdropMaterial does not work on AppWindow.
            this.Loaded += (s, e) =>
            {
                var tab = UIHelper.GetCurrentTabPage(this);
                if (tab?.RootAppWindow is not null) SetDefaultBackground();
            };
        }
        else
        {
            SetDefaultBackground();
        }

        flipView.UseTouchAnimationsForAllNavigation = (bool)SettingStorage.GetValue("ScrollAnimation");

        if (Binding != null)
        {
            Binding.PagePropertyChanged += async (s, e) =>
             {
                 if ((e.Item1 == flipView.SelectedItem || e.Item1?.NextPage?.NextPage == flipView.SelectedItem) && e.Item2.PropertyName == nameof(PageViewModel.SpreadDisplayedStatus))
                 {
                     await Binding.UpdatePages(this.Dispatcher);
                 }
             };
        }

        flipView.SelectionChanged += async (s, e) =>
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is PageViewModel vm)
            {
                await Binding?.UpdatePages(this.Dispatcher);
            }
        };
    }

    private void SetDefaultBackground(double? brightness = null)
    {
        var brSet = brightness ?? (double)SettingStorage.GetValue(SettingKeys.BackgroundBrightness);
        var br = (byte)((ThemeManager.IsDarkTheme ? 1 - brSet : brSet) / 100.0 * 255.0);
        var color = new Color() { A = 255, B = br, G = br, R = br };
        this.Background = new AcrylicBrush()
        {
            BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
            TintColor = color,
            FallbackColor = color,
            TintOpacity = 0.8
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
        public BookshelfBookViewModel BookshelfModel;
        public string Title;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
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
            //SetBookshelfModel(param.BookshelfModel);
            if (Binding != null)
                Binding.Title = param.Title;
        }
        else if (e.Parameter is Stream stream)
        {
            throw new NotImplementedException();
        }

        var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
        //currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
        currentView.BackRequested += CurrentView_BackRequested;

        //このディレイがないとタブコントロールが表示されてしまう。親がロードされれば問題ないはずなのでディレイ。
        //TrySetFullScreenMode()内のコメントアウト参照。そっちでの細かな経緯は忘れた。
        //Without this Delay, Tab control is not hidden. See TrySetFullScreenMode().
        await Task.Delay(500);
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

    public void CloseOperation()
    {
        Binding?.SaveInfo();
        Binding?.DisposeBasic();
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

        Binding?.DisposeBasic();

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
        Binding?.UpdateContainerInfo(file);
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
        if (file != null)
        {
            if (BookManager.GetBookTypeByPath(file.Path) == BookManager.BookType.Epub)
            {
                var tab = UIHelper.GetCurrentTabPage(this);
                if (tab is null) return;
                var (frame, newTab) = tab.OpenTab("BookViewer");
                UIHelper.FrameOperation.OpenEpubPreferedEngine(frame, file, newTab);
            }
            else
            {
                Binding?.UpdateContainerInfo(file);
                Binding?.Initialize(file, this.flipView);
            }
        }
    }

    public static void AddItemFromToc(IEnumerable<TocEntryViewModes> items, MenuFlyoutSubItem menuToAdd, BookViewModel vm)
    {
        //Toc in ContextMenu is strange. Isn't it?
        foreach (var item in items)
        {
            if (item.Children.Count > 0)
            {
                var menu = new MenuFlyoutSubItem() { Text = item.Title };
                AddItemFromToc(item.Children, menu, vm);
                menuToAdd.Items.Add(menu);
            }
            else
            {
                var menu = new MenuFlyoutItem() { Text = item.Title };
                menu.Click += (_, _) =>
                {
                    //In Pdf you have to add 1. See
                    //BookFixed3ViewerControllerControl.TreeView_ItemInvoked(_,_)
                    vm.PageSelectedDisplay = item.Page + 1;
                };
                menuToAdd.Items.Add(menu);
            }
        }
    }

    private void flipView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var option = new FlyoutShowOptions();
        if (args.TryGetPosition(this, out Point p)) option.Position = p;
        if (sender is not FrameworkElement fe || fe.Resources["ContextFlyout"] is not MenuFlyout menuFlyout) return;
        if (Binding is null) return;
        {
            if (menuFlyout.Items.FirstOrDefault(a => a.Tag?.ToString() == "Bookmark") is MenuFlyoutSubItem menuBookmark)
            {
                //Changes to MenuFlyoutSubItem.Items don't update UI #1652
                //https://github.com/microsoft/microsoft-ui-xaml/issues/1652
                //workaround start here
                var index = menuFlyout.Items.IndexOf(menuBookmark);
                menuBookmark = new MenuFlyoutSubItem() { Text = menuBookmark.Text, FlowDirection = menuBookmark.FlowDirection, Icon = new SymbolIcon(Symbol.Bookmarks), Tag = menuBookmark.Tag };
                menuFlyout.Items[index] = menuBookmark;
                //end here

                menuBookmark.Items.Clear();
                foreach (var item in Binding.Bookmarks)
                {
                    if (item.AutoGenerated && (item.Page == 1 || item.Page == Binding.PagesCount)) continue;
                    var menu = new MenuFlyoutItem()
                    {
                        Text = item.Title,
                    };
                    menu.Click += (_, _) => Binding.PageSelectedDisplay = item.Page;
                    menuBookmark.Items.Add(menu);
                }

                {
                    if (menuBookmark.Items.Count > 0) menuBookmark.Items.Add(new MenuFlyoutSeparator());
                    if (string.IsNullOrEmpty(Binding.CurrentBookmarkName))
                    {
                        var menu = new MenuFlyoutItem()
                        {
                            Text = ResourceManager.Loader.GetString("Bookmark/Add/Title")
                        };
                        menu.Click += (_, _) => Binding.CurrentBookmarkName = ResourceManager.Loader.GetString("Bookmark/New/Title");
                        menuBookmark.Items.Add(menu);
                    }
                    else
                    {
                        var menu = new MenuFlyoutItem()
                        {
                            Text = ResourceManager.Loader.GetString("Bookmark/Delete/Title")
                        };
                        menu.Click += (_, _) => Binding.CurrentBookmarkName = string.Empty;
                        menuBookmark.Items.Add(menu);
                    }
                }
            }
        }
        {
            if (menuFlyout.Items.FirstOrDefault(a => a.Tag?.ToString() == "Toc") is MenuFlyoutSubItem menuToc)
            {
                //Changes to MenuFlyoutSubItem.Items don't update UI #1652
                //https://github.com/microsoft/microsoft-ui-xaml/issues/1652
                //workaround start here
                var index = menuFlyout.Items.IndexOf(menuToc);
                menuToc = new MenuFlyoutSubItem() { Text = menuToc.Text, FlowDirection = menuToc.FlowDirection, Tag = menuToc.Tag };
                menuFlyout.Items[index] = menuToc;
                //end here

                if (Binding.Toc.Count > 0)
                {
                    menuToc.IsEnabled = true;
                    AddItemFromToc(Binding.Toc, menuToc, Binding);
                }
                else
                {
                    menuToc.IsEnabled = false;
                }
            }
        }
        {
            if (menuFlyout.Items.FirstOrDefault(a => a.Tag?.ToString() == "OpenWith") is MenuFlyoutSubItem menuOpenWith)
            {
                //Changes to MenuFlyoutSubItem.Items don't update UI #1652
                //https://github.com/microsoft/microsoft-ui-xaml/issues/1652
                //workaround start here
                var index = menuFlyout.Items.IndexOf(menuOpenWith);
                menuOpenWith = new MenuFlyoutSubItem() { Text = menuOpenWith.Text, FlowDirection = menuOpenWith.FlowDirection, Tag = menuOpenWith.Tag };
                menuFlyout.Items[index] = menuOpenWith;
                //end here

                menuOpenWith.Items.Clear();
                menuOpenWith.IsEnabled = false;
                if (Binding.FileItem is kurema.FileExplorerControl.Models.FileItems.StorageFileItem sfi && sfi.Content is Windows.Storage.IStorageFile isf)
                {
                    menuOpenWith.IsEnabled = true;

                    if (Path.GetExtension(Binding.FileItem?.Name).ToUpperInvariant() == ".PDF")
                    {
                        if (kurema.BrowserControl.Helper.Functions.IsWebView2Supported)
                        {
                            var tab = UIHelper.GetCurrentTabPage(this);
                            {
                                var menu = new MenuFlyoutItem()
                                {
                                    Text = ResourceManager.Loader.GetString("TabHeader/PdfJs"),
                                };
                                menu.Click += (_, _) =>
                                {
                                    tab.OpenTabPdfJs(isf);
                                };
                                menuOpenWith.Items.Add(menu);
                            }
                            {
                                var menu = new MenuFlyoutItem()
                                {
                                    Text = ResourceManager.Loader.GetString("TabHeader/Browser"),
                                };
                                menu.Click += (_, _) =>
                                {
                                    tab.OpenTabBrowser(isf);
                                };
                                menuOpenWith.Items.Add(menu);
                            }
                            menuOpenWith.Items.Add(new MenuFlyoutSeparator());
                        }
                    }

                    {
                        var menu = new MenuFlyoutItem()
                        {
                            Text = ResourceManager.LoaderFileExplorer.GetString("ContextMenu/OpenWith/Choose")
                        };
                        menu.Click += async (_, _) =>
                        {
                            await Windows.System.Launcher.LaunchFileAsync(isf, new Windows.System.LauncherOptions() { DisplayApplicationPicker = true });
                        };
                        menuOpenWith.Items.Add(menu);
                    }
                }
            }
        }

        menuFlyout.ShowAt(this, option);
        args.Handled = true;
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        //See zoomButton in BookFixed3ViewerControllerControl.xaml
        //Flyoutが一度開くまでKeyboardAcceleratorが機能しないので、無理やりこっちでやった。
        //本来はXAML側でやるべき。
        switch (e.Key)
        {
            case Windows.System.VirtualKey.W:
            case Windows.System.VirtualKey.NumberPad8:
            case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
                Binding?.PageSelectedViewModel?.MoveCommand?.Execute("0,0.1");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.A:
            case Windows.System.VirtualKey.NumberPad4:
            case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
                Binding?.PageSelectedViewModel?.MoveCommand?.Execute("-0.1,0");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.S:
            case Windows.System.VirtualKey.NumberPad2:
            case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
                Binding?.PageSelectedViewModel?.MoveCommand?.Execute("0,-0.1");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.D:
            case Windows.System.VirtualKey.NumberPad6:
            case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
                Binding?.PageSelectedViewModel?.MoveCommand?.Execute("0.1,0");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Subtract:
            case Windows.System.VirtualKey.Q:
                Binding?.PageSelectedViewModel?.ZoomFactorMultiplyCommand?.Execute("0.83");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Add:
            case Windows.System.VirtualKey.E:
            case Windows.System.VirtualKey.GamepadRightShoulder:
                Binding?.PageSelectedViewModel?.ZoomFactorMultiplyCommand?.Execute("1.2");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.NumberPad5:
            case Windows.System.VirtualKey.Z:
            case Windows.System.VirtualKey.GamepadRightTrigger:
                if (Binding?.PageSelectedViewModel?.ZoomFactor != null) Binding.PageSelectedViewModel.ZoomFactor = 1.0f;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.GamepadDPadLeft:
                if (Binding?.PageVisualAddCommand?.CanExecute("-1") == true) Binding?.PageVisualAddCommand?.Execute("-1");
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.GamepadDPadRight:
                if (Binding?.PageVisualAddCommand?.CanExecute("1") == true) Binding?.PageVisualAddCommand?.Execute("1");
                e.Handled = true;
                break;
        }

    }

    private async void MenuFlyoutItem_Click_OpenSettingDialog(object sender, RoutedEventArgs e)
    {
        var panel = new SettingPanelControl();
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Content = new ScrollViewer()
            {
                Content = panel
            },
        };

        var src = new List<SettingPage.SettingViewModel>(SettingStorage.SettingInstances.Length);
        foreach (var item in SettingStorage.SettingInstances.Where(a => a.IsVisible && a.GroupName == "Viewer"))
        {
            src.Add(new SettingPage.SettingViewModel(item));
        }
        panel.SettingSource.Source = src.GroupBy(a => a.Group);
        dialog.IsPrimaryButtonEnabled = true;
        dialog.PrimaryButtonText = ResourceManager.Loader.GetString("Word/OK");
        Binding.SaveInfo();

        dialog.Closed += async (s, e) =>
        {
            if (Binding is null) return;
            var page = Binding.PageSelectedDisplay;
            this.Binding.UpdateSettings();
            await this.Binding.UpdatePages(this.Dispatcher);
            Binding.PageSelectedDisplay = page;
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch
        {
        }
    }

    private void flipView_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = false;
        if (sender is not FlipViewEx flip) return;
        if (e.Pointer?.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Mouse && Binding?.PageSelectedViewModel?.ZoomFactor == 1.0)
        {
            if (e.Pointer.IsInContact)
            {
                const double extraFlipLength = 0.3;

                var point = e.GetCurrentPoint(flip);
                if (point.Properties.IsLeftButtonPressed)
                {
                    if (_LastPoint is null) _LastPoint = point;
                    var offset = flip.HorizontalOffset + ((float)(point.Position.X - _LastPoint.Position.X));
                    if (flip.SelectedIndex == 0) offset = Math.Min(offset, (float)(flip.ActualWidth * extraFlipLength));
                    if (flip.SelectedIndex == flip.Items.Count - 1) offset = Math.Max(offset, -(float)(flip.ActualWidth * extraFlipLength));
                    flip.HorizontalOffset = offset;
                    _LastPoint = point;
                    e.Handled = true;
                }
                else
                {
                }
            }
        }
    }

    Windows.UI.Input.PointerPoint _LastPoint = null;

    private void flipView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = false;
        if (sender is not FlipViewEx flip) return;
        if (e.Pointer?.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Mouse && Binding?.PageSelectedViewModel?.ZoomFactor == 1.0)
        {
            if (e.Pointer.IsInContact)
            {
                flip.CapturePointer(e.Pointer);
                var point = e.GetCurrentPoint(flip);
                if (point.Properties.IsLeftButtonPressed)
                {
                    _LastPoint = point;
                    e.Handled = true;
                }
                else if (point.Properties.IsRightButtonPressed)
                {
                    //ViewerController.SetControlPanelVisibility(true);
                }
            }
        }
    }

    private async void PlayPageAnimation(FlipViewEx flip)
    {
        //var doubleAnime = new DoubleAnimationUsingKeyFrames();
        //Storyboard.SetTarget(doubleAnime, flipView);
        //Storyboard.SetTargetProperty(doubleAnime, nameof(flipView.HorizontalOffset));
        //doubleAnime.KeyFrames.Add(new EasingDoubleKeyFrame()
        //{
        //    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0)),
        //    Value = flipView.HorizontalOffset
        //});
        //doubleAnime.KeyFrames.Add(new EasingDoubleKeyFrame()
        //{
        //    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)),
        //    Value = 0
        //});
        //Storyboard storyboard = new();
        //TimelineDelegate timeline = new TimelineDelegate()
        //{
        //    Duration=new Duration(TimeSpan.FromSeconds(0.3)),
        //}
        //storyboard.Children.Add(doubleAnime);
        //storyboard.Begin();

        await SemaphorePageAnimation.WaitAsync();
        try
        {
            var anime = flip.UseTouchAnimationsForAllNavigation;
            flip.UseTouchAnimationsForAllNavigation = false;
            //flip.SelectedIndex = Math.Max(0, Math.Min(flip.Items.Count - 1, (flip.SelectedIndex - GetSnapResult(flip.HorizontalOffset / flip.ActualWidth, 0.3))));
            var result = Math.Max(0, Math.Min(flip.Items.Count - 1, flip.SelectedIndex - GetSnapResult(flip.HorizontalOffset / flip.ActualWidth, ScrollDetectionWidthRelative)));
            var target = -(result - flip.SelectedIndex) * flip.ActualWidth;
            var diff = target - flip.HorizontalOffset;
            while (true)
            {
                //This is rough implementation of animation. But that's fine.
                if (diff == 0) break;
                var offset = flip.HorizontalOffset + Math.Sign(diff) * (float)flip.ActualWidth * 0.08f;
                if (diff > 0 ? offset > target : target > offset) break;
                flip.HorizontalOffset = offset;
                await Task.Delay(16);
            }
            flip.SelectedIndex = result;
            flip.HorizontalOffset = 0;
            flip.UseTouchAnimationsForAllNavigation = anime;
        }
        finally
        {
            SemaphorePageAnimation.Release();
        }
    }

    public const double ScrollDetectionWidthRelative = 0.25;

    private System.Threading.SemaphoreSlim SemaphorePageAnimation = new(1, 1);

    private static int GetSnapResult(double diff, double minimumX)
    {
        var abs = Math.Abs(diff);
        var floor = Math.Floor(abs);
        if (abs - floor > minimumX) floor++;
        return (int)floor * Math.Sign(diff);
    }

    private void flipView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = false;
        if (sender is not FlipViewEx flip) return;
        if (e.Pointer?.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Mouse && Binding?.PageSelectedViewModel?.ZoomFactor == 1.0)
        {
            _LastPoint = null;
            PlayPageAnimation(flip);
            flip.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void flipView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = false;
        if (sender is not FlipViewEx flip) return;
        if (e.Pointer?.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Mouse && Binding?.PageSelectedViewModel?.ZoomFactor == 1.0)
        {
            _LastPoint = null;
            PlayPageAnimation(flip);
            flip.ReleasePointerCapture(e.Pointer);
            try
            {
                flip.Focus(FocusState.Programmatic);
            }
            catch { }
            e.Handled = true;
        }
    }
}
