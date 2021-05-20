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

namespace BookViewerApp.Views.Bookshelf
{
    public sealed partial class BookInfo : UserControl
    {
        public UIElement ShadowTarget => BookMain.ShadowTarget;


        public BookInfo()
        {
            this.InitializeComponent();

            this.DataContext = new ViewModels.Bookshelf2BookViewModel();

            SharedShadow.Receivers.Add(BackgroundGrid);

            BookMain.ShadowTarget.Shadow = SharedShadow;
            BookMain.Translation += new System.Numerics.Vector3(0, 0, 32);
        }
    }
}
