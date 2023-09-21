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

using winui = Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

using Windows.Networking.BackgroundTransfer;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;

using BookViewerApp.Helper;
using BookViewerApp.Storages;
using static BookViewerApp.Storages.SettingStorage;
using Microsoft.UI.Xaml.Controls;
using BookViewerApp.Managers;
using Windows.Storage;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class TabPage : Page
{
	public winui.Controls.TabView Control => this.TabViewMain;

	private const string DataIdentifier = "MainTabItem";

	//https://docs.microsoft.com/ja-jp/windows/uwp/design/controls-and-patterns/tab-view
	public TabPage()
	{
		this.InitializeComponent();

		{
			this.RequestedTheme = ThemeManager.AsElementTheme;
			var theme = SettingInstances.FirstOrDefault(a => a.Key == SettingKeys.Theme);
			if (theme is not null) theme.ValueChanged += Theme_ValueChanged;
		}
	}

	private async void Theme_ValueChanged(object sender, EventArgs e)
	{
		var theme = ThemeManager.AsElementTheme;
		this.RequestedTheme = theme;
		//BackdropMaterial.SetApplyToRootOrPageBackground(this, ThemeManager.IsMica);
		foreach (var item in Control.TabItems)
		{
			if (item is not TabViewItem itemTvi) continue;
			itemTvi.RequestedTheme = theme;
			if (itemTvi.Content is FrameworkElement content)
			{
				content.RequestedTheme = theme;
			}
			if (itemTvi.Content is Frame f)
			{
				if (f.Content is kurema.BrowserControl.Views.BrowserControl2 contentWb2)
				{
					await contentWb2.WebView2.EnsureCoreWebView2Async();
					contentWb2.WebView2.CoreWebView2.Profile.PreferredColorScheme = ThemeManager.AsElementTheme switch
					{
						ElementTheme.Light => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light,
						ElementTheme.Dark => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark,
						ElementTheme.Default or _ => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Auto,
					};
				}
				if (f.Content is IThemeChangedListener listener)
				{
					listener?.OnThemeChanged();
				}
			}
		}
	}

	//ToDo: Delete when TabViewMain_TabItemsChanged no more need this.
	public AppWindow RootAppWindow = null;

	void SetupWindow(AppWindow window)
	{
		//https://github.com/microsoft/Xaml-Controls-Gallery/blob/f2d4568ec53464c3d290940282d2f70cfd62fa94/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
		if (window is null)
		{
			// Extend into the titlebar
			var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
			coreTitleBar.ExtendViewIntoTitleBar = true;

			coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

			var titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
			titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

			Window.Current.SetTitleBar(CustomDragRegion);
		}
		else
		{
			// Secondary AppWindows --- keep track of the window
			RootAppWindow = window;

			// Extend into the titlebar
			window.TitleBar.ExtendsContentIntoTitleBar = true;
			window.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
			window.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

			// Due to a bug in AppWindow, we cannot follow the same pattern as CoreWindow when setting the min width.
			// Instead, set a hardcoded number. 
			CustomDragRegion.MinWidth = 188;

			window.Frame.DragRegionVisuals.Add(CustomDragRegion);
		}
	}

	private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
	{
		if (FlowDirection == FlowDirection.LeftToRight)
		{
			CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
			ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
		}
		else
		{
			CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
			ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
		}

		CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
	}

	public async void OpenTabBookPicked()
	{
		await UIHelper.FrameOperation.OpenBookPicked(() => OpenTab("BookViewer"));
	}

	public void OpenTabBook(IEnumerable<Windows.Storage.IStorageItem> files)
	{
		if (files is null) return;
		foreach (var item in files) OpenTabBook(item);
	}

	public void OpenTabPdfJs(Windows.Storage.IStorageFile file)
	{
		if (file is null) return;
		var (frame, newTab) = OpenTab("PdfJs");
		UIHelper.FrameOperation.OpenPdfJs(frame, file, newTab);
	}

	public void OpenTabBrowser(Windows.Storage.IStorageFile file)
	{
		if (file is null) return;
		var (frame, newTab) = OpenTab("Browser");
		UIHelper.FrameOperation.OpenSingleFile(frame, file, newTab);
	}

	public async void OpenTabBook(Windows.Storage.IStorageItem file, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
	{
		if (file is Windows.Storage.IStorageFile item)
		{
			await UIHelper.FrameOperation.OpenBook(item, () => OpenTab("BookViewer"), () =>
			 {
				 //Dialog:Open dangerous file?
				 //Open.
				 //Or just ignore.
			 }, epubType);
		}
	}

	public void OpenTabBook(Stream stream)
	{
		var (frame, _) = OpenTab("BookViewer");
		frame.Navigate(typeof(BookFixed3Viewer), stream);
	}

	public (Frame, TabViewItem) OpenTab(string titleId, Type sourcePageType, object parameter)
	{
		var result = OpenTab(titleId);
		result.Item1.Navigate(sourcePageType, parameter);
		return result;
	}

	public async void OpenTabBook(System.Threading.Tasks.Task<Stream> stream)
	{
		OpenTabBook(await stream);
	}


	public async Task<(Frame, TabViewItemEx)> OpenTabExplorer()
	{
		var (frame, newTab) = OpenTab("Explorer");
		newTab.SessionInfo = new Storages.WindowStates.WindowStateWindowExplorerTab();
		await UIHelper.FrameOperation.OpenExplorer(frame, newTab);
		return (frame, newTab);
	}

	public void OpenTabMedia(kurema.FileExplorerControl.Models.FileItems.IFileItem file)
	{
		var (frame, newTab) = OpenTab("MediaPlayer");
		switch (file)
		{
			case kurema.FileExplorerControl.Models.FileItems.StorageFileItem sf when sf.Content is Windows.Storage.IStorageFile sfFile:
				newTab.Header = Path.GetFileNameWithoutExtension(sfFile.Path);
				newTab.SessionInfo = new Storages.WindowStates.WindowStateWindowMediaPlayerTab() { Path = sfFile.Path };
				frame.Navigate(typeof(kurema.FileExplorerControl.Views.Viewers.SimpleMediaPlayerPage), Windows.Media.Core.MediaSource.CreateFromStorageFile(sfFile));
				break;
		}
	}

	public void OpenTabWeb()
	{
		OpenTabWeb(SettingStorage.GetValue("WebHomePage") as string ?? "https://www.google.com/");
	}


	public async void OpenTabWeb(string uri)
	{
		var (frame, newTab) = OpenTab("Browser");
		newTab.SessionInfo = new Storages.WindowStates.WindowStateWindowBrowserTab() { Url = uri };

		//UIHelper.FrameOperation.OpenBrowser(frame, uri, (a) => OpenTabWeb(a), (a) => OpenTabBook(a), (title) =>
		//{
		//    newTab.Header = title;
		//}
		//);

		if ((bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserUseWebView2))
		{
			try
			{
				//https://github.com/MicrosoftEdge/WebView2Feedback/issues/2545
				//var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
				//if (string.IsNullOrEmpty(version)) throw new Exception();
				await UIHelper.FrameOperation.OpenBrowser2(frame, uri, OpenTabWeb, (title) =>
				{
					newTab.Header = title;
				}, () => this, newTab);
				return;
			}
			catch
			{
			}
		}
		{
			await UIHelper.FrameOperation.OpenBrowser(frame, uri, (a) => OpenTabWeb(a), (a) => OpenTabBook(a), (title) =>
			{
				newTab.Header = title;
			}
			, () => this, newTab);
		}
	}

	public async System.Threading.Tasks.Task OpenTabWebPreferedBrowser(string uri)
	{
		if ((bool)SettingStorage.GetValue("DefaultBrowserExternal"))
		{
			if (Uri.TryCreate(uri, UriKind.Absolute, out var uri1))
			{
				await Windows.System.Launcher.LaunchUriAsync(uri1);
			}
		}
		else
		{
			OpenTabWeb(uri);
		}
	}

	public void OpenTabSetting()
	{
		var (frame, t) = OpenTab("Setting");
		t.SessionInfo = new Storages.WindowStates.WindowStateWindowSettingTab();
		frame?.Navigate(typeof(SettingPage));
	}

	public void OpenTabBookshelf()
	{
		var (frame, t) = OpenTab("Bookshelf");
		t.SessionInfo = new Storages.WindowStates.WindowStateWindowBookshelfTab();
		frame?.Navigate(typeof(Bookshelf.NavigationPage));
	}

	public kurema.FileExplorerControl.Views.Viewers.TextEditorPage OpenTabTextEditor(IStorageFile file)
	{
		var (frame, t) = OpenTab("TextEditor", file.Name, file.Path);
		t.SessionInfo = new Storages.WindowStates.WindowStateWindowTextEditorTab() { Path = file.Path };
		frame?.Navigate(typeof(kurema.FileExplorerControl.Views.Viewers.TextEditorPage), file);
		return frame.Content as kurema.FileExplorerControl.Views.Viewers.TextEditorPage;
	}

	public void OpenTabTextEditor(kurema.FileExplorerControl.Models.FileItems.IFileItem file)
	{
		var (frame, t) = OpenTab("TextEditor", file.Name, file.Path);
		t.SessionInfo = new Storages.WindowStates.WindowStateWindowTextEditorTab() { Path = file.Path };
		frame?.Navigate(typeof(kurema.FileExplorerControl.Views.Viewers.TextEditorPage), file);
	}


	public (Frame, TabViewItemEx) OpenTab(string titleId, params object[] args)
	{
		var newTab = new TabViewItemEx();
		var titleString = UIHelper.GetTitleByResource(titleId);
		if (args is not null and { Length: > 0 }) titleString = string.Format(titleString, args);
		newTab.Header = string.IsNullOrWhiteSpace(titleString) ? "New Tab" : titleString;

		Frame frame = new();
		newTab.Content = frame;

		TabViewMain.TabItems.Add(newTab);
		TabViewMain.SelectedItem = newTab;

		frame.Loaded += (_, _) => frame.Focus(FocusState.Programmatic);
		frame.RequestedTheme = ThemeManager.AsElementTheme;

		return (frame, newTab);
	}

	private async void TabViewMain_AddTabButtonClick(winui.Controls.TabView sender, object args)
	{
		await OpenTabExplorer();
	}

	private async void TabViewMain_TabCloseRequested(winui.Controls.TabView sender, winui.Controls.TabViewTabCloseRequestedEventArgs args)
	{
		if (RootAppWindow == null && TabViewMain.TabItems.Count == 1)
		{
			await OpenTabExplorer();
			await CloseTab(args.Tab);
			TabViewMain.SelectedIndex = 0;
		}
		else
		{
			await CloseTab(args.Tab);
		}
	}

	public async System.Threading.Tasks.Task CloseTab(winui.Controls.TabViewItem tab, bool dispose = true)
	{
		if (tab is null) return;
		if (dispose)
		{
			//((tab?.Content as Frame)?.Content as BookFixed3Viewer)?.CloseOperation();
			((tab?.Content as Frame)?.Content as IDisposable)?.Dispose();
			((tab?.Content as Frame)?.Content as IDisposableBasic)?.DisposeBasic();
			(tab?.Content as Frame)?.Navigate(typeof(Page));
		}

		if (tab.IsClosable)
		{
			TabViewMain.TabItems.Remove(tab);
		}

		if (TabViewMain.TabItems.Count == 0)
		{
			//https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
			//This is far from smartness. But this is in microsoft repo.
			//There should be a way to detect this is mainwindow or not, but I dont know.
			if (RootAppWindow != null)
			{
				await RootAppWindow.CloseAsync();
			}
			else
			{
				try
				{
					//I need to close only main window. But error occurs and I have no idea how to fix it...
					//Closing main window is not allowed.
					//There is a question but no answer.
					//https://stackoverflow.com/questions/39944258/closing-main-window-is-not-allowed

					//Application.Current.Exit();
					//Window.Current.Close();
					//OpenTabWeb("https://www.google.com/");
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

	}

	#region keyAccelerator
	//二回発火するのと、WebView使ってると機能していないみたいなんでオフにしました。

	//private DateTimeOffset LastKeyboardActionDateTime = new();
	//private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	//{
	//    _ = DateTimeOffset.UtcNow - LastKeyboardActionDateTime;
	//    if ((DateTimeOffset.UtcNow - LastKeyboardActionDateTime).TotalSeconds > 0.2)
	//    {
	//        OpenTabWeb();
	//        LastKeyboardActionDateTime = DateTimeOffset.UtcNow;
	//    }
	//}

	//private async void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	//{
	//    if ((DateTimeOffset.UtcNow - LastKeyboardActionDateTime).TotalSeconds > 0.2)
	//    {
	//        await CloseTab(((winui.Controls.TabViewItem)TabViewMain?.SelectedItem));
	//        LastKeyboardActionDateTime = DateTimeOffset.UtcNow;
	//    }
	//}

	//private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	//{
	//    int tabToSelect = 0;

	//    switch (sender.Key)
	//    {
	//        case Windows.System.VirtualKey.Number1:
	//            tabToSelect = 0;
	//            break;
	//        case Windows.System.VirtualKey.Number2:
	//            tabToSelect = 1;
	//            break;
	//        case Windows.System.VirtualKey.Number3:
	//            tabToSelect = 2;
	//            break;
	//        case Windows.System.VirtualKey.Number4:
	//            tabToSelect = 3;
	//            break;
	//        case Windows.System.VirtualKey.Number5:
	//            tabToSelect = 4;
	//            break;
	//        case Windows.System.VirtualKey.Number6:
	//            tabToSelect = 5;
	//            break;
	//        case Windows.System.VirtualKey.Number7:
	//            tabToSelect = 6;
	//            break;
	//        case Windows.System.VirtualKey.Number8:
	//            tabToSelect = 7;
	//            break;
	//        case Windows.System.VirtualKey.Number9:
	//            // Select the last tab
	//            tabToSelect = TabViewMain.TabItems.Count - 1;
	//            break;
	//    }

	//    // Only select the tab if it is in the list
	//    if (tabToSelect < TabViewMain.TabItems.Count)
	//    {
	//        TabViewMain.SelectedIndex = tabToSelect;
	//    }
	//}
	#endregion

	public Storages.WindowStates.WindowStatesLast GetWindowStates()
	{
		return new Storages.WindowStates.WindowStatesLast()
		{
			WindowState = new Storages.WindowStates.WindowState()
			{
				Window = new Storages.WindowStates.WindowStateWindow[]
				{
					new Storages.WindowStates.WindowStateWindow(){
						Items= TabViewMain.TabItems.Select(a => (a as TabViewItemEx)?.SessionInfo).Where(a => a is not null).ToArray(),
						isTop=true,
					}
				}
			}
		};
	}

	public async void OpenWindowState(Storages.WindowStates.WindowStatesLast last)
	{
		async Task<StorageFile> GetStorage(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				try
				{
					return await StorageFile.GetFileFromPathAsync(path);
				}
				catch { }
			}
			return null;
		}

		if (!(last?.WindowState?.Window?.FirstOrDefault()?.Items?.Length > 0)) return;
		foreach (var item in last.WindowState.Window[0].Items)
		{
			try
			{
				switch (item)
				{
					case Storages.WindowStates.WindowStateWindowBookshelfTab bs:
						OpenTabBookshelf();
						break;
					case Storages.WindowStates.WindowStateWindowBrowserTab br:
						if (string.IsNullOrEmpty(br.Url)) OpenTabWeb();
						else OpenTabWeb(br.Url);
						break;
					case Storages.WindowStates.WindowStateWindowExplorerTab ex:
						//ToDo: Load path;
						var (f, _) = await OpenTabExplorer();
						try { await OpenExplorerPath(f?.Content as kurema.FileExplorerControl.Views.FileExplorerControl, ex.Structure); } catch { }
						break;
					case Storages.WindowStates.WindowStateWindowMediaPlayerTab md:
						{ if (await GetStorage(md.Path) is { } storage) { OpenTabMedia(new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(storage)); break; } }
						break;
					case Storages.WindowStates.WindowStateWindowSettingTab st:
						OpenTabSetting();
						break;
					case Storages.WindowStates.WindowStateWindowTextEditorTab te:
						{ if (await GetStorage(te.Path) is { } storage) { OpenTabTextEditor(storage); break; } }
						break;
					case Storages.WindowStates.WindowStateWindowViewerTab vw:
						{ if (await GetStorage(vw.Path) is { } storage) { OpenTabBook(storage); break; } }
						if (!string.IsNullOrEmpty(vw.Token) && HistoryManager.List.ContainsItem(vw.Token))
						{
							var storage = await HistoryManager.List.GetItemAsync(vw.Token);
							OpenTabBook(storage);
						}
						break;
				}
			}
			catch { }
		}
	}

	async static Task OpenExplorerPath(kurema.FileExplorerControl.Views.FileExplorerControl control, IEnumerable<Storages.WindowStates.WindowStateWindowExplorerTabItem> items)
	{
		if (control is null || control.DataContext is not kurema.FileExplorerControl.ViewModels.FileExplorerViewModel vm) return;
		var children = control.GetTreeViewRoot();
		if (children is null) return;
		if (items is null) return;
		kurema.FileExplorerControl.ViewModels.FileItemViewModel result = null;
		foreach (var item in items.Skip(1))
		{
			{
				var hits = children.Where(a => a.Path == item.Path).ToArray();
				if (!string.IsNullOrEmpty(item.Path) && hits.Count() == 1)
				{
					result = hits[0];
					await result.UpdateChildren();
					children = result.Children;
					continue;
				}
			}
			{
				var hits = children.Where(a => a.Title == item.DisplayedName).ToArray();
				if (!string.IsNullOrWhiteSpace(item.DisplayedName) && hits.Count() == 1)
				{
					result = hits[0];
					await result.UpdateChildren();
					children = result.Children;
					continue;
				}
			}
			break;
		}
		await control.ContentControl.SetFolder(result);
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		if (e.Parameter == null || (e.Parameter is string s && s == ""))
		{
			_ = OpenTabExplorer();
		}
		else if (e.Parameter is Storages.WindowStates.WindowStatesLast last)
		{
			if (last?.WindowState?.Window?.FirstOrDefault()?.Items?.Length > 0) OpenWindowState(last);
			else _ = OpenTabExplorer();
		}
		else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs args)
		{
			if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
			{
				foreach (var item in ((Windows.ApplicationModel.Activation.FileActivatedEventArgs)args).Files)
				{
					OpenTabBook(item);
				}
			}
		}
		else if (e.Parameter is Windows.Storage.IStorageItem f)
		{
			OpenTabBook(f);
		}

		SetupWindow(null);
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		var theme = SettingInstances.FirstOrDefault(a => a.Key == SettingKeys.Theme);
		if (theme is not null) theme.ValueChanged -= Theme_ValueChanged;

		base.OnNavigatedFrom(e);
	}

	public void AddItemToTabs(winui.Controls.TabViewItem tab)
	{
		TabViewMain.TabItems.Add(tab);
		TabViewMain.SelectedItem = tab;
	}

	private async void TabViewMain_TabDroppedOutside(winui.Controls.TabView sender, winui.Controls.TabViewTabDroppedOutsideEventArgs e)
	{
		//{
		//    CloseTab(e.Tab);

		//    var newView = CoreApplication.CreateNewView();
		//    await newView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
		//    {
		//        var frame = new Frame();
		//        frame.Navigate(typeof(TabPage), null);

		//        (frame.Content as TabPage)?.AddItemToTabs(e.Tab);

		//        Window.Current.Content = frame;
		//        Window.Current.Activate();
		//    });

		//    return;
		//}

		{
			if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				return;
			}
			if (sender.TabItems.Count <= 1) return;

			var newWindow = await AppWindow.TryCreateAsync();
			var newPage = new TabPage();
			newPage.SetupWindow(newWindow);
			await CloseTab(e.Tab, false);
			newPage.AddItemToTabs(e.Tab);
			Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetAppWindowContent(newWindow, newPage);

			// Show the window
			await newWindow.TryShowAsync();
		}
	}

	private void TabViewMain_TabItemsChanged(winui.Controls.TabView sender, IVectorChangedEventArgs args)
	{
		////This is buggy when you exit FullScreen.
		//if (sender.TabItems.Count == 0)
		//{
		//    //https://github.com/microsoft/Xaml-Controls-Gallery/blob/master/XamlControlsGallery/TabViewPages/TabViewWindowingSamplePage.xaml.cs
		//    //This is far from smartness. But this is in microsoft repo.
		//    //There should be a way to detect this is mainwindow or not, but I dont know.
		//    if (RootAppWindow != null)
		//    {
		//        await RootAppWindow.CloseAsync();
		//    }
		//    else
		//    {
		//        try
		//        {
		//            Application.Current.Exit();
		//            //Window.Current.Close();
		//            //OpenTabWeb("https://www.google.com/");
		//        }
		//        catch (System.InvalidOperationException e)
		//        {
		//        }
		//    }
		//}
	}

	private async void TabViewMain_TabStripDrop(object sender, DragEventArgs e)
	{
		if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
		{
			if (obj is null) return;
			var destinationTabView = sender as winui.Controls.TabView;
			if (destinationTabView.TabItems != null)
			{
				var index = -1;
				for (int i = 0; i < destinationTabView.TabItems.Count; i++)
				{
					var item = destinationTabView.ContainerFromIndex(i) as winui.Controls.TabViewItem;

					if (e.GetPosition(item).X - item.ActualWidth < 0)
					{
						index = i;
						break;
					}

				}

				{
					//var destinationTabViewListView = ((obj as winui.Controls.TabViewItem).Parent as winui.Controls.Primitives.TabViewListView);
					//destinationTabViewListView?.Items.Remove(obj);
					var tabPage = UIHelper.GetCurrentTabPage(obj as UIElement);
					if (tabPage != null) await tabPage.CloseTab(obj as winui.Controls.TabViewItem, false); else return;
					//if ((obj as winui.Controls.TabViewItem).XamlRoot.Content is TabPage tabPage) await tabPage.CloseTab(obj as winui.Controls.TabViewItem);
				}

				if (index < 0)
				{
					destinationTabView.TabItems.Add(obj);
				}
				else if (index < destinationTabView.TabItems.Count)
				{
					destinationTabView.TabItems.Insert(index, obj);
				}

				// Select the newly dragged tab
				destinationTabView.SelectedItem = obj;
			}
		}
	}

	private void TabViewMain_TabStripDragOver(object sender, DragEventArgs e)
	{
		if (e.DataView.Properties.ContainsKey(DataIdentifier))
		{
			e.AcceptedOperation = DataPackageOperation.Move;
		}
	}

	private void TabViewMain_TabDragStarting(winui.Controls.TabView sender, winui.Controls.TabViewTabDragStartingEventArgs args)
	{
		//Closing main window causes a problem. So disable it.
		if (this.TabViewMain.TabItems.Count <= 1 && RootAppWindow is null) return;

		var firstItem = args.Tab;
		args.Data.Properties.Add(DataIdentifier, firstItem);
		args.Data.RequestedOperation = DataPackageOperation.Move;
	}

	private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement elem && elem.Tag != null)
		{
			switch (elem.Tag.ToString())
			{
				case "Explorer": await this.OpenTabExplorer(); break;
				case "Browser": this.OpenTabWeb(); break;
				case "Setting": this.OpenTabSetting(); break;
				case "Picker": this.OpenTabBookPicked(); break;
				case "Bookshelf": this.OpenTabBookshelf(); break;
			}
		}
	}

	//private void AddButtonMenu_Click(object sender, RoutedEventArgs e)
	//{
	//    MenuFlyoutItem GetMenu(string title,Action action)
	//    {
	//        var menuItem = new MenuFlyoutItem();
	//        menuItem.Text = title;
	//        menuItem.Click += (s2, e2) => action?.Invoke();
	//        return menuItem;
	//    }

	//    if(sender is Button button)
	//    {
	//        var menu = new MenuFlyout() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
	//        menu.Items.Add(GetMenu("Explorer", () => OpenTabExplorer()));
	//        button.Flyout = menu;
	//    }
	//}
}
