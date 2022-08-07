using System;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.Networking.BackgroundTransfer;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using BookViewerApp.Views;

using System.Threading.Tasks;

namespace BookViewerApp.Helper;

public static partial class UIHelper
{
    public static class FrameOperation
    {
        public static async void OpenPdfJs(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender)
        {
            if (file is null) return;
            if (sender is null) return;
            SetTitle(sender, Path.GetFileNameWithoutExtension(file.Name));
            var tabPage = GetCurrentTabPage(sender);

            EpubResolverBase resolver = await EpubResolverBase.GetResolverPdfJs(file);
            frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

            if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
            if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
            if (tabPage is null) return;

            {
                content.WebView2.CoreWebView2Initialized += (s, e) =>
                {
                    OpenBrowser2_UpdateCoreEvents(content.WebView2.CoreWebView2, async (a) =>
                    {
                        await tabPage.OpenTabWebPreferedBrowser(a);
                    }, t => SetTitle(sender, t));
                };

                OpenBrowser_BookmarkSetViewModel(vm);

                var uri = resolver.GetUri(resolver.PathHome);
                await content.WebView2.EnsureCoreWebView2Async();
                content.WebView2.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
                content.WebView2.CoreWebView2.WebResourceRequested += resolver.WebResourceRequested;
                vm.SourceString = uri;
                vm.ControllerCollapsed = true;
            }
            //ID can not be saved properly here. It cause thumbnail problem in the history page of Filer.
            //HistoryManager.AddEntry(file);
        }

        public static async void OpenSingleFile(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender)
        {
            if (file is null) return;
            if (sender is null) return;
            SetTitle(sender, Path.GetFileNameWithoutExtension(file.Name));
            var tabPage = GetCurrentTabPage(sender);

            frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), null);

            if (frame.Content is not kurema.BrowserControl.Views.BrowserControl2 content) return;
            if (content.DataContext is not kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm) return;
            if (tabPage is null) return;

