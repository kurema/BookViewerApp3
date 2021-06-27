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

        private async void ButtonAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return;
            
            if (DataContext is ViewModels.LibraryMemberViewModel vm && vm.Content!=null)
            {
                var items = vm.Content?.Items?.ToList() ?? new List<object>();
                items.Add(await Managers.BookManager.GetTokenFromPathOrRegister(folder));
                vm.Content.Items = items.ToArray();
                //vm.OnPropertyChanged(nameof(vm.Items));
            }
        }

        //private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        //{
        //    if(args.NewValue is ViewModels.LibraryMemberViewModel vm)
        //    {
        //        vm.Dispatcher = this.Dispatcher;
        //    }
        //}
    }
}
