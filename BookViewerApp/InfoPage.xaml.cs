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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class InfoPage : Page
    {
        public InfoPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            base.OnNavigatedFrom(e);
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Frame.CanGoBack ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed; currentView.BackRequested += CurrentView_BackRequested;
            currentView.BackRequested += CurrentView_BackRequested;
        }
    }
}
