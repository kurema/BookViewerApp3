﻿using System;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.Networking.BackgroundTransfer;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using BookViewerApp.Views;

using System.Threading.Tasks;
using System.ServiceModel.Channels;

namespace BookViewerApp.Helper;

public static partial class UIHelper
{
	public static class FrameOperation
	{
		public static async Task OpenPdfJs(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, TabPage tabPage = null, string token = null)
		{
			if (file is null) return;
			if (sender is null) return;
			SetTitle(sender, Path.GetFileNameWithoutExtension(file.Name));
			tabPage ??= GetCurrentTabPage(sender);

			EpubResolverBase resolver = await EpubResolverBase.GetResolverPdfJs(file);
			frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

			if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
			if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
			if (tabPage is null) return;

			{
				content.WebView2.CoreWebView2Initialized += (s, e) =>
				{
					OpenBrowser2_UpdateCoreStuffs(content.WebView2.CoreWebView2, async (a) =>
					{
						await tabPage.OpenTabWebPreferedBrowser(a);
					}, t => SetTitle(sender, t));
				};

				OpenBrowser_BookmarkSetViewModel(vm, () => GetCurrentTabPage(sender));
				var uri = resolver.GetUri(resolver.PathHome);
				await content.WebView2.EnsureCoreWebView2Async();
				//https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.addwebresourcerequestedfilter?view=webview2-dotnet-1.0.1293.44
				content.WebView2.CoreWebView2.AddWebResourceRequestedFilter($"*://{resolver.Host}/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
				content.WebView2.CoreWebView2.WebResourceRequested += resolver.WebResourceRequested;
				vm.SourceString = uri;
				vm.HomePage = uri;
				vm.ControllerCollapsed = true;
			}
			{
				var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));
				//ID can not be saved properly here. It cause thumbnail problem in the history page of Filer.
				//var token = HistoryManager.AddEntry(file);
				if (tagInfo is not null) tagInfo.SessionInfo = new Storages.WindowStates.WindowStateWindowViewerTab() { Token = token, ViewerKey = "PDF.js", Path = file.Path };
			}
		}