            {
                content.WebView2.CoreWebView2Initialized += (s, e) =>
                {
                    OpenBrowser2_UpdateCoreEvents(content.WebView2.CoreWebView2, async (a) =>
                    {
                        await tabPage.OpenTabWebPreferedBrowser(a);
                    }, t => SetTitle(sender, t));
                };

                OpenBrowser_BookmarkSetViewModel(vm);

                var uri = $"https://file.example/{System.Web.HttpUtility.UrlEncode(System.IO.Path.GetFileName(file.Name))}";
                await content.WebView2.EnsureCoreWebView2Async();
                content.WebView2.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
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
                vm.ControllerCollapsed = true;
            }
            //HistoryManager.AddEntry(file);
        }

        public static async void OpenEpub2(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
        {
            if (file is null) return;
            if (sender is null) return;
            SetTitleByResource(sender, "Epub");
            var tabPage = GetCurrentTabPage(sender);

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

            OpenBrowser_BookmarkSetViewModel(vm);

            content.WebView2.CoreWebView2Initialized += (s, e) =>
            {
                OpenBrowser2_UpdateCoreEvents(content.WebView2.CoreWebView2, async (a) =>
                {
                    await tabPage.OpenTabWebPreferedBrowser(a);
                }, t => SetTitle(sender, t));
            };

            var uri = resolver.GetUri(resolver.PathHome);
            await content.WebView2.EnsureCoreWebView2Async();
            //https://github.com/MicrosoftEdge/WebView2Feedback/issues/372
            //https://web.biz-prog.net/praxis/webview/response.html
            content.WebView2.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
            content.WebView2.CoreWebView2.WebResourceRequested += resolver.WebResourceRequested;
            //content.WebView2.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            vm.SourceString = uri;

            vm.ControllerCollapsed = true;
            {
                {
                    //普通ブラウザでもダークモード対応するのも選択肢。でもbackgroundとか修正しないといけないし、とりあえずなし。
                    var defaultDark = (bool)SettingStorage.GetValue("EpubViewerDarkMode") && Application.Current.RequestedTheme == ApplicationTheme.Dark;
                    var checkbox = new CheckBox() { Content = ResourceManager.Loader.GetString("Browser/Addon/DarkMode"), IsChecked = defaultDark, HorizontalAlignment = HorizontalAlignment.Stretch };

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
            }
            HistoryManager.AddEntry(file);
        }

        public static void OpenEpubPreferedEngine(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
        {
            if ((bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserUseWebView2))
            {
                try
                {
                    //https://github.com/MicrosoftEdge/WebView2Feedback/issues/2545
                    var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                    if (string.IsNullOrEmpty(version)) throw new Exception();
                    OpenEpub2(frame, file, sender, epubType);
                    return;
                }
                catch
                {
                }
            }
            else
            {
                OpenEpub(frame, file, sender, epubType);
                return;
            }
        }

        public static async void OpenEpub(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
        {
            if (file is null) return;
            if (sender is null) return;
            SetTitleByResource(sender, "Epub");
            var tabPage = GetCurrentTabPage(sender);

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
                OpenBrowser_BookmarkSetViewModel(content?.DataContext as kurema.BrowserControl.ViewModels.BrowserControlViewModel);

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
                }
                {
                    {
                        //普通ブラウザでもダークモード対応するのも選択肢。でもbackgroundとか修正しないといけないし、とりあえずなし。
                        var defaultDark = (bool)SettingStorage.GetValue("EpubViewerDarkMode") && Application.Current.RequestedTheme == ApplicationTheme.Dark;
                        var checkbox = new CheckBox() { Content = ResourceManager.Loader.GetString("Browser/Addon/DarkMode"), IsChecked = defaultDark, HorizontalAlignment = HorizontalAlignment.Stretch };

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
                }

                //content.Control.ScriptNotify += (s, e) =>
                //{
                //    //window.external.notify(document.body.style.background);
                //    var v = e.Value;
                //};
            }
            HistoryManager.AddEntry(file);
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

        public static async Task<bool> OpenBookPicked(Func<(Frame, FrameworkElement)> frameProvider, Action handleOtherFileAction = null)
        {
            var file = await BookManager.PickFile();
            if (file is null) return false;
            OpenBook(file, frameProvider, handleOtherFileAction);
            return true;
        }

        public async static void OpenBook(Windows.Storage.IStorageFile file, Func<(Frame, FrameworkElement)> frameProvider, Action handleOtherFileAction = null, SettingStorage.SettingEnums.EpubViewerType? epubType = null)
        {
            if (file is null) return;
            if (frameProvider is null) return;

            var type = await BookManager.GetBookTypeByStorageFile(file);

            if (type == BookManager.BookType.Epub)
            {
                var (frame, sender) = frameProvider();
                OpenEpubPreferedEngine(frame, file, sender, epubType);
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

        public static async void OpenExplorer(Frame frame, FrameworkElement sender = null)
        {
            {
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
                            if (BookManager.AvailableExtensionsArchive.Contains(Path.GetExtension(fileitem?.Name ?? "").ToLowerInvariant()))
                            {
                                var tab = GetCurrentTabPage(control);
                                if (tab != null)
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
                                }
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
                                var tab = GetCurrentTabPage(control);
                                if (tab != null)
                                {
                                    tab.OpenTabMedia(fileitem);
                                }
                            }
                        };

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

        private static void OpenBrowser_BookmarkSetViewModel(kurema.BrowserControl.ViewModels.IBrowserControlViewModel viewModel)
        {
            if (viewModel is null) return;
            var bookmark = LibraryStorage.GetItemBookmarks((_, _2) => { });
            viewModel.BookmarkRoot = new kurema.BrowserControl.ViewModels.BookmarkItem("", (bmNew) =>
            {
                LibraryStorage.OperateBookmark(a =>
                {
                    a?.Add(new Storages.Library.libraryBookmarksContainerBookmark() { created = DateTime.Now, title = bmNew.Title, url = bmNew.Address });
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

        public static void OpenBrowser2_UpdateCoreEvents(Microsoft.Web.WebView2.Core.CoreWebView2 core, Action<string> OpenTabWeb, Action<string> UpdateTitle)
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
            };
        }

        public static void OpenBrowser2(Frame frame, string uri, Action<string> OpenTabWeb, Action<string> UpdateTitle)
        {
            frame?.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl2), uri);

            if ((frame.Content as kurema.BrowserControl.Views.BrowserControl2)?.DataContext is kurema.BrowserControl.ViewModels.BrowserControl2ViewModel vm && vm != null)
            {
                OpenBrowser_BookmarkSetViewModel(vm);

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

            if (frame?.Content is kurema.BrowserControl.Views.BrowserControl2 content)
            {
                //await content.WebView2.EnsureCoreWebView2Async();

                content.WebView2.CoreWebView2Initialized += (s, e) =>
                {
                    OpenBrowser2_UpdateCoreEvents(content.WebView2.CoreWebView2, OpenTabWeb, UpdateTitle);
                };
            }
        }

        public static void OpenBrowser(Frame frame, string uri, Action<string> OpenTabWeb, Action<Windows.Storage.IStorageItem> OpenTabBook, Action<string> UpdateTitle)
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
            }

            if ((frame.Content as kurema.BrowserControl.Views.BrowserControl)?.DataContext is kurema.BrowserControl.ViewModels.BrowserControlViewModel vm && vm != null)
            {
                OpenBrowser_BookmarkSetViewModel(vm);

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
            }
        }

    }
}
