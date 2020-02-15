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

namespace kurema.FileExplorerControl.Views
{
    public sealed partial class FileExplorerControl : UserControl
    {
        public FileExplorerControl()
        {
            this.InitializeComponent();

            if(this.DataContext is ViewModels.FileExplorerViewModel fvm && this.content.DataContext is ViewModels.ContentViewModel vm)
            {
                fvm.Content = vm;

                fvm.Content.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(ViewModels.ContentViewModel.Item))
                    {
                        address.SetAddress(fvm.Content.Item);
                    }
                };
            }
        }

        public void SetTreeViewItem(params ViewModels.FileItemViewModel[] fileItemVMs)
        {
            foreach (var item in fileItemVMs)
            {
                treeview.RootNodes.Add(new TreeViewNode()
                {
                    IsExpanded = false,
                    Content = item,
                    HasUnrealizedChildren = item.IsFolder,
                });
            }
        }

        private async void TreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (!args.Node.HasUnrealizedChildren) return;

            sender.IsEnabled = false;

            try
            {
                if (args.Item is ViewModels.FileItemViewModel vm)
                {
                    if (vm.Children == null) await vm.UpdateChildren();
                    args.Node.Children.Clear();
                    foreach (var item in vm.Folders)
                    {
                        args.Node.Children.Add(new TreeViewNode()
                        {
                            IsExpanded = false,
                            Content = item,
                            HasUnrealizedChildren = item.IsFolder,
                        });
                    }
                }
            }
            finally
            {
                args.Node.HasUnrealizedChildren = false;
                sender.IsEnabled = true;
            }
        }

        private async void treeview_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is TreeViewNode tvn && tvn.Content is ViewModels.FileItemViewModel vm)
            {
                await content.SetFolder(vm);
                if (tvn.IsExpanded == false) tvn.IsExpanded = true;
            }
        }
    }
}
