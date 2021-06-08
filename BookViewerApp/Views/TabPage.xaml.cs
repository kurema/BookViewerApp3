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

using winui = Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

using Windows.Networking.BackgroundTransfer;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;

using BookViewerApp.Helper;
using BookViewerApp.Storages;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class TabPage : Page
    {
        public winui.Controls.TabView Control => this.tabView;

        private const string DataIdentifier = "MainTabItem";

        //https://docs.microsoft.com/ja-jp/windows/uwp/design/controls-and-patterns/tab-view
        public TabPage()
        {
            this.InitializeComponent();
        }

        //ToDo: Delete when tabView_TabItemsChanged no more need this.
        public AppWindow RootAppWindow = null;

        void SetupWindow(AppWindow window)
        {
            //https://github.com/microsoft/Xaml-Controls-Gallery/blob/f2d4568ec53464c3d290940282d2f70cfd62fa94/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
            if (window is null)
            {
                // Extend into the titlebar
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                Window.Current.SetTitleBar(CustomDragRegion);
            }
            else
            {
                // Secondary AppWindows --- keep track of the window
                RootAppWindow = window;

                // Extend into the titlebar
                window.TitleBar.ExtendsContentIntoTitleBar = true;
                window.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                window.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                // Due to a bug in AppWindow, we cannot follow the same pattern as CoreWindow when setting the min width.
                // Instead, set a hardcoded number. 
                CustomDragRegion.MinWidth = 188;

                window.Frame.DragRegionVisuals.Add(CustomDragRegion);
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        }

        public async void OpenTabBookPicked()
        {
            await UIHelper.FrameOperation.OpenBookPicked(() => OpenTab("BookViewer"));
        }

        public void OpenTabBook(IEnumerable<Windows.Storage.IStorageItem> files)
        {
            if (files is null) return;
            foreach (var item in files) OpenTabBook(item);
        }


        public void OpenTabBook(Windows.Storage.IStorageItem file)
        {
            if (file is Windows.Storage.IStorageFile item)
            {
                UIHelper.FrameOperation.OpenBook(item, () => OpenTab("BookViewer"), () =>
                 {
                    //Dialog:Open dangerous file?
                    //Open.
                    //Or just ignore.
                });
            }
        }

        public void OpenTabBook(Stream stream)
        {
            var (frame, newTab) = OpenTab("BookViewer");
            frame.Navigate(typeof(BookFixed3Viewer), stream);
        }

        public void OpenTabExplorer()
        {
            var (frame, newTab) = OpenTab("Explorer");
            UIHelper.FrameOperation.OpenExplorer(frame, newTab);
        }


        public async void OpenTabBook(System.Threading.Tasks.Task<Stream> stream)
        {
            OpenTabBook(await stream);
        }


        public void OpenTabWeb()
        {
            OpenTabWeb(SettingStorage.GetValue("WebHomePage") as string ?? "https://www.google.com/");
        }


        public void OpenTabWeb(string uri)
        {
            var (frame, newTab) = OpenTab("Browser");
            UIHelper.FrameOperation.OpenBrowser(frame, uri, (a) => OpenTabWeb(a), (a) => OpenTabBook(a), (title) =>
            {
                newTab.Header = title;
            }
            );
        }

        public async System.Threading.Tasks.Task OpenTabWebPreferedBrowser(string uri)
        {
            if ((bool)SettingStorage.GetValue("DefaultBrowserExternal"))
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var uri1))
                {
                    await Windows.System.Launcher.LaunchUriAsync(uri1);
                }
            }
            else
            {
                OpenTabWeb(uri);
            }
        }

        public void OpenTabSetting()
        {
            var (frame, newTab) = OpenTab("Setting");
            frame?.Navigate(typeof(SettingPage));
        }

        public void OpenTabBookshelf()
        {
            var (frame, newTab) = OpenTab("Bookshelf");
            frame?.Navigate(typeof(Bookshelf.BookshelfPage));
        }


        public (Frame, winui.Controls.TabViewItem) OpenTab(string titleId)
        {
            var newTab = new winui.Controls.TabViewItem();
            var titleString = UIHelper.GetTitleByResource(titleId);
            newTab.Header = string.IsNullOrWhiteSpace(titleString) ? "New Tab" : titleString;

            Frame frame = new Frame();
            newTab.Content = frame;

            tabView.TabItems.Add(newTab);
            tabView.SelectedItem = newTab;

            frame.Focus(FocusState.Programmatic);

            return (frame, newTab);
        }

        private void TabView_AddTabButtonClick(winui.Controls.TabView sender, object args)
        {
            OpenTabExplorer();
        }

        private void TabView_TabCloseRequested(winui.Controls.TabView sender, winui.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            if (RootAppWindow == null && tabView.TabItems.Count == 1)
            {
                OpenTabExplorer();
                CloseTab(args.Tab);
                tabView.SelectedIndex = 0;
            }
            else
            {
                CloseTab(args.Tab);
            }
        }

        public async void CloseTab(winui.Controls.TabViewItem tab)
        {
            if (tab is null) return;
            ((tab?.Content as Frame)?.Content as BookFixed3Viewer)?.CloseOperation();
            ((tab?.Content as Frame)?.Content as IDisposable)?.Dispose();
            ((tab?.Content as Frame)?.Content as IDisposableBasic)?.DisposeBasic();

            if (tab.IsClosable)
            {
                tabView.TabItems.Remove(tab);
            }

            if (tabView.TabItems.Count == 0)
            {
                //https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
                //This is far from smartness. But this is in microsoft repo.
                //There should be a way to detect this is mainwindow or not, but I dont know.
                if (RootAppWindow != null)
                {
                    await RootAppWindow.CloseAsync();
                }
                else
                {
                    try
                    {
                        //I need to close only main window. But error occurs and I have no idea how to fix it...
                        //Closing main window is not allowed.
                        //There is a question but no answer.
                        //https://stackoverflow.com/questions/39944258/closing-main-window-is-not-allowed

                        //Application.Current.Exit();
                        //Window.Current.Close();
                        //OpenTabWeb("https://www.google.com/");
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

        }

        #region keyAccelerator
        //二回発火するのと、WebView使ってると機能していないみたいなんでオフにしました。

        private DateTimeOffset LastKeyboardActionDateTime = new DateTimeOffset();
        private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var sec = DateTimeOffset.UtcNow - LastKeyboardActionDateTime;
            if ((DateTimeOffset.UtcNow - LastKeyboardActionDateTime).TotalSeconds > 0.2)
            {
                OpenTabWeb();
                LastKeyboardActionDateTime = DateTimeOffset.UtcNow;
            }
        }

        private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if ((DateTimeOffset.UtcNow - LastKeyboardActionDateTime).TotalSeconds > 0.2)
            {
                CloseTab(((winui.Controls.TabViewItem)tabView?.SelectedItem));
                LastKeyboardActionDateTime = DateTimeOffset.UtcNow;
            }
        }

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            int tabToSelect = 0;

            switch (sender.Key)
            {
                case Windows.System.VirtualKey.Number1:
                    tabToSelect = 0;
                    break;
                case Windows.System.VirtualKey.Number2:
                    tabToSelect = 1;
                    break;
                case Windows.System.VirtualKey.Number3:
                    tabToSelect = 2;
                    break;
                case Windows.System.VirtualKey.Number4:
                    tabToSelect = 3;
                    break;
                case Windows.System.VirtualKey.Number5:
                    tabToSelect = 4;
                    break;
                case Windows.System.VirtualKey.Number6:
                    tabToSelect = 5;
                    break;
                case Windows.System.VirtualKey.Number7:
                    tabToSelect = 6;
                    break;
                case Windows.System.VirtualKey.Number8:
                    tabToSelect = 7;
                    break;
                case Windows.System.VirtualKey.Number9:
                    // Select the last tab
                    tabToSelect = tabView.TabItems.Count - 1;
                    break;
            }

            // Only select the tab if it is in the list
            if (tabToSelect < tabView.TabItems.Count)
            {
                tabView.SelectedIndex = tabToSelect;
            }
        }
        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null || (e.Parameter is string s && s == ""))
            {
                OpenTabExplorer();
            }
            else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs args)
            {
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    foreach (var item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
                    {
                        OpenTabBook(item);
                    }
                }
            }
            else if (e.Parameter is Windows.Storage.IStorageItem f)
            {
                OpenTabBook(f);
            }

            SetupWindow(null);

        }

        public void AddItemToTabs(winui.Controls.TabViewItem tab)
        {
            tabView.TabItems.Add(tab);
            tabView.SelectedItem = tab;
        }

        private async void tabView_TabDroppedOutside(winui.Controls.TabView sender, winui.Controls.TabViewTabDroppedOutsideEventArgs e)
        {
            //{
            //    CloseTab(e.Tab);

            //    var newView = CoreApplication.CreateNewView();
            //    await newView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //    {
            //        var frame = new Frame();
            //        frame.Navigate(typeof(TabPage), null);

            //        (frame.Content as TabPage)?.AddItemToTabs(e.Tab);

            //        Window.Current.Content = frame;
            //        Window.Current.Activate();
            //    });

            //    return;
            //}

            {
                if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    return;
                }
                if (sender.TabItems.Count <= 1) return;

                var newWindow = await AppWindow.TryCreateAsync();
                var newPage = new TabPage();
                newPage.SetupWindow(newWindow);
                CloseTab(e.Tab);
                newPage.AddItemToTabs(e.Tab);
                Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetAppWindowContent(newWindow, newPage);

                // Show the window
                await newWindow.TryShowAsync();
            }
        }

        private void tabView_TabItemsChanged(winui.Controls.TabView sender, IVectorChangedEventArgs args)
        {
            ////This is buggy when you exit FullScreen.
            //if (sender.TabItems.Count == 0)
            //{
            //    //https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
            //    //This is far from smartness. But this is in microsoft repo.
            //    //There should be a way to detect this is mainwindow or not, but I dont know.
            //    if (RootAppWindow != null)
            //    {
            //        await RootAppWindow.CloseAsync();
            //    }
            //    else
            //    {
            //        try
            //        {
            //            Application.Current.Exit();
            //            //Window.Current.Close();
            //            //OpenTabWeb("https://www.google.com/");
            //        }
            //        catch (System.InvalidOperationException e)
            //        {
            //        }
            //    }
            //}
        }

        private void tabView_TabStripDrop(object sender, DragEventArgs e)
        {
            object obj;
            if (e.DataView.Properties.TryGetValue(DataIdentifier, out obj))
            {
                if (obj is null) return;
                var destinationTabView = sender as winui.Controls.TabView;
                if (destinationTabView.TabItems != null)
                {
                    var index = -1;
                    for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                    {
                        var item = destinationTabView.ContainerFromIndex(i) as winui.Controls.TabViewItem;

                        if (e.GetPosition(item).X - item.ActualWidth < 0)
                        {
                            index = i;
                            break;
                        }

                    }

                    {
                        //var destinationTabViewListView = ((obj as winui.Controls.TabViewItem).Parent as winui.Controls.Primitives.TabViewListView);
                        //destinationTabViewListView?.Items.Remove(obj);
                        if ((obj as winui.Controls.TabViewItem).XamlRoot.Content is TabPage tabPage) tabPage.CloseTab(obj as winui.Controls.TabViewItem);
                    }

                    if (index < 0)
                    {
                        destinationTabView.TabItems.Add(obj);
                    }
                    else if (index < destinationTabView.TabItems.Count)
                    {
                        destinationTabView.TabItems.Insert(index, obj);
                    }

                    // Select the newly dragged tab
                    destinationTabView.SelectedItem = obj;
                }
            }
        }

        private void tabView_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(DataIdentifier))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }

        private void tabView_TabDragStarting(winui.Controls.TabView sender, winui.Controls.TabViewTabDragStartingEventArgs args)
        {
            //Closing main window causes a problem. So disable it.
            if (this.tabView.TabItems.Count <= 1 && RootAppWindow is null) return;

            var firstItem = args.Tab;
            args.Data.Properties.Add(DataIdentifier, firstItem);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag != null)
            {
                switch (elem.Tag.ToString())
                {
                    case "Explorer": this.OpenTabExplorer(); break;
                    case "Browser": this.OpenTabWeb(); break;
                    case "Setting": this.OpenTabSetting(); break;
                    case "Picker": this.OpenTabBookPicked(); break;
                    case "Bookshelf": this.OpenTabBookshelf(); break;
                }
            }
        }

        //private void AddButtonMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    MenuFlyoutItem GetMenu(string title,Action action)
        //    {
        //        var menuItem = new MenuFlyoutItem();
        //        menuItem.Text = title;
        //        menuItem.Click += (s2, e2) => action?.Invoke();
        //        return menuItem;
        //    }

        //    if(sender is Button button)
        //    {
        //        var menu = new MenuFlyout() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
        //        menu.Items.Add(GetMenu("Explorer", () => OpenTabExplorer()));
        //        button.Flyout = menu;
        //    }
        //}
    }
}
