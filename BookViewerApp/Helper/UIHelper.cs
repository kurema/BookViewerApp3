﻿using System.Collections.Generic;
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

using winui = Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;

using System.Threading.Tasks;
using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Views;

using System;
using System.Linq;
using System.IO;
using BookViewerApp.Storages;
using static BookViewerApp.Storages.SettingStorage;
using BookViewerApp.Storages.ExtensionAdBlockerItems;

namespace BookViewerApp.Helper;

public static partial class UIHelper
{
	public static void SetTitle(FrameworkElement targetElement, string title)
	{
		var tab = GetCurrentTabViewItem(targetElement);
		if (tab is not null)
		{
			tab.Header = title;
		}
		else
		{
			ApplicationView.GetForCurrentView().Title = title;
		}
	}

	public static winui.Controls.TabViewItem GetCurrentTabViewItem(FrameworkElement targetElement)
	{
		{ if (((targetElement as Page)?.Frame)?.Parent is winui.Controls.TabViewItem item) return item; }
		{ if ((targetElement.Parent as Frame)?.Parent is winui.Controls.TabViewItem item) return item; }
		{ if (targetElement is winui.Controls.TabViewItem item) return item; }
		return null;
	}

	public static void SetTitleByResource(FrameworkElement targetElement, string id) => SetTitle(targetElement, GetTitleByResource(id));

	public static string GetTitleByResource(string id) => ResourceManager.Loader.GetString("TabHeader/" + id);

	public static TabPage GetCurrentTabPage(UIElement ui)
	{
		if ((ui?.XamlRoot?.Content as Frame)?.Content is TabPage tab)
		{
			return tab;
		}
		else if (ui?.XamlRoot?.Content is TabPage tab3)
		{
			return tab3;
		}
		//else if (Window.Current?.Content is Frame f && f.Content is TabPage tab2)
		//{
		//    return tab2;
		//}
		return null;
	}

	public static string GetFileTypeDescription(Windows.Storage.IStorageItem item)
	{
		if (item is null) return null;
		_ = Path.GetExtension(item.Path);
		if (item is Windows.Storage.StorageFolder)
		{
			return kurema.FileExplorerControl.Application.ResourceLoader.Loader.GetString("FileType/Folder");
		}
		return kurema.FileExplorerControl.Models.FileItems.StorageFileItem.GetGeneralFileType(item.Path);
	}

	public async static Task RequestPurchaseAsync(Windows.ApplicationModel.Store.ProductListing product)
	{
		var loader = ResourceManager.Loader;

		var owned = LicenseManager.IsActive(product);
		if (owned)
		{
			try { await new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/AlreadyPurchased/Message"), loader.GetString("Info/Purchase/Message/AlreadyPurchased/Title")).ShowAsync(); } catch { }
		}
		else
		{
			try
			{
				//https://docs.microsoft.com/en-us/answers/questions/3971/windowsapplicationmodelstorecurrentappsimulatorreq.html
				//It always return NotPurchased
				var result = await LicenseManager.RequestPurchaseAsync(product);
				switch (result.Status)
				{
					case Windows.ApplicationModel.Store.ProductPurchaseStatus.Succeeded:
						try { await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/Purchased/Message"), loader.GetString("Info/Purchase/Message/Purchased/Title"))).ShowAsync(); } catch { }
						break;
					case Windows.ApplicationModel.Store.ProductPurchaseStatus.AlreadyPurchased:
						try { await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/AlreadyPurchased/Message"), loader.GetString("Info/Purchase/Message/AlreadyPurchased/Title"))).ShowAsync(); } catch { }
						break;
					case Windows.ApplicationModel.Store.ProductPurchaseStatus.NotFulfilled:
						break;
					case Windows.ApplicationModel.Store.ProductPurchaseStatus.NotPurchased:
						//#if DEBUG
						//                            await (new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Purchase/Message/Purchased/Message"), loader.GetString("Info/Purchase/Message/Purchased/Title"))).ShowAsync();
						//#endif
						return;
					default:
						break;
				}
			}
			catch
			{
			}
		}
	}

	public static async Task OpenWebExternal(string address)
	{
		if (Uri.TryCreate(address, UriKind.Absolute, out var uri)) await Windows.System.Launcher.LaunchUriAsync(uri);
	}

	public async static Task<kurema.FileExplorerControl.ViewModels.FileItemViewModel> GetFileItemViewModelFromRoot(string address, IEnumerable<kurema.FileExplorerControl.ViewModels.FileItemViewModel> root)
	{
		if (root is null) return null;
		var folders = root?.FirstOrDefault(a => a.Content.Tag is Storages.LibraryStorage.LibraryKind kind && kind == Storages.LibraryStorage.LibraryKind.Folders);
		if (folders is null) return null;
		if (folders.Children is null) await folders.UpdateChildren();
		var currentDir = address;
		kurema.FileExplorerControl.ViewModels.FileItemViewModel result = null;

		var pathList = new Stack<string>();
		while (true)
		{
			foreach (var item in folders.Children)
			{
				var rel = Path.GetRelativePath(item.Path, currentDir.ToString());
				if (Path.GetRelativePath(item.Path, currentDir.ToString()) == ".")
				{
					result = item;
					goto outofwhile;
				}
			}

			if (!string.IsNullOrEmpty(Path.GetFileName(currentDir))) pathList.Push(Path.GetFileName(currentDir));

			currentDir = Path.GetDirectoryName(currentDir);
			if (string.IsNullOrEmpty(currentDir)) return null;
		}
	outofwhile:;

		foreach (var item in pathList)
		{
			if (result.Children is null) await result.UpdateChildren();
			result = result.Children.FirstOrDefault(a => a.Title == item);
			if (result is null) return null;
		}
		await result.UpdateChildren();
		return result;
	}

	public static void ChangeViewWithKeepCurrentCenter(ScrollViewer sv, float zoomFactor)
	{
		zoomFactor = Math.Min(zoomFactor, sv.MaxZoomFactor);
		double originalCenterX;
		if (sv.ViewportWidth < sv.ExtentWidth)
		{
			double eCenterX = sv.HorizontalOffset + sv.ViewportWidth / 2;
			originalCenterX = eCenterX / sv.ZoomFactor;
		}
		else
		{
			double eCenterX = sv.HorizontalOffset + sv.ExtentWidth / 2;
			originalCenterX = eCenterX / sv.ZoomFactor;
		}

		double originalCenterY;
		if (sv.ViewportHeight < sv.ExtentHeight)
		{
			double eCenterY = sv.VerticalOffset + sv.ViewportHeight / 2;
			originalCenterY = eCenterY / sv.ZoomFactor;
		}
		else
		{
			double eCenterY = sv.VerticalOffset + sv.ExtentHeight / 2;
			originalCenterY = eCenterY / sv.ZoomFactor;
		}


		double newExtentCenterX = originalCenterX * zoomFactor;
		double newExtentCenterY = originalCenterY * zoomFactor;

		double newExtentOffsetX = newExtentCenterX - sv.ViewportWidth / 2;
		double newExtentOffsetY = newExtentCenterY - sv.ViewportHeight / 2;
		sv.ChangeView(newExtentOffsetX, newExtentOffsetY, zoomFactor, true);
	}
}
