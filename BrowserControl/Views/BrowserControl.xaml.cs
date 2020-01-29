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
    public sealed partial class BrowserControl : UserControl
    {
        public WebView Control => this.webView;

        public BrowserControl()
        {
            this.InitializeComponent();

            webView.Navigate(new Uri("http://www.google.co.jp/"));
            if(this.DataContext is ViewModels.BrowserControlViewModel vm)
            {
                vm.Content = this.webView;
            }
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

        public event TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> NewWindowRequested;

        private void webView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            NewWindowRequested?.Invoke(sender, args);
        }
    }
}
