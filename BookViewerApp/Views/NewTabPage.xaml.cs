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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NewTabPage : Page
    {
        public NewTabPage()
        {
            this.InitializeComponent();
        }

        private void Button_Click_GoToSetting(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }

        private void Button_Click_GoToBrowser(object sender, RoutedEventArgs e)
        {
            string homepage = "https://www.google.com/";
            if (SettingStorage.GetValue("WebHomePage") is string h)
            {
                homepage = h;
            }

            if ((this.XamlRoot?.Content as Frame).Content is TabPage tab)
            {
                var item = (this.Parent as Frame)?.Parent as Microsoft.UI.Xaml.Controls.TabViewItem;
                UIHelper.OpenBrowser(this.Frame, homepage, (a) => { tab.OpenTabWeb(a); }, (b) => { tab.OpenTabBook(b); }, (c) =>
                {
                    {
                        if (item != null) item.Header = c;
                    }
                });
            }
            else
            {
                //ToDo: This will not be used. But set correct Action.
                UIHelper.OpenBrowser(this.Frame, homepage, (a) => { }, (b) => { }, (c) => { });
            }
        }

        private async void Button_Click_PickBook(object sender, RoutedEventArgs e)
        {
            var file = await Books.BookManager.PickFile();
            if (file != null)
            {
                this.Frame.Navigate(typeof(BookFixed3Viewer), file);
            }
        }

        private async void Button_Click_PickBookEpub(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".epub");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            var resolver = new EpubResolver();
            await resolver.LoadAsync((await file.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream());
            this.Frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserPage), null);
            if (this.Frame.Content is kurema.BrowserControl.Views.BrowserPage content)
            {

                Uri uri = content.Control.Control.BuildLocalStreamUri("epub", "/reader/index.html");
                //Uri uri = content.Control.Control.BuildLocalStreamUri("epub", "/contents/META-INF/container.xml");
                content.Control.Control.NavigateToLocalStreamUri(uri, resolver);
            }
        }
    }
}
