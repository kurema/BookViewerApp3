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

namespace BookViewerApp.Helper
{
    public static partial class UIHelper
    {
        public static class FrameOperation
        {
            public static void OpenEpub(Frame frame, Windows.Storage.IStorageFile file, FrameworkElement sender)
            {
                if (file == null) return;
                if (sender == null) return;
                SetTitleByResource(sender, "Epub");
                var tabPage = GetCurrentTabPage(sender);


                var resolver = EpubResolver.GetResolverBibi(file);
                frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserControl), null);

                if (frame.Content is kurema.BrowserControl.Views.BrowserControl content)
                {
                    Uri uri = content.Control.BuildLocalStreamUri("epub", resolver.PathHome);
                    content.Control.NavigateToLocalStreamUri(uri, resolver);
                    if (tabPage != null)
                    {
                        content.Control.NewWindowRequested += (s, e) =>
                        {
                            tabPage.OpenTabWeb(e.Uri.ToString());
                            e.Handled = true;
                        };
                    }
                }
                HistoryManager.List.Add(file);
                //await HistoryStorage.AddHistory(file, null);
            }

            public static async Task<bool> OpenBookPicked(Func<(Frame, FrameworkElement)> frameProvider, Action handleOtherFileAction = null)
            {
                var file = await BookManager.PickFile();
                if (file == null) return false;
                OpenBook(file, frameProvider, handleOtherFileAction);
                return true;
            }

            public async static void OpenBook(Windows.Storage.IStorageFile file, Func<(Frame, FrameworkElement)> frameProvider, Action handleOtherFileAction = null)
            {
                if (file == null) return;
                if (frameProvider == null) return;

                var type = await BookManager.GetBookTypeByStorageFile(file);

                if (type == BookManager.BookType.Epub)
                {
                    var (frame, sender) = frameProvider();
                    OpenEpub(frame, file, sender);
                }
                else if (type != null)
                {
                    var (frame, sender) = frameProvider();
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
                    UIHelper.SetTitleByResource(sender, "Explorer");
                    frame.Navigate(typeof(kurema.FileExplorerControl.Views.FileExplorerControl), null);
                    if (frame.Content is kurema.FileExplorerControl.Views.FileExplorerControl control)
                    {
                        if (control.DataContext is kurema.FileExplorerControl.ViewModels.FileExplorerViewModel fvm && fvm.Content != null)
                        {
                            control.AddressRequesteCommand = new DelegateCommand(async (address) =>
                            {
                                if (string.IsNullOrWhiteSpace(address?.ToString())) return;
                                Uri uriResult;
                                var tab = GetCurrentTabPage(control);
                                if (Uri.TryCreate(address?.ToString() ?? "", UriKind.Absolute, out uriResult))
                                {
                                    if ((uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) tab.OpenTabWeb(address?.ToString());
                                    if (uriResult.IsFile)
                                    {
                                        var result = await GetFileItemViewModelFromRoot(address.ToString(), control.GetTreeViewRoot());
                                        if (result != null) fvm.Content.Item = result;
                                    }
                                }
                            }, address =>
                            {
                                if (string.IsNullOrWhiteSpace(address?.ToString())) return false;
                                Uri uriResult;
                                var tab = GetCurrentTabPage(control);
                                if (Uri.TryCreate(address?.ToString() ?? "", UriKind.Absolute, out uriResult))
                                {
                                    if ((uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) return true;
                                    if (uriResult.IsFile)
                                    {
                                        var folder = control.GetTreeViewRoot()?.FirstOrDefault(a => a.Content.Tag is Storages.LibraryStorage.LibraryKind kind && kind == Storages.LibraryStorage.LibraryKind.Folders);
                                        if (folder == null) return false;
                                        return folder.Children?.Any(item => Functions.IsAncestorOf(item.Path, address.ToString())) ?? true;
                                    }
                                    return false;
                                }
                                else return false;

                            }
                            );

                            //control.MenuChildrens.Add(new ExplorerMenuControl() { OriginPage = content });

                            var library = LibraryStorage.GetItem((a) =>
                            {
                                var tab = GetCurrentTabPage(control);
                                if (tab == null) return;
                                tab.OpenTabWeb(a);
                            }, control.AddressRequesteCommand
                            );

                            //var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(folder));
                            var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(library);
                            fv.IconProviders.Add(new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async (a) =>
                            {
                                if (a is kurema.FileExplorerControl.Models.FileItems.StorageBookmarkItem)
                                {
                                    return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_bookmark_s.png")),
                                    () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_bookmark_l.png"))
                                    );
                                }
                                if (BookManager.AvailableExtensionsArchive.Contains(System.IO.Path.GetExtension(a.Name).ToLower()))
                                {
                                    string id = (a as kurema.FileExplorerControl.Models.FileItems.HistoryItem)?.Content?.Id ?? PathStorage.GetIdFromPath(a.Path);
                                    if (!string.IsNullOrEmpty(id))
                                    {
                                        var image = await ThumbnailManager.GetImageSourceAsync(id);
                                        if (image != null)
                                        {
                                            return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_book_s.png")),
                                            () => image
                                            );
                                        }
                                    }

                                    return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_book_s.png")),
                                    () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_book_l.png"))
                                    );
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
                                if (!BookManager.AvailableExtensionsArchive.Contains(System.IO.Path.GetExtension(fileitem?.Name ?? "").ToLower()))
                                {
                                    return;
                                }

                                var tab = UIHelper.GetCurrentTabPage(control);
                                if (tab != null)
                                {
                                    if (fileitem is kurema.FileExplorerControl.Models.FileItems.StorageFileItem sfi)
                                    {
                                        tab.OpenTabBook(sfi.Content);
                                    }
                                    else if (fileitem is kurema.FileExplorerControl.Models.FileItems.HistoryItem hi)
                                    {
                                        var file = await hi.GetFile();
                                        if (file != null) tab.OpenTabBook(file);
                                    }
                                    else
                                    {
                                        var stream = fileitem?.OpenStreamForReadAsync();
                                        if (stream != null)
                                            tab.OpenTabBook(stream);
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
                    {
                        var bookmark = LibraryStorage.GetItemBookmarks((_) => { });
                        vm.BookmarkRoot = new kurema.BrowserControl.ViewModels.BookmarkItem("", (bmNew) =>
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
                                    , () => Task.FromResult( container.Children.Select(b => (b as kurema.FileExplorerControl.Models.FileItems.IStorageBookmark)?.GetBrowserBookmarkItem())))
                                    { IsReadOnly = true };
                                }
                                else return null;
                            })?.Where(a => a != null);
                        });
                    }

                    vm.OpenDownloadDirectoryCommand = new Helper.DelegateCommand(async (_) =>
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
}
