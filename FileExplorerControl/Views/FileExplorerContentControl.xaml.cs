﻿using System.Collections.Generic;
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
using Windows.UI.Xaml;



// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views;

public sealed partial class FileExplorerContentControl : UserControl
{
    public FileExplorerContentControl()
    {
        this.InitializeComponent();

        {
            var loader = Application.ResourceLoader.Loader;
            this.headerName.Header = loader.GetString("Header/Name");
            this.headerDate.Header = loader.GetString("Header/Date");
            this.headerSize.Header = loader.GetString("Header/Size");
        }
    }

    public async Task SetFolder(ViewModels.FileItemViewModel folder)
    {
        await OperateBinding(async (vm) =>
        {
            vm?.Item?.IconFetchingCancel();
            if (folder is null) return;
            if (folder.Children is null) await folder.UpdateChildren();
            folder.IconFetchingUnCancel();
            vm.Item = folder;
            FolderChangedHandler?.Invoke(this, vm);
		});

	}

    public TypedEventHandler<FileExplorerContentControl, ViewModels.FileItemViewModel> FileOpenedEventHandler;

	public TypedEventHandler<FileExplorerContentControl, ViewModels.ContentViewModel> FolderChangedHandler;


	private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is ViewModels.ContentViewModel vm)
        {
            void OrderChange(object s, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(vm.Item.Order))
                {
                    foreach (var column in dataGrid.Columns)
                    {
                        if (vm.Item.Order.Key == column.Tag as string && !string.IsNullOrEmpty(column.Tag as string))
                        {
                            column.SortDirection = vm.Item.Order.KeyIsAscending ? Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Ascending : Microsoft.Toolkit.Uwp.UI.Controls.DataGridSortDirection.Descending;
                        }
                        else
                        {
                            column.SortDirection = null;
                        }
                    }
                }
            }
            vm.DialogDelete = async (arg, canDeleteComplete) =>
            {
                var dialog = new DeleteContentDialog() { IsSecondaryButtonEnabled = canDeleteComplete, XamlRoot = this.XamlRoot };
                dialog.DataContext = arg;
                Windows.UI.Xaml.Controls.ContentDialogResult result;
                try
                {
                    result = await dialog.ShowAsync();
                }
                catch
                {
                    return (false, false);
                }
                return result switch
                {
                    ContentDialogResult.Primary => (true, false),
                    ContentDialogResult.Secondary => (true, true),
                    _ => (false, false),
                };
            };


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
                if (e.PropertyName == nameof(vm.Item))
                {
                    vm.Item.PropertyChanged += OrderChange;
                }
            };
            vm.SetDefaultOrderSelectors();
        }
    }

    private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ViewModels.FileItemViewModel vm1)
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

    private async void Button_Tapped_Open(object sender, object e)
    {
        if ((sender as Button)?.DataContext is ViewModels.FileItemViewModel vm1)
        {
            await Open(vm1);
        }
    }

    public async Task OperateBinding(Func<ViewModels.ContentViewModel, Task> action)
    {
        if (this.DataContext is ViewModels.ContentViewModel vm)
        {
            if (action is not null) await action.Invoke(vm);
        }
    }

    private async void DataGrid_Sorting(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumnEventArgs e)
    {
        await OperateBinding((vm) =>
        {
            if (vm.Item is null) return Task.CompletedTask;
            if (vm.Item.Order is null)
            {
                vm.Item.Order = new ViewModels.FileItemViewModel.OrderStatus().GetShiftedBasicOrder(e.Column.Tag as string);
            }
            else
            {
                vm.Item.Order = vm.Item.Order.GetShiftedBasicOrder(e.Column.Tag as string);
            }

            return Task.CompletedTask;
        });
    }

    private async Task Open(ViewModels.FileItemViewModel file)
    {
        if (file is null) return;
        else if (file.IsFolder)
        {
            await SetFolder(file);
        }
        else
        {
            FileOpenedEventHandler?.Invoke(this, file);
        }
    }

    private async void MenuFlyoutItem_Click_Open(object sender, object e)
    {
        if ((sender as MenuFlyoutItem)?.DataContext is ViewModels.FileItemViewModel vm1)
        {
            await Open(vm1);
        }
    }

    private async void MenuFlyoutItem_Click_Property(object sender, RoutedEventArgs e)
    {
        if ((sender as MenuFlyoutItem)?.DataContext is ViewModels.FileItemViewModel vm1)
        {
            var dialog = new ContentDialog()
            {
                DataContext = vm1,
                XamlRoot = this.XamlRoot,
                //IsSecondaryButtonEnabled=true,
                //SecondaryButtonText="Float",
            };
            {
                var loader = Application.ResourceLoader.Loader;
                dialog.CloseButtonText = loader.GetString("Command/OK");
            }

            dialog.Content = new PropertyControl();
            //dialog.SecondaryButtonClick += (s2, e2) =>
            //{
            //};
            try
            {
                await dialog.ShowAsync();
            }
            catch { }
        }
    }

    //private void StackPanel_LostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (((sender as StackPanel).Parent as FlyoutPresenter).Parent is Popup f)
    //    {
    //        f.IsOpen = false;
    //    }
    //}

    private void Button_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (sender is FrameworkElement f && f.DataContext is ViewModels.FileItemViewModel vm)
        {
            var menu = new MenuFlyout();
            {
                var openCommand = new StandardUICommand(StandardUICommandKind.Open);
                openCommand.ExecuteRequested += async (s, e) =>
                {
                    await Open(vm);
                };
                var item = new MenuFlyoutItem()
                {
                    Text = Application.ResourceLoader.Loader.GetString("Command/Open"),
                    Command = openCommand
                };
                //item.Click += MenuFlyoutItem_Click_Open;
                menu.Items.Add(item);
            }
            foreach (var item in Models.MenuCommand.GetMenuFlyoutItems(vm.MenuCommands))
            {
                //if(item is MenuFlyoutItem itemM)
                //{
                //    itemM.CommandParameter = this.DataContext;
                //}
                menu.Items.Add(item);
            }
            {
                var item = new MenuFlyoutItem()
                {
                    Text = Application.ResourceLoader.Loader.GetString("Command/Delete"),
                    Command = new StandardUICommand(StandardUICommandKind.Delete) { Command = vm.DeleteCommand },
                    Icon = new SymbolIcon(Symbol.Delete),
                };
                menu.Items.Add(item);
            }
            {
                var item = new MenuFlyoutItem()
                {
                    Text = Application.ResourceLoader.Loader.GetString("Command/Property")

                };
                item.Click += MenuFlyoutItem_Click_Property;
                menu.Items.Add(item);
            }

            var option = new FlyoutShowOptions();
            if (args.TryGetPosition(sender, out Point p)) option.Position = p;
            menu.ShowAt(f, option);

            args.Handled = true;
        }
    }

    //private async void items_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    //{
    //    var option = new FlyoutShowOptions() { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
    //    if (args.TryGetPosition(sender, out Point p)) option.Position = p;

    //    await OperateBinding(vm =>
    //    {
    //        var menu = new MenuFlyout();
    //        foreach (var item in Models.MenuCommand.GetMenus(vm.Item.MenuCommands)) menu.Items.Add(item);
    //        {
    //            var item = new MenuFlyoutItem()
    //            {
    //                Text = Application.ResourceLoader.Loader.GetString("Command/Property")

    //            };
    //            item.DataContext = vm.Item;
    //            item.Click += MenuFlyoutItem_Click_Property;
    //            menu.Items.Add(item);
    //        }
    //        menu.ShowAt(sender, option);

    //        return Task.CompletedTask;
    //    });
    //    args.Handled = true;
    //}

    private void DataGrid_LoadingRow(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowEventArgs e)
    {
        e.Row.ContextRequested += Button_ContextRequested;
        //e.Row.Tapped += async (s, e2) =>
        //{
        //    if (e2.OriginalSource is ViewModels.FileItemViewModel vm1)
        //    {
        //        if (vm1.IsFolder)
        //        {
        //            await SetFolder(vm1);
        //        }
        //        else
        //        {
        //            FileOpenedEventHandler?.Invoke(this, vm1);
        //            (sender as Microsoft.Toolkit.Uwp.UI.Controls.DataGrid).SelectedItem = null;
        //        }
        //    }
        //};
    }
}
