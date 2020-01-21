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


// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class TabPage : Page
    {
        //https://docs.microsoft.com/ja-jp/windows/uwp/design/controls-and-patterns/tab-view
        public TabPage()
        {
            this.InitializeComponent();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            Window.Current.SetTitleBar(CustomDragRegion);
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
                catch(Exception e)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "error",
                        Content = e.Message+"\n\n"+e.StackTrace+"\n\n"+e.InnerException+"\n\n"+e.Source,
                        CloseButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void TabView_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            ((args.Tab?.Content as Frame)?.Content as BookFixed3Viewer)?.SaveInfo();
            sender.TabItems.Remove(args.Tab);

            if (sender.TabItems.Count == 0)
            {
                CoreApplication.Exit();
            }
        }
    }
}
