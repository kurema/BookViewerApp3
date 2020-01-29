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

namespace kurema.BrowserControl.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class BrowserPage : Page
    {
        public BrowserPage()
        {
            this.InitializeComponent();
        }

        public BrowserControl Control => this.control;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null) return;
            else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs arg)
            {
            }else if(e.Parameter is string s)
            {
                if (control.DataContext is ViewModels.BrowserControlViewModel vm && vm != null)
                {
                    vm.Uri = s;
                }
            }
        }
    }
}
