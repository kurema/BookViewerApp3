﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using winui = Microsoft.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views;

public sealed partial class FileExplorerControl : Page
{
	public FileExplorerControl()
	{
		this.InitializeComponent();

		if (this.DataContext is ViewModels.FileExplorerViewModel fvm && this.content.DataContext is ViewModels.ContentViewModel vm)
		{
			fvm.Content = vm;

			fvm.Content.PropertyChanged += async (s, e) =>
			{
				if (e.PropertyName == nameof(ViewModels.ContentViewModel.Item))
				{
					address.SetAddress(fvm.Content.Item);
					if (vm.SyncTreeView) try { await OpenTreeView(fvm.Content.Item); } catch { }
				}
			};
		}
	}

	public UIElementCollection MenuChildrens => this.pageMenuStack.Children;

	public async Task OpenTreeView(ViewModels.FileItemViewModel fv)
	{
		var list = new Stack<ViewModels.FileItemViewModel>();
		var cfv = fv;
		while (cfv != null)
		{
			list.Push(cfv);
			cfv = cfv.Parent;
		}

		var cnode = treeview.RootNodes;
		winui.TreeViewNode ctreenode = null;
		foreach (var item in list)
		{
			var ctreenodeTmp = cnode.FirstOrDefault(a => a.Content is ViewModels.FileItemViewModel fv2 && (fv2 == item || fv2.Content == item.Content));
			if (ctreenodeTmp is null) continue;
			ctreenode = ctreenodeTmp;
			if (ctreenode.Content is ViewModels.FileItemViewModel fvm && fvm.Children is null)
			{
				await fvm.UpdateChildren();
			}
			ctreenode.IsExpanded = true;
			treeview.Expand(ctreenode);
			cnode = ctreenode.Children;
		}
		//You can't get item in single selection mode before Microsoft.UI.Xaml v2.2.190731001-prerelease.
		//See.
		//https://github.com/microsoft/microsoft-ui-xaml/pull/243
		//So treeview is replaced by WinUI version.

		treeview.SelectedNode = ctreenode;

		//Maybe you can scroll...
		//See.
		//https://stackoverflow.com/questions/52015723/uwp-winui-treeview-programatically-scroll-to-item
		//But well... I dont like this.
	}

	public void SetTreeViewItem(params ViewModels.FileItemViewModel[] fileItemVMs)
	{
		foreach (var item in fileItemVMs)
		{
			treeview.RootNodes.Add(new winui.TreeViewNode()
			{
				IsExpanded = false,
				Content = item,
				HasUnrealizedChildren = item.IsFolder,
			});
		}
	}

	public IEnumerable<ViewModels.FileItemViewModel> GetTreeViewRoot()
	{
		return treeview.RootNodes?.Select(a => a.Content as ViewModels.FileItemViewModel);
	}

