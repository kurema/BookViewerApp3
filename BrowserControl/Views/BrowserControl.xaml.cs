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

using Windows.Networking.BackgroundTransfer;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.BrowserControl.Views
{
    public sealed partial class BrowserControl : Page, IDisposable
    {
        public WebView Control => this.webView;

        public BrowserControl()
        {
            this.InitializeComponent();

            //webView.Navigate(new Uri("http://www.google.co.jp/"));
            if (this.DataContext is ViewModels.BrowserControlViewModel vm)
            {
                vm.Content = this.webView;
            }

            webView.NavigationFailed += WebView_NavigationFailed;
        }

        private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            HideErrorStoryboard.Begin();
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                webView.Focus(FocusState.Programmatic);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        private async void Button_Click_OpenBrowser(object sender, RoutedEventArgs e)
        {
            if (webView.Source != null) await Windows.System.Launcher.LaunchUriAsync(webView.Source);
        }

        private void webView_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();

            if (sender?.ContainsFullScreenElement == true && !v.IsFullScreenMode)
            {
                if (v.TryEnterFullScreenMode())
                {
                }
            }
            else if (sender?.ContainsFullScreenElement == false && v.IsFullScreenMode)
            {
                v.ExitFullScreenMode();
            }
        }

        public void Dispose()
        {
            //To stop audio in background.
            //There should be the better way.
            webView.NavigateToString("");
        }

        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var command = ((sender as FrameworkElement)?.DataContext as ViewModels.BrowserControlViewModel)?.OpenDownloadDirectoryCommand;
            if (command?.CanExecute(null) == true) command.Execute(null);
        }

        public Action<WebView, WebViewUnviewableContentIdentifiedEventArgs, ViewModels.BrowserControlViewModel> UnviewableContentIdentifiedOverride { get; set; }

        private async void webView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            var dataContext = DataContext as ViewModels.BrowserControlViewModel;
            if (dataContext == null) return;

            if (UnviewableContentIdentifiedOverride != null)
            {
                UnviewableContentIdentifiedOverride.Invoke(sender, args, dataContext);
                return;
            }

            try
            {
                var namebody = Path.GetFileNameWithoutExtension(args.Uri.AbsoluteUri);
                namebody = namebody.Length > 32 ? namebody.Substring(0, 32) : namebody;
                var extension = Path.GetExtension(args.Uri.AbsoluteUri);
                extension = string.IsNullOrEmpty(extension) ? MimeTypes.MimeTypeMap.GetExtension(args.MediaType) : extension;

                var item = await (await dataContext.FolderProvider?.Invoke())?.CreateFileAsync(
                    namebody + extension
                    , Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                if (item == null) return;
                var downloader = new BackgroundDownloader();
                var download = downloader.CreateDownload(args.Uri, item);

                var dlitem = new ViewModels.DownloadItemViewModel(item);
                dataContext?.Downloads.Add(dlitem);

                await download.StartAsync().AsTask(new Progress<DownloadOperation>((t) =>
                {
                    try
                    {
                        //https://stackoverflow.com/questions/38939720/backgrounddownloader-progress-doesnt-update-in-uwp-c-sharp
                        if (t.Progress.TotalBytesToReceive > 0)
                        {
                            double br = t.Progress.TotalBytesToReceive;
                            dlitem.DownloadedRate = br / t.Progress.TotalBytesToReceive;
                        }
                    }
                    catch { }
                }));

                dlitem.DownloadedRate = 1.0;
                OpenFile(item);
            }
            catch { }
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is ViewModels.DownloadItemViewModel dlitem && dlitem.DownloadedRate == 1.0) OpenFile(dlitem.File);
        }

        private void OpenFile(Windows.Storage.StorageFile storageFile)
        {
            DownloadedFileOpenedEventHandler?.Invoke(this, storageFile);
        }

        public TypedEventHandler<BrowserControl, Windows.Storage.StorageFile> DownloadedFileOpenedEventHandler;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null) return;
            else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs arg)
            {
            }
            else if (e.Parameter is string s)
            {
                if (DataContext is ViewModels.BrowserControlViewModel vm && vm != null)
                {
                    vm.Uri = s;
                }
            }
        }
    }
}