		public static async Task OpenSingleFile(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, TabPage tabPage = null, string token = null)
		{
			if (file is null) return;
			if (sender is null) return;
			SetTitle(sender, Path.GetFileNameWithoutExtension(file.Name));
			tabPage ??= GetCurrentTabPage(sender);

			frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

			if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
			if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
			if (tabPage is null) return;

			{
				content.WebView2.CoreWebView2Initialized += (s, e) =>
				{
					OpenBrowser2_UpdateCoreStuffs(content.WebView2.CoreWebView2, async (a) =>
					{
						await tabPage.OpenTabWebPreferedBrowser(a);
					}, t => SetTitle(sender, t));
				};

				OpenBrowser_BookmarkSetViewModel(vm, () => GetCurrentTabPage(sender));

				var uri = $"https://file.example/{System.Web.HttpUtility.UrlEncode(System.IO.Path.GetFileName(file.Name))}";
				await content.WebView2.EnsureCoreWebView2Async();
				content.WebView2.CoreWebView2.AddWebResourceRequestedFilter(uri, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
				content.WebView2.CoreWebView2.WebResourceRequested += async (sender, args) =>
				{
					if (args.Request.Uri.ToUpper() != uri.ToUpper()) return;
					var deferral = args.GetDeferral();
					var ext = Path.GetExtension(file.Name);
					var mimetype = MimeTypes.MimeTypeMap.GetMimeType(ext);
					var header = new System.Text.StringBuilder();
					if (!string.IsNullOrEmpty(mimetype) && string.IsNullOrEmpty(ext)) header.Append($"Content-Type: {mimetype}");
					args.Response = sender.Environment.CreateWebResourceResponse(await file.OpenReadAsync(), 200, "OK", header.ToString());
					deferral.Complete();
					return;
				};
				vm.SourceString = uri;
				vm.HomePage = uri;
				vm.ControllerCollapsed = true;
			}
			{
				var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));
				//ID can not be saved properly here. It cause thumbnail problem in the history page of Filer.
				//var token = HistoryManager.AddEntry(file);
				if (tagInfo is not null) tagInfo.SessionInfo = new Storages.WindowStates.WindowStateWindowViewerTab() { Token = token, ViewerKey = "Browser", Path = file.Path };
			}
			//HistoryManager.AddEntry(file);
		}

		public static async Task OpenSharpCompress(Frame frame, SharpCompress.Archives.IArchive archive, FrameworkElement sender, string homePath, TabPage tabPage = null)
		{
			if (archive is null) return;
			if (sender is null) return;
			SetTitle(sender, "");
			tabPage ??= GetCurrentTabPage(sender);

			frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

			var resolver = new GeneralResolverSharpCompress(archive);

			if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
			if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
			if (tabPage is null) return;

			{
				content.WebView2.CoreWebView2Initialized += (s, e) =>
				{
					OpenBrowser2_UpdateCoreStuffs(content.WebView2.CoreWebView2, async (a) =>
					{
						await tabPage.OpenTabWebPreferedBrowser(a);
					}, t => SetTitle(sender, t));
				};

				OpenBrowser_BookmarkSetViewModel(vm, () => GetCurrentTabPage(sender));
				var uri = resolver.GetUri($"/{homePath}");
				await content.WebView2.EnsureCoreWebView2Async();
				//https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.addwebresourcerequestedfilter?view=webview2-dotnet-1.0.1293.44
				content.WebView2.CoreWebView2.AddWebResourceRequestedFilter($"*://{resolver.Host}/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
				content.WebView2.CoreWebView2.WebResourceRequested += resolver.WebResourceRequested;
				vm.SourceString = uri;
				vm.HomePage = uri;
				vm.ControllerCollapsed = true;
			}

			{
				//This tab will not be restored.
				//var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));
				//if (tagInfo is not null) tagInfo.SessionInfo = new Storages.WindowStates.WindowStateWindowViewerTab() { Token = token, ViewerKey = "Browser", Path = file.Path };
			}
			//HistoryManager.AddEntry(file);
		}

		private static bool OpenEpub_CurrentDarkMode() => (bool)SettingStorage.GetValue("EpubViewerDarkMode") && Managers.ThemeManager.IsDarkTheme;

		public static async Task OpenEpub2(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, TabPage tabPage = null, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
		{
			if (file is null) return;
			if (sender is null) return;
			SetTitleByResource(sender, "Epub");
			tabPage ??= GetCurrentTabPage(sender);

			epubType ??= SettingStorage.GetValue("EpubViewerType") as SettingStorage.SettingEnums.EpubViewerType?;
			EpubResolverBase resolver = epubType switch
			{
				//SettingStorage.SettingEnums.EpubViewerType.Bibi => EpubResolverFile.GetResolverBibi(file),
				SettingStorage.SettingEnums.EpubViewerType.Bibi => await EpubResolverBase.GetResolverBibiZip(file),
				SettingStorage.SettingEnums.EpubViewerType.EpubJsReader => EpubResolverBase.GetResolverBasicFile(file),
				_ => await EpubResolverBase.GetResolverBibiZip(file),
			};
			frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

			if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
			if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
			if (tabPage is null) return;

			OpenBrowser_BookmarkSetViewModel(vm, () => GetCurrentTabPage(sender));

			content.WebView2.CoreWebView2Initialized += (s, e) =>
			{
				OpenBrowser2_UpdateCoreStuffs(content.WebView2.CoreWebView2, async (a) =>
				{
					await tabPage.OpenTabWebPreferedBrowser(a);
				}, t => SetTitle(sender, t));
			};

			var uri = resolver.GetUri(resolver.PathHome);
			await content.WebView2.EnsureCoreWebView2Async();
			//https://github.com/MicrosoftEdge/WebView2Feedback/issues/372
			//https://web.biz-prog.net/praxis/webview/response.html
			content.WebView2.CoreWebView2.AddWebResourceRequestedFilter($"*://{resolver.Host}/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
			content.WebView2.CoreWebView2.WebResourceRequested += resolver.WebResourceRequested;
			//content.WebView2.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
			vm.SourceString = uri;
			vm.HomePage = uri;

			vm.ControllerCollapsed = true;
			{
				{
					//普通ブラウザでもダークモード対応するのも選択肢。でもbackgroundとか修正しないといけないし、とりあえずなし。
					var defaultDark = OpenEpub_CurrentDarkMode();
					var checkbox = new CheckBox() { Content = ResourceManager.Loader.GetString("Extension/DarkMode/Title"), IsChecked = defaultDark, HorizontalAlignment = HorizontalAlignment.Stretch };

					async Task applyDarkMode()
					{
						if (checkbox.IsChecked ?? false)
						{
							//if (epubType == SettingStorage.SettingEnums.EpubViewerType.Bibi)
							await content.WebView2.CoreWebView2.ExecuteScriptAsync(@"if(document.body.style.background===""""){document.body.style.background='white';}");
							await content.WebView2.CoreWebView2.ExecuteScriptAsync(@"document.body.style.filter='invert(100%) hue-rotate(180deg)';");
						}
						else
						{
							//if (epubType == SettingStorage.SettingEnums.EpubViewerType.Bibi) 
							await content.WebView2.CoreWebView2.ExecuteScriptAsync(@"if(document.body.style.background===""white""){document.body.style.background='';}");
							await content.WebView2.CoreWebView2.ExecuteScriptAsync(@"document.body.style.filter='none';");
						}
					}

					checkbox.Checked += async (s, e) => { await applyDarkMode(); };
					checkbox.Unchecked += async (s, e) => { await applyDarkMode(); };
					content.WebView2.NavigationCompleted += async (s, e) => { await applyDarkMode(); };
					content.AddOnSpace.Add(checkbox);
				}
				{
					content.AddOnSpace.Add(OpenEpub_GetDarkmodeCheckBox());
				}
				{
					content.AddOnSpace.Add(new NavigationViewItemSeparator());
					content.AddOnSpace.Add(new Views.BrowserAddOn.CaptureControl()
					{
						WriteToStreamAction = async (s) =>
						{
							if (s is null) return;
							try
							{
								await content.WebView2.EnsureCoreWebView2Async();
								await content.WebView2.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, s);
							}
							catch { }
						},
						XamlRootProvider = () => content.XamlRoot,
					});
				}
			}
			{
				var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));
				var token = HistoryManager.AddEntry(file);
				if (tagInfo is not null) tagInfo.SessionInfo = new Storages.WindowStates.WindowStateWindowViewerTab() { Token = token, Path = file.Path };
			}
		}

		public static async Task OpenEpubPreferedEngine(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, TabPage tabPage = null, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
		{
			if ((bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserUseWebView2))
			{
				try
				{
					//https://github.com/MicrosoftEdge/WebView2Feedback/issues/2545
					var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
					if (string.IsNullOrEmpty(version)) throw new Exception();
					await OpenEpub2(frame, file, sender, tabPage, epubType);
					return;
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					await OpenEpub(frame, file, sender, tabPage, epubType);
					return;
				}
				catch { }
			}
		}

		public static async Task OpenEpub(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, TabPage tabPage = null, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
		{
			if (file is null) return;
			if (sender is null) return;
			SetTitleByResource(sender, "Epub");
			tabPage ??= GetCurrentTabPage(sender);

			epubType ??= SettingStorage.GetValue("EpubViewerType") as SettingStorage.SettingEnums.EpubViewerType?;
			EpubResolverBase resolver = epubType switch
			{
				//SettingStorage.SettingEnums.EpubViewerType.Bibi => EpubResolverFile.GetResolverBibi(file),
				SettingStorage.SettingEnums.EpubViewerType.Bibi => await EpubResolverBase.GetResolverBibiZip(file),
				SettingStorage.SettingEnums.EpubViewerType.EpubJsReader => EpubResolverBase.GetResolverBasicFile(file),
				_ => await EpubResolverBase.GetResolverBibiZip(file),
			};
			frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl), null);

			if (frame.Content is kurema.BrowserControl.Views.BrowserControl content)
			{
				OpenBrowser_BookmarkSetViewModel(content?.DataContext as kurema.BrowserControl.ViewModels.BrowserControlViewModel, () => GetCurrentTabPage(sender));

				Uri uri = content.Control.BuildLocalStreamUri("epub", resolver.PathHome);
				content.Control.NavigateToLocalStreamUri(uri, resolver);
				if (tabPage is not null)
				{
					content.Control.NewWindowRequested += async (s, e) =>
					{
						await tabPage.OpenTabWebPreferedBrowser(e.Uri.ToString());
						e.Handled = true;
					};
				}
				if (content.DataContext is kurema.BrowserControl.ViewModels.BrowserControlViewModel vm)
				{
					vm.ControllerCollapsed = true;
					if (SettingStorage.GetValue("WebHomePage") is string homepage)
					{
						vm.HomePage = homepage;
					}
					//vm.HomePage = uri.ToString();
				}
				{
					{
						//普通ブラウザでもダークモード対応するのも選択肢。でもbackgroundとか修正しないといけないし、とりあえずなし。
						var defaultDark = OpenEpub_CurrentDarkMode();
						var checkbox = new CheckBox() { Content = ResourceManager.Loader.GetString("Extension/DarkMode/Title"), IsChecked = defaultDark, HorizontalAlignment = HorizontalAlignment.Stretch };

						async Task applyDarkMode()
						{
							if (checkbox.IsChecked ?? false)
							{
								//if (epubType == SettingStorage.SettingEnums.EpubViewerType.Bibi)
								await content.Control.InvokeScriptAsync("eval", new[] { @"if(document.body.style.background===""""){document.body.style.background='white';}" });
								await content.Control.InvokeScriptAsync("eval", new[] { @"document.body.style.filter='invert(100%) hue-rotate(180deg)';" });
							}
							else
							{
								//if (epubType == SettingStorage.SettingEnums.EpubViewerType.Bibi) 
								await content.Control.InvokeScriptAsync("eval", new[] { @"if(document.body.style.background===""white""){document.body.style.background='';}" });
								await content.Control.InvokeScriptAsync("eval", new[] { @"document.body.style.filter='none';" });
							}
						}

						checkbox.Checked += async (s, e) => { await applyDarkMode(); };
						checkbox.Unchecked += async (s, e) => { await applyDarkMode(); };
						content.Control.NavigationCompleted += async (s, e) => { await applyDarkMode(); };
						content.AddOnSpace.Add(checkbox);
					}
					{
						content.AddOnSpace.Add(OpenEpub_GetDarkmodeCheckBox());
					}
					{
						content.AddOnSpace.Add(new NavigationViewItemSeparator());
						content.AddOnSpace.Add(new Views.BrowserAddOn.CaptureControl()
						{
							WriteToStreamAction = async (s) =>
							{
								if (s is null) return;
								try
								{
									if (content.Control.IsLoaded) await content.Control.CapturePreviewToStreamAsync(s);
								}
								catch { }
							},
							XamlRootProvider = () => content.XamlRoot,
						});
					}
				}

				//content.Control.ScriptNotify += (s, e) =>
				//{
				//    //window.external.notify(document.body.style.background);
				//    var v = e.Value;
				//};
			}
			{
				var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));
				var token = HistoryManager.AddEntry(file);
				if (tagInfo is not null) tagInfo.SessionInfo = new Storages.WindowStates.WindowStateWindowViewerTab() { Token = token, Path = file.Path };
			}
		}

		private static CheckBox OpenEpub_GetDarkmodeCheckBox()
		{
			var checkbox = new CheckBox()
			{
				Content = ResourceManager.Loader.GetString("Setting_EpubViewerDarkMode/Title"),
				IsChecked = (bool)SettingStorage.GetValue("EpubViewerDarkMode"),
				HorizontalAlignment = HorizontalAlignment.Stretch,
			};
			ToolTipService.SetToolTip(checkbox, ResourceManager.Loader.GetString("Setting_EpubViewerDarkMode/Description"));
			checkbox.Checked += (s, e) => SettingStorage.SetValue("EpubViewerDarkMode", true);
			checkbox.Unchecked += (s, e) => SettingStorage.SetValue("EpubViewerDarkMode", false);
			return checkbox;
		}

		public static async Task<bool> OpenBookPicked(Func<(Frame, FrameworkElement)> frameProvider, TabPage tabPage = null, Action handleOtherFileAction = null)
		{
			var file = await BookManager.PickFile();
			if (file is null) return false;
			await OpenBook(file, frameProvider, handleOtherFileAction, tabPage);
			return true;
		}

		public async static Task OpenBook(Windows.Storage.IStorageFile file, Func<(Frame, FrameworkElement)> frameProvider, Action handleOtherFileAction = null, TabPage tabPage = null, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
		{
			if (file is null) return;
			if (frameProvider is null) return;

			var type = await BookManager.GetBookTypeByStorageFile(file);

			if (type == BookManager.BookType.Epub)
			{
				var (frame, sender) = frameProvider();
				await OpenEpubPreferedEngine(frame, file, sender, tabPage, epubType);
			}
			//else if (type is BookManager.BookType.Pdf)
			//{
			//    var (frame, sender) = frameProvider();
			//    OpenSingleFile(frame, file, sender);
			//}
			else if (type is not null)
			{
				var (frame, _) = frameProvider();
				frame.Navigate(typeof(BookFixed3Viewer), file);
			}
			else
			{
				handleOtherFileAction?.Invoke();
			}
		}

		public static async Task OpenExplorer(Frame frame, FrameworkElement sender = null)
		{
			{
				var tagInfo = TabViewItemTagInfo.SetOrGetTag(GetCurrentTabViewItem(sender));

				//Too long! Split!
				SetTitleByResource(sender, "Explorer");
				frame.Navigate(typeof(kurema.FileExplorerControl.Views.FileExplorerControl), null);
				if (frame.Content is kurema.FileExplorerControl.Views.FileExplorerControl control)
				{
					if (control.DataContext is kurema.FileExplorerControl.ViewModels.FileExplorerViewModel fvm && fvm.Content != null)
					{
						control.AddressRequesteCommand = new DelegateCommand(async (address) =>
						{
							if (string.IsNullOrWhiteSpace(address?.ToString())) return;
							var tab = GetCurrentTabPage(control);
							if (Uri.TryCreate(address?.ToString() ?? "", UriKind.Absolute, out Uri uriResult))
							{
								if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) await tab.OpenTabWebPreferedBrowser(address?.ToString());
								if (uriResult.IsFile)
								{
									var result = await GetFileItemViewModelFromRoot(address.ToString(), control.GetTreeViewRoot());
									if (result != null)
									{
										fvm.Content.Item = result;
									}
								}
							}
						}, address =>
						{
							if (string.IsNullOrWhiteSpace(address?.ToString())) return false;
							var tab = GetCurrentTabPage(control);
							if (Uri.TryCreate(address?.ToString() ?? "", UriKind.Absolute, out Uri uriResult))
							{
								if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) return true;
								if (uriResult.IsFile)
								{
									var folder = control.GetTreeViewRoot()?.FirstOrDefault(a => a.Content.Tag is LibraryStorage.LibraryKind kind && kind == LibraryStorage.LibraryKind.Folders);
									if (folder is null) return false;
									return folder.Children?.Any(item => Functions.IsAncestorOf(item.Path, address.ToString())) ?? true;
								}
								return false;
							}
							else return false;

						}
						);

						//control.MenuChildrens.Add(new ExplorerMenuControl() { OriginPage = content });

						var library = LibraryStorage.GetItem(async (a, b) =>
						{
							var tab = GetCurrentTabPage(control);
							if (tab is null) return;
							switch (b)
							{
								default:
								case LibraryStorage.BookmarkActionType.Auto:
									await tab.OpenTabWebPreferedBrowser(a);
									break;
								case LibraryStorage.BookmarkActionType.Internal:
									tab.OpenTabWeb(a);
									break;
								case LibraryStorage.BookmarkActionType.External:
									await OpenWebExternal(a);
									break;
							}
						}, control.AddressRequesteCommand, () => { return GetCurrentTabPage(control); }
						);

						//var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(folder));
						var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(library);
						fv.IconProviders.Add(new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async (a, cancel) =>
						{
							if (a is kurema.FileExplorerControl.Models.FileItems.StorageBookmarkItem bookmark)
							{
								return IconProviderHelper.BookmarkIconsExplorer();
							}
							if (BookManager.AvailableExtensionsArchive.Contains(Path.GetExtension(a.Name).ToLowerInvariant()))
							{
								return await IconProviderHelper.BookIconsExplorer(a, cancel, sender.Dispatcher);
							}
							else { return (null, null); }
						}));
						await fv.UpdateChildren();
						control.SetTreeViewItem(fv.Folders);
						await control.ContentControl.SetFolder(fv);
						control.ContentControl.FileOpenedEventHandler += async (s2, e2) =>
						{
							e2?.Content?.Open();
							var fileitem = e2?.Content;
							var tab = GetCurrentTabPage(control);
							if (tab is null) return;
							if (BookManager.AvailableExtensionsArchive.Contains(Path.GetExtension(fileitem?.Name ?? "").ToLowerInvariant()))
							{

								if (fileitem is kurema.FileExplorerControl.Models.FileItems.StorageFileItem sfi)
								{
									tab.OpenTabBook(sfi.Content);
								}
								else if (fileitem is kurema.FileExplorerControl.Models.FileItems.HistoryMRUItem hm)
								{
									var file = await hm.GetFile();
									if (file != null) tab.OpenTabBook(file);
								}
								//else if (fileitem is kurema.FileExplorerControl.Models.FileItems.HistoryItem hi)
								//{
								//    var file = await hi.GetFile();
								//    if (file != null) tab.OpenTabBook(file);
								//}
								else
								{
									var stream = fileitem?.OpenStreamForReadAsync();
									if (stream != null)
										tab.OpenTabBook(stream);
								}
								return;

							}

							//var codecQuery = new Windows.Media.Core.CodecQuery();
							//var queryResults= await codecQuery.FindAllAsync(Windows.Media.Core.CodecKind.Video,Windows.Media.Core.CodecCategory.Decoder,"");
							//foreach(var item in queryResults)
							//{
							//    System.Diagnostics.Debug.WriteLine(item.DisplayName);
							//    foreach (var item2 in item.Subtypes)
							//    {
							//        System.Diagnostics.Debug.WriteLine(item2);
							//    }
							//}
							if ((await kurema.FileExplorerControl.Views.Viewers.SimpleMediaPlayerPage.GetAvailableExtensionsAsync()).Contains(Path.GetExtension(fileitem?.Name ?? "").ToUpperInvariant()))
							{
								tab.OpenTabMedia(fileitem);
								return;
							}
							if ((fileitem as kurema.FileExplorerControl.Models.FileItems.StorageFileItem)?.Content is Windows.Storage.IStorageFile isf)
							{
								//Should I warn before opening? And I think it's annoying to open random file by mistake.
								await Windows.System.Launcher.LaunchFileAsync(isf);
								return;
							}
						};
						if (tagInfo is not null)
						{
							control.ContentControl.FolderChangedHandler += (s2, e2) =>
							{
								var t = (tagInfo.SessionInfo as Storages.WindowStates.WindowStateWindowExplorerTab) ?? new();
								t.Path = e2.Item.Path;
								var structure =
									kurema.FileExplorerControl.ViewModels.FileItemViewModel.GetStructure(e2.Item)
									.Select(a => new Storages.WindowStates.WindowStateWindowExplorerTabItem() { Path = a.Path, DisplayedName = a.Title }).ToArray();
								t.Structure = structure;
								tagInfo.SessionInfo = t;
							};
						}

						void Content_PropertyChanged(object _, System.ComponentModel.PropertyChangedEventArgs e)
						{
							switch (e.PropertyName)
							{
								case nameof(fvm.Content.ContentStyle):
									SettingStorage.SetValue("ExplorerContentStyle", fvm.Content.ContentStyle);
									break;
								case nameof(fvm.Content.IconSize):
									SettingStorage.SetValue("ExplorerIconSize", fvm.Content.IconSize);
									break;
							}
						}

						fvm.Content.PropertyChanged += Content_PropertyChanged;
						fvm.PropertyChanged += (s, e) =>
						{
							if (e.PropertyName == nameof(fvm.Content))
							{
								fvm.Content.PropertyChanged += Content_PropertyChanged;
							}
						};

						if (SettingStorage.GetValue("ExplorerContentStyle") is kurema.FileExplorerControl.ViewModels.ContentViewModel.ContentStyles f)
						{
							fvm.Content.ContentStyle = f;
						}
						if (SettingStorage.GetValue("ExplorerIconSize") is double d)
						{
							fvm.Content.IconSize = d;
						}
					}
				}
			}
		}

		private static void OpenBrowser_BookmarkSetViewModel(kurema.BrowserControl.ViewModels.IBrowserControlViewModel viewModel, Func<Views.TabPage> tabPageProvider)
		{
			if (viewModel is null) return;
			var bookmark = LibraryStorage.GetItemBookmarks((_, _) => { }, tabPageProvider);
			viewModel.BookmarkRoot = new kurema.BrowserControl.ViewModels.BookmarkItem("", (bmNew) =>
			{
				LibraryStorage.OperateBookmark(a =>
				{
					a?.Add(new Storages.Library.bookmarksContainerBookmark() { created = DateTime.Now, title = bmNew.Title, url = bmNew.Address });
					return Task.CompletedTask;
				});
			}, async () =>
			{
				return (await bookmark.GetChildren())?.Select(a =>
				{
					if (a is kurema.FileExplorerControl.Models.FileItems.IStorageBookmark bm) { return bm.GetBrowserBookmarkItem(); }
					else if (a is kurema.FileExplorerControl.Models.FileItems.ContainerItem container)
					{
						return new kurema.BrowserControl.ViewModels.BookmarkItem(container.Name, (_) => { }
						, () => Task.FromResult(container.Children.Select(b => (b as kurema.FileExplorerControl.Models.FileItems.IStorageBookmark)?.GetBrowserBookmarkItem())))
						{ IsReadOnly = true };
					}
					else return null;
				})?.Where(a => a != null);
			});
		}

		public static void OpenBrowser2_UpdateCoreStuffs(Microsoft.Web.WebView2.Core.CoreWebView2 core, Action<string> OpenTabWeb, Action<string> UpdateTitle)
		{
			core.NewWindowRequested += (s, e) =>
			{
				OpenTabWeb?.Invoke(e.Uri.ToString());
				e.Handled = true;
			};
			core.DocumentTitleChanged += (s, e) =>
			{
				UpdateTitle(core.DocumentTitle);
			};
			core.DownloadStarting += (s, e) =>
			{
				e.Handled = false;
			};
			core.Profile.PreferredColorScheme = ThemeManager.AsElementTheme switch
			{
				ElementTheme.Light => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light,
				ElementTheme.Dark => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark,
				ElementTheme.Default or _ => Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Auto,
			};
		}

		public static async Task OpenBrowser2(Frame frame, string uri, Action<string> OpenTabWeb, Action<string> UpdateTitle, Func<Views.TabPage> tabPageProvider, Microsoft.UI.Xaml.Controls.TabViewItem tabViewItem)
		{
			frame?.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), uri);

			if ((frame.Content as kurema.BrowserControl.Views.BrowserControl2)?.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
			if (frame?.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;

			{
				OpenBrowser_BookmarkSetViewModel(vm, tabPageProvider);

				vm.OpenDownloadDirectoryCommand = new DelegateCommand(async (_) =>
				{
					var folder = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Download", Windows.Storage.CreationCollisionOption.OpenIfExists);
					await Windows.System.Launcher.LaunchFolderAsync(folder);
				});

				if (SettingStorage.GetValue("WebHomePage") is string homepage)
				{
					vm.HomePage = homepage;
				}
				if (SettingStorage.GetValue("WebSearchEngine") is string searchEngine)
				{
					vm.SearchEngine = searchEngine;
				}
			}

			{
				//await content.WebView2.EnsureCoreWebView2Async();

				void updateUserAgent()
				{
					var ua = (string)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserUserAgent);
					if (!string.IsNullOrWhiteSpace(ua))
					{
						content.WebView2.CoreWebView2.Settings.UserAgent = ua;
					}
					else if (content.UserAgentOriginal is not null)
					{
						content.WebView2.CoreWebView2.Settings.UserAgent = content.UserAgentOriginal;
					}
				}

				{
					var addonUA = new Views.BrowserAddOn.UserAgentOverride();
					addonUA.UserAgentUpdated += (_, _) => { updateUserAgent(); content?.WebView2?.Reload(); };
					content.AddOnSpace.Add(addonUA);
					content.AddOnSpace.Add(new NavigationViewItemSeparator());
					content.AddOnSpace.Add(new Views.BrowserAddOn.CaptureControl()
					{
						WriteToStreamAction = async (s) =>
						{
							if (s is null) return;
							try
							{
								await content.WebView2.EnsureCoreWebView2Async();
								await content.WebView2.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, s);
							}
							catch { }
						},
						XamlRootProvider = () => content.XamlRoot,
					});
					content.AddOnSpace.Add(new NavigationViewItemSeparator());
					try
					{
						await ExtensionAdBlockerManager.LoadRules();
						var adbc = new Views.BrowserAddOn.AdBlockerControl();
						adbc.SetBinding(Views.BrowserAddOn.AdBlockerControl.UrlProperty, new Windows.UI.Xaml.Data.Binding()
						{
							Source = content,
							Path = new PropertyPath("DataContext.Source"),
							Mode = Windows.UI.Xaml.Data.BindingMode.OneWay,
						});
						adbc.RefreshCommand = new DelegateCommand(a =>
						{
							content.WebView2.Reload();
						});
						content.AddOnSpace.Add(adbc);
					}
					catch { }
					{
						void WebView2InitalizedOperation()
						{
							content.UserAgentOriginal ??= content.WebView2.CoreWebView2.Settings.UserAgent;
							OpenBrowser2_UpdateCoreStuffs(content.WebView2.CoreWebView2, OpenTabWeb, UpdateTitle);
							content.WebView2.CoreWebView2.NavigationCompleted += (s, e) => UpdateBrowserSession(tabViewItem, s.Source);
							content.WebView2.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
							content.WebView2.CoreWebView2.WebResourceRequested += ExtensionAdBlockerManager.WebView2WebResourceRequested;
							updateUserAgent();
						}

						await content.WebView2.EnsureCoreWebView2Async();
						WebView2InitalizedOperation();
						content.WebView2.CoreWebView2Initialized += (_, _) => WebView2InitalizedOperation();
					}

					var bookmarksLocal = (await LibraryStorage.LocalBookmarks.GetContentAsync())?.GetBookmarksForCulture(System.Globalization.CultureInfo.CurrentCulture);
					vm.SearchEngines = bookmarksLocal?.GetSearchEngineEntries().ToArray();
				}
			}
		}

		static void UpdateBrowserSession(Microsoft.UI.Xaml.Controls.TabViewItem tabView, string url)
		{
			var tag = TabViewItemTagInfo.SetOrGetTag(tabView);
			if (tag is null) return;
			var browserTab = (tag.SessionInfo as Storages.WindowStates.WindowStateWindowBrowserTab) ?? new Storages.WindowStates.WindowStateWindowBrowserTab();
			browserTab.Url = url;
			tag.SessionInfo = browserTab;
		}

		public static async Task OpenBrowser(Frame frame, string uri, Action<string> OpenTabWeb, Action<Windows.Storage.IStorageItem> OpenTabBook, Action<string> UpdateTitle, Func<Views.TabPage> tabPageProvider, Microsoft.UI.Xaml.Controls.TabViewItem tabViewItem)
		{
			frame?.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl), uri);
			if (frame?.Content is kurema.BrowserControl.Views.BrowserControl content)
			{
				content.Control.NewWindowRequested += (s, e) =>
				{
					OpenTabWeb?.Invoke(e.Uri.ToString());
					e.Handled = true;
				};
				content.DownloadedFileOpenedEventHandler += (s, e) =>
				  {
					  OpenTabBook?.Invoke(e);
				  };

				{
					content.AddOnSpace.Add(new Views.BrowserAddOn.CaptureControl()
					{
						WriteToStreamAction = async (s) =>
						{
							if (s is null) return;
							try
							{
								if (content.Control.IsLoaded) await content.Control.CapturePreviewToStreamAsync(s);
							}
							catch { }
						},
						XamlRootProvider = () => content.XamlRoot,
					});
				}

				// Uncomment following to enable AdBlocker. But it's using Wait() which may affect performance.
				{
					try
					{
						_ = ExtensionAdBlockerManager.LoadRules();
						var adbc = new Views.BrowserAddOn.AdBlockerControl();
						adbc.SetBinding(Views.BrowserAddOn.AdBlockerControl.UrlProperty, new Windows.UI.Xaml.Data.Binding()
						{
							Source = content,
							Path = new PropertyPath("DataContext.Uri"),
							Mode = Windows.UI.Xaml.Data.BindingMode.OneWay,
						});
						adbc.RefreshCommand = new DelegateCommand(a =>
						{
							content.Control.Refresh();
						});
						content.AddOnSpace.Add(new NavigationViewItemSeparator());
						content.AddOnSpace.Add(adbc);
					}
					catch { }

					content.Control.WebResourceRequested += ExtensionAdBlockerManager.WebViewWebResourceRequested;
					content.Control.NavigationCompleted += (s, e) => { try { if (s.Source is not null) UpdateBrowserSession(tabViewItem, s.Source.ToString()); } catch { } };
				}
			}

			if ((frame.Content as kurema.BrowserControl.Views.BrowserControl)?.DataContext is kurema.BrowserControl.ViewModels.BrowserControlViewModel vm && vm != null)
			{
				OpenBrowser_BookmarkSetViewModel(vm, tabPageProvider);

				vm.OpenDownloadDirectoryCommand = new DelegateCommand(async (_) =>
				{
					var folder = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Download", Windows.Storage.CreationCollisionOption.OpenIfExists);
					await Windows.System.Launcher.LaunchFolderAsync(folder);
				});

				if (SettingStorage.GetValue("WebHomePage") is string homepage)
				{
					vm.HomePage = homepage;
				}
				if (SettingStorage.GetValue("WebSearchEngine") is string searchEngine)
				{
					vm.SearchEngine = searchEngine;
				}

				vm.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == nameof(kurema.BrowserControl.ViewModels.BrowserControlViewModel.Title))
					{
						UpdateTitle(vm.Title);
					}
				};

				var bookmarksLocal = (await LibraryStorage.LocalBookmarks.GetContentAsync())?.GetBookmarksForCulture(System.Globalization.CultureInfo.CurrentCulture);
				vm.SearchEngines = bookmarksLocal?.GetSearchEngineEntries().ToArray();
			}
		}
	}
}