	private async void TreeView_Expanding(winui.TreeView sender, winui.TreeViewExpandingEventArgs args)
	{
		if (!args.Node.HasUnrealizedChildren) return;

		sender.IsEnabled = false;

		try
		{
			if (args.Item is ViewModels.FileItemViewModel vm)
			{
				if (vm.Children is null) await vm.UpdateChildren();
				args.Node.Children.Clear();
				foreach (var item in vm.Folders)
				{
					args.Node.Children.Add(new winui.TreeViewNode()
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

	public FileExplorerContentControl ContentControl => content;

	private async void Treeview_ItemInvoked(winui.TreeView sender, winui.TreeViewItemInvokedEventArgs args)
	{
		if (args.InvokedItem is winui.TreeViewNode tvn && tvn.Content is ViewModels.FileItemViewModel vm)
		{
			await content.SetFolder(vm);
			if (tvn.IsExpanded == false) tvn.IsExpanded = true;
		}
	}

	private void Address_FocusLostRequested(object sender, object e)
	{
		address.Opacity = 0;
		address.IsHitTestVisible = false;
		address_text.Opacity = 1;
		address_text.IsHitTestVisible = true;
		address_text.Focus(FocusState.Programmatic);

		address_text.SelectAll();
	}

	private void Address_text_FocusDisengaged(object sender, RoutedEventArgs e)
	{
		address.Opacity = 1;
		address.IsHitTestVisible = true;
		address_text.Opacity = 0;
		address_text.IsHitTestVisible = false;
	}

	//private void RadioButton_Checked(object sender, RoutedEventArgs e)
	//{
	//    if (sender is RadioButton rd)
	//    {
	//        if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
	//        {
	//            fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(rd.Tag.ToString(), fevm.Content.Item.Order?.KeyIsAscending ?? true);
	//        }
	//    }
	//}

	//private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
	//{
	//    if (sender is ToggleButton rd)
	//    {
	//        if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item != null)
	//        {
	//            fevm.Content.Item.Order = new ViewModels.FileItemViewModel.OrderStatus();
	//        }
	//    }
	//}

	//private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
	//{
	//    if (sender is ToggleButton tb)
	//    {
	//        if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
	//        {
	//            fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(fevm.Content.Item.Order.Key, tb.Tag.ToString() == "Ascending");
	//        }
	//    }
	//}

	//private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
	//{
	//    if (sender is ToggleButton tb)
	//    {
	//        if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
	//        {
	//            fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(fevm.Content.Item.Order.Key, tb.Tag.ToString() != "Ascending");
	//        }
	//    }
	//}

	private void UserControl_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var option = new FlyoutShowOptions();
		if (args.TryGetPosition(sender, out Point p)) option.Position = p;

		if (this.content.DataContext is ViewModels.ContentViewModel vm)
		{
			var menu = new MenuFlyout();
			foreach (var item in Models.MenuCommand.GetMenuFlyoutItems(vm.Item.MenuCommands)) menu.Items.Add(item);

			if (vm.Item?.IsFolder is true)
			{
				//var item = new MenuFlyoutItem()
				//{
				//    //ToDo: Fix and translate.
				//    Text = "Rename",
				//};
				//item.Click += async (sender, e) => await Helper.UIHelper.OpenRename(null);
				//menu.Items.Add(item);
			}
			{
				var item = new MenuFlyoutItem()
				{
					Text = Application.ResourceLoader.Loader.GetString("Command/Property"),
				};
				item.DataContext = vm.Item;
				item.Click += MenuFlyoutItem_Click_Property;
				menu.Items.Add(item);
			}
			menu.ShowAt(sender, option);
		}
		args.Handled = true;

	}

	private async void MenuFlyoutItem_Click_Property(object sender, RoutedEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is ViewModels.FileItemViewModel vm)
		{
			var dialog = new ContentDialog()
			{
				DataContext = vm,
				XamlRoot = this.XamlRoot,
			};
			{
				var loader = Application.ResourceLoader.Loader;
				dialog.CloseButtonText = loader.GetString("Command/OK");
			}

			dialog.Content = new PropertyControl();
			try
			{
				await dialog.ShowAsync();
			}
			catch { }
		}
	}

	private void TreeViewItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var option = new FlyoutShowOptions();
		if (args.TryGetPosition(sender, out Point p)) option.Position = p;

		if (((sender as FrameworkElement)?.DataContext as winui.TreeViewNode)?.Content is ViewModels.FileItemViewModel vm)
		{
			var menu = new MenuFlyout();
			foreach (var item in Models.MenuCommand.GetMenuFlyoutItems(vm.MenuCommands))
			{
				menu.Items.Add(item);
			}
			{
				var item = new MenuFlyoutItem()
				{
					Text = Application.ResourceLoader.Loader.GetString("Command/Property")

				};
				item.DataContext = vm;
				item.Click += MenuFlyoutItem_Click_Property;
				menu.Items.Add(item);
			}
			menu.ShowAt(sender, option);
		}
		args.Handled = true;
	}


	public System.Windows.Input.ICommand AddressRequesteCommand { get; set; }

	private void Address_text_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		var address = address_text.Text;
		if (e.Key == Windows.System.VirtualKey.Enter)
		{
			if (AddressRequesteCommand?.CanExecute(address) == true) AddressRequesteCommand.Execute(address);

			//Addressを更新するために、バインディングを再設定。
			//あんまりスマートには見えないが、
			//address_text.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
			//はUpdateSourceTriggerの値がExplicitでないと機能しないみたい。
			//https://docs.microsoft.com/ja-jp/dotnet/framework/wpf/data/how-to-control-when-the-textbox-text-updates-the-source
			address_text.SetBinding(TextBox.TextProperty, address_text.GetBindingExpression(TextBox.TextProperty)?.ParentBinding);
			e.Handled = true;
		}
		else
		{
			e.Handled = false;
		}
	}

	private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.F && Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
		{
			buttonMenu.Flyout.ShowAt(buttonMenu);
			await Task.Delay(100);
			searchBox.Focus(FocusState.Keyboard);
			e.Handled = true;
			return;
		}
		e.Handled = false;
	}

	private void Address_CopyAddress(object sender, RoutedEventArgs e)
	{
		var dataPackage = new DataPackage();
		dataPackage.RequestedOperation = DataPackageOperation.Copy;
		dataPackage.SetText(address_text.Text);
		Clipboard.SetContent(dataPackage);
	}

	private void address_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		if (this.DataContext is not ViewModels.FileExplorerViewModel fvm) return;
		var option = new FlyoutShowOptions();
		if (args.TryGetPosition(sender, out Point p)) option.Position = p;
		var menu = new MenuFlyout();
		if (fvm.Content?.Item?.Content is Models.FileItems.StorageFileItem sfi)
		{
			var item = new MenuFlyoutItem()
			{
				Text = Application.ResourceLoader.Loader.GetString("ContextMenu/Address/CopyAddress"),
			};
			item.Click += (sender, e) =>
			{
				var dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(fvm.Content?.Item?.Content?.Path ?? string.Empty);
				dataPackage.SetStorageItems(new[] { sfi.Content });
				Clipboard.SetContent(dataPackage);
			};
			menu.Items.Add(item);
		}
		if (!string.IsNullOrEmpty(fvm.Content?.Item?.Content?.Path))
		{
			var item = new MenuFlyoutItem()
			{
				Text = Application.ResourceLoader.Loader.GetString("ContextMenu/Address/CopyAddressAsText"),
			};
			item.Click += (sender, e) =>
			{
				var dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(fvm.Content?.Item?.Content?.Path ?? string.Empty);
				Clipboard.SetContent(dataPackage);
			};
			menu.Items.Add(item);
		}
		{
			var item = new MenuFlyoutItem()
			{
				Text = Application.ResourceLoader.Loader.GetString("ContextMenu/Address/EditAddress"),
			};
			item.Click += Address_FocusLostRequested;
			menu.Items.Add(item);
		}
		menu.ShowAt(sender, option);
		args.Handled = true;
	}
}
