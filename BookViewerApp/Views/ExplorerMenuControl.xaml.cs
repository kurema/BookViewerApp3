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
    public sealed partial class ExplorerMenuControl : UserControl
    {
        public ExplorerMenuControl()
        {
            this.InitializeComponent();

            listView.ItemsSource = new MenuItem[]
            {
                new MenuItem(Symbol.Folder,"Add Folder",()=>{ }),
                new MenuItem(Symbol.World,"Browser",()=>{ }),
                new MenuItem(Symbol.Setting,"Setting",()=>{ }),
            };
        }

        public class MenuItem
        {
            public MenuItem(Symbol icon, string title, Action action)
            {
                Icon = icon;
                Title = title ?? throw new ArgumentNullException(nameof(title));
                Action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public Symbol Icon { get; set; }
            public string Title { get; set; }
            public Action Action { get; set; } 
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(e.ClickedItem is MenuItem item)
            {
                item?.Action?.Invoke();
            }
        }
    }
}
