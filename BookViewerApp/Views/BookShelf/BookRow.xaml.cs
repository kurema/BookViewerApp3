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
    public sealed partial class BookRow : UserControl
    {
        public BookRow()
        {
            this.InitializeComponent();

            SharedShadow.Receivers.Add(BackgroundGrid);
        }

        public void UpdateShadow()
        {
            foreach(var target in BookRowMain.ShadowTargets)
            {
                target.Shadow = SharedShadow;
            }
        }
    }
}
