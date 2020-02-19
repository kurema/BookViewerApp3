using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading.Tasks;
using System;
using System.Linq;



// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views
{
    public sealed partial class FileExplorerContentControl : UserControl
    {
        public FileExplorerContentControl()
        {
            this.InitializeComponent();
        }


        public async Task SetFolder(ViewModels.FileItemViewModel folder)
        {
            await OperateBinding(async (vm) =>
            {
                if (folder.Children == null) await folder.UpdateChildren();
                vm.Item = folder;
            });
        }

        public TypedEventHandler<FileExplorerContentControl, ViewModels.FileItemViewModel> FileOpenedEventHandler;

        private void UserControl_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
        {
            if (args.NewValue is ViewModels.ContentViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (s != this.DataContext) return;
                    if (e.PropertyName == nameof(vm.ContentStyle))
                    {
                        if (items.ItemTemplateSelector is FileExplorerContentDataSelector fc)
                        {
                            fc.ContentStyle = vm.ContentStyle;
                            if (vm?.RefreshCommand?.CanExecute(null) == true) vm.RefreshCommand.Execute(null);
                        }
                    }
                };
            }
        }

        private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count>0 && e.AddedItems[0] is ViewModels.FileItemViewModel vm1)
            {
                if (vm1.IsFolder)
                {
                    await SetFolder(vm1);
                }
                else
                {
                    FileOpenedEventHandler?.Invoke(this, vm1);
                    (sender as Microsoft.Toolkit.Uwp.UI.Controls.DataGrid).SelectedItem = null;
                }
            }
        }

        private async void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ViewModels.FileItemViewModel vm1)
            {
                if (vm1.IsFolder)
                {
                    await SetFolder(vm1);
                }
                else
                {
                    FileOpenedEventHandler?.Invoke(this, vm1);
                }

            }

        }

        public async Task OperateBinding(System.Func<ViewModels.ContentViewModel, Task> action)
        {
            if (this.DataContext is ViewModels.ContentViewModel vm)
            {
                await action(vm);
            }
        }

        private async void DataGrid_Sorting(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumnEventArgs e)
        {
            //async void setSort<T>(Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumn column, Func<ViewModels.FileItemViewModel,T> getSortKey,string key)
            //{
            //    await OperateBinding((vm) =>
            //        {
            //            if (vm?.Item == null) return Task.CompletedTask;
            //            switch (column.SortDirection)
            //            {
            //                case null:
            //                default:
            //                    vm.Item.OrderFunc = (a) => a.OrderBy(getSortKey);
            //                    vm.Item.OrderKey = (key, true);
            //                    //column.SortDirection = Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Ascending;
            //                    return Task.CompletedTask;
            //                case Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Ascending:
            //                    vm.Item.OrderFunc = (a) => a.OrderByDescending(getSortKey);
            //                    column.SortDirection = Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Descending;
            //                    return Task.CompletedTask;
            //                case Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Descending:
            //                    vm.Item.OrderFunc = null;
            //                    column.SortDirection = null;
            //                    return Task.CompletedTask;
            //            }
            //        });
            //}

            //switch (e.Column.Tag)
            //{
            //    case "Name":
            //        setSort(e.Column, a => a.Title,"Title");
            //        break;
            //    case "Size":
            //        setSort(e.Column, a => a.Size ?? 0,"Size");
            //        break;
            //    case "Date":
            //        setSort(e.Column, a => a.LastModified.Ticks,"Date");
            //        break;
            //}

            await OperateBinding((vm) =>
            {
                if (vm.Item == null) return Task.CompletedTask;
                if (vm.Item.Order == null)
                {
                    vm.Item.Order = new ViewModels.FileItemViewModel.OrderStatus().GetShiftedBasicOrder(e.Column.Tag as string);
                }
                else
                {
                    vm.Item.Order = vm.Item.Order.GetShiftedBasicOrder(e.Column.Tag as string);
                }

                foreach(var item in dataGrid.Columns)
                {
                    item.SortDirection = null;
                }

                if (vm.Item.Order.Key == e.Column.Tag as string && !string.IsNullOrEmpty(e.Column.Tag as string))
                {
                    e.Column.SortDirection = vm.Item.Order.KeyIsAscending ? Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Ascending : Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Descending;
                }
                else
                {
                    e.Column.SortDirection = null;
                }

                return Task.CompletedTask;
            });
        }
    }
}
