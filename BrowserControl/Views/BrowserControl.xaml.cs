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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.BrowserControl.Views
{
    public sealed partial class BrowserControl : UserControl, IDisposable
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
    }
}
