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
    public sealed partial class SimpleListViewControl : UserControl
    {
        public SimpleListViewControl()
        {
            this.InitializeComponent();
        }

        public object Source
        {
            get => itemsSource?.Source;
            set => itemsSource.Source = value;
        }

        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var command = ((sender as FrameworkElement)?.DataContext as ViewModels.ListItemViewModel)?.OpenCommand;
            if (command?.CanExecute(sender) == true) command.Execute(sender);
        }
    }
}
