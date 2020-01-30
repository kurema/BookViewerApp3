﻿using System;
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


// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
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

            //var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            //coreTitleBar.ExtendViewIntoTitleBar = true;
            //coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            //var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            //titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            //titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            //Window.Current.SetTitleBar(CustomDragRegion);
        }

        //ToDo: Delete when tabView_TabItemsChanged no more need this.
        public AppWindow RootAppWindow = null;

        void SetupWindow(AppWindow window)
        {
            //https://github.com/microsoft/Xaml-Controls-Gallery/blob/f2d4568ec53464c3d290940282d2f70cfd62fa94/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
            if (window == null)
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

        public void OpenTabBook(IEnumerable< Windows.Storage.IStorageItem> files)
        {
            foreach (var item in files) OpenTabBook(item);
        }


        public void OpenTabBook(Windows.Storage.IStorageItem file)
        {
            var newTab = new winui.Controls.TabViewItem();
            newTab.Header = "Book";
            var frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(BookFixed3Viewer), file);
            tabView.TabItems.Add(newTab);
            tabView.SelectedIndex = tabView.TabItems.Count - 1;
        }

        public void OpenTabWeb(string uri)
        {
            var newTab = new winui.Controls.TabViewItem();
            newTab.Header = "Web";
            var frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserPage), uri);
            tabView.TabItems.Add(newTab);
            tabView.SelectedIndex = tabView.TabItems.Count - 1;

            if (frame.Content is kurema.BrowserControl.Views.BrowserPage content)
            {
                content.Control.NewWindowRequested += (s, e) =>
                {
                    OpenTabWeb(e.Uri.ToString());
                    e.Handled = true;
                };

                content.Control.Control.UnviewableContentIdentified += async (s, e) =>
                {
                    string extension = "";
                    foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
                    { 
                        if (Path.GetExtension(e.Uri.ToString()).ToLower()  == ext)
                        {
                            extension = ext;
                            goto success;
                        }
                    }
                    //return;
                success:;
                    try
                    {
                        var namebody = Path.GetFileNameWithoutExtension(e.Uri.ToString());
                        namebody = namebody.Length > 32 ? namebody.Substring(0, 32) : namebody;
                        var item = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                            Path.Combine("Download", namebody + extension)
                            , Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                        var downloader = new BackgroundDownloader();
                        var download = downloader.CreateDownload(e.Uri, item);

                        var vmb = content.Control.DataContext as kurema.BrowserControl.ViewModels.BrowserControlViewModel;
                        var dlitem = new kurema.BrowserControl.ViewModels.DownloadItemViewModel(item.Name);
                        vmb?.Downloads.Add(dlitem);

                        await download.StartAsync().AsTask(new Progress<DownloadOperation>((t) => {
                            try
                            {
                                //https://stackoverflow.com/questions/38939720/backgrounddownloader-progress-doesnt-update-in-uwp-c-sharp
                                if (t.Progress.TotalBytesToReceive>0) {
                                    double br = t.Progress.TotalBytesToReceive;
                                    dlitem.DownloadedRate = br / t.Progress.TotalBytesToReceive;
                                }
                            }
                            catch { }
                        }));

                        dlitem.DownloadedRate = 1.0;
                        dlitem.OpenCommand = new DelegateCommand((a) => OpenTabBook(item));

                        OpenTabBook(item);
                        BookViewerApp.App.Current.Suspending += async (s2, e2) =>
                        {
                            try
                            {
                                await item.DeleteAsync();
                            }
                            catch { }
                        };
                    }
                    catch { }
                    return;
                };
            }

            if ((frame.Content as kurema.BrowserControl.Views.BrowserPage)?.Control.DataContext is kurema.BrowserControl.ViewModels.BrowserControlViewModel vm && vm != null)
            {
                if (SettingStorage.GetValue("WebHomePage") is string homepage)
                {
                    vm.HomePage = homepage;
                }

                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(kurema.BrowserControl.ViewModels.BrowserControlViewModel.Title))
                    {
                        newTab.Header = vm.Title;
                    }
                };
            }

        }

        private async void TabView_AddTabButtonClick(Microsoft.UI.Xaml.Controls.TabView sender, object args)
        {
            var newTab = new winui.Controls.TabViewItem();
            //newTab.IconSource = new winui.Controls.SymbolIconSource() { Symbol = Symbol.Document };
            newTab.Header = "Book";

            // The Content of a TabViewItem is often a frame which hosts a page.
            Frame frame = new Frame();
            newTab.Content = frame;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            foreach (var ext in Books.BookManager.AvailableExtensionsArchive)
            {
                picker.FileTypeFilter.Add(ext);
            }
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    frame.Navigate(typeof(BookFixed3Viewer), file);
                    sender.TabItems.Add(newTab);
                    sender.SelectedIndex = sender.TabItems.Count - 1;
                }
                catch (Exception e)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "error",
                        Content = e.Message + "\n\n" + e.StackTrace + "\n\n" + e.InnerException + "\n\n" + e.Source,
                        CloseButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void TabView_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            CloseTab(args.Tab);
        }

        private void CloseTab(winui.Controls.TabViewItem tab)
        {
            if (tab == null) return;
            ((tab?.Content as Frame)?.Content as BookFixed3Viewer)?.SaveInfo();
            if (tab.IsClosable)
            {
                tabView.TabItems.Remove(tab);
            }

            //if (tabView.TabItems.Count == 0)
            //{
            //    CoreApplication.Exit();
            //}
        }

        #region keyAccelerator
        //二回発火するのと、WebView使ってると機能していないみたいなんでオフにしました。

        private DateTimeOffset LastKeyboardActionDateTime = new DateTimeOffset();
        private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var sec = DateTimeOffset.UtcNow - LastKeyboardActionDateTime;
            if ((DateTimeOffset.UtcNow - LastKeyboardActionDateTime).TotalSeconds > 0.2)
            {
                OpenTabWeb("https://www.google.com/");
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
                OpenTabWeb("https://www.google.com/");
            }
            else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
            {
                var args = (Windows.ApplicationModel.Activation.IActivatedEventArgs)e.Parameter;
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    foreach (var item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
                    {
                        var newTab = new winui.Controls.TabViewItem();
                        newTab.Header = "Book";
                        var frame = new Frame();
                        newTab.Content = frame;
                        frame.Navigate(typeof(BookFixed3Viewer), item);
                        tabView.TabItems.Add(newTab);
                        tabView.SelectedIndex = tabView.TabItems.Count - 1;
                    }
                }
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
            if (!Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
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

        private async void tabView_TabItemsChanged(winui.Controls.TabView sender, IVectorChangedEventArgs args)
        {
            if (sender.TabItems.Count == 0)
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
                        Window.Current.Close();
                    }
                    catch(System.InvalidOperationException e)
                    {
                    }
                }
            }
        }

        private void tabView_TabStripDrop(object sender, DragEventArgs e)
        {
            object obj;
            if (e.DataView.Properties.TryGetValue(DataIdentifier, out obj))
            {
                if (obj == null) return;
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

                    var destinationTabViewListView = ((obj as winui.Controls.TabViewItem).Parent as winui.Controls.Primitives.TabViewListView);
                    destinationTabViewListView?.Items.Remove(obj);

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
            if (this.tabView.TabItems.Count <= 1 && RootAppWindow == null)return;

            var firstItem = args.Tab;
            args.Data.Properties.Add(DataIdentifier, firstItem);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }
}
