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

namespace BookViewerApp.Views
{
    public sealed partial class LibraryManagerControl : UserControl
    {
        public LibraryManagerControl()
        {
            this.InitializeComponent();
        }

        private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            e.Cancel = e.Items.Count == 0;
        }
    }
}
