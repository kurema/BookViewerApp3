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
    public sealed partial class PageBookFixedViewer : Page
    {
        public PageBookFixedViewer()
        {
            this.InitializeComponent();
        }

        public void Open(Windows.Storage.IStorageFile file)
        {
            BodyControl.Open(file);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter !=null && e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
            {
                var args = (Windows.ApplicationModel.Activation.IActivatedEventArgs)e.Parameter;
                if(args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    foreach(Windows.Storage.IStorageFile item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
                    {
                        Open(item);
                        break;
                    }
                }
            }
        }
    }
}
