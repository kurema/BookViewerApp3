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
            public static void OpenEpub(Frame frame, Windows.Storage.IStorageFile file, TabPage tabPage = null)
            {
                if (file == null) return;
                var resolver = EpubResolver.GetResolverBibi(file);
                frame.Navigate(typeof(kurema.BrowserControl.Views.BrowserPage), null);

                if (frame.Content is kurema.BrowserControl.Views.BrowserPage content)
                {
                    Uri uri = content.Control.Control.BuildLocalStreamUri("epub", resolver.PathHome);
                    content.Control.Control.NavigateToLocalStreamUri(uri, resolver);
                    if (tabPage != null)
                    {
                        content.Control.Control.NewWindowRequested += (s, e) =>
                        {
                            tabPage.OpenTabWeb(e.Uri.ToString());
                            e.Handled = true;
                        };
                    }
                }
            }

            public static async void OpenBookPicked(Frame frame, FrameworkElement sender)
            {
                var file = await BookManager.PickFile();
                OpenBook(file, frame, sender);
            }

            public static void OpenBook(Windows.Storage.IStorageFile file, Frame frame, FrameworkElement sender = null)
            {
                if (file == null) return;

                if (file.ContentType == "application/epub+zip" || file.FileType.ToLower() == ".epub")
                {
                    if (sender != null) SetTitleByResource(sender, "Epub");
                    OpenEpub(frame, file, GetCurrentTabPage(sender));
                }
                else
                {
                    frame.Navigate(typeof(BookFixed3Viewer), file);
                }
            }

            public static async void OpenExplorer(Frame frame, FrameworkElement sender = null)
            {
                {
                    UIHelper.SetTitleByResource(sender, "Explorer");
                    frame.Navigate(typeof(kurema.FileExplorerControl.Views.FileExplorerPage), null);
                    if (frame.Content is kurema.FileExplorerControl.Views.FileExplorerPage content)
                    {
                        if (content.Content is kurema.FileExplorerControl.Views.FileExplorerControl control)
                        {
                            if (control.DataContext is kurema.FileExplorerControl.ViewModels.FileExplorerViewModel fvm && fvm.Content != null)
                            {
                                control.AddressRequesteCommand = new DelegateCommand(async (address) =>
                                {
                                    if (string.IsNullOrWhiteSpace(address?.ToString())) return;
                                    Uri uriResult;
                                    var tab = GetCurrentTabPage(content);
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
                                    var tab = GetCurrentTabPage(content);
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

                                control.MenuChildrens.Add(new ExplorerMenuControl() { OriginPage = content });

                                var library = LibraryStorage.GetItem((a) =>
                                {
                                    var tab = GetCurrentTabPage(content);
                                    if (tab == null) return;
                                    tab.OpenTabWeb(a);
                                }, control.AddressRequesteCommand
                                );

                                //var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(folder));
                                var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(library);
                                fv.IconProviders.Add(new kurema.FileExplorerControl.Models.IconProviders.IconProviderDelegate(async (a) =>
                                {
                                    if (a is kurema.FileExplorerControl.Models.FileItems.BookmarkItem || a is kurema.FileExplorerControl.Models.FileItems.StorageBookmarkItem)
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
                                    var fileitem = (e2 as kurema.FileExplorerControl.ViewModels.FileItemViewModel)?.Content;
                                    if (!BookManager.AvailableExtensionsArchive.Contains(System.IO.Path.GetExtension(fileitem?.Name ?? "").ToLower()))
                                    {
                                        return;
                                    }

                                    var tab = UIHelper.GetCurrentTabPage(content);
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
            }

            public static void OpenBrowser(Frame frame, string uri, Action<string> OpenTabWeb, Action<Windows.Storage.IStorageItem> OpenTabBook, Action<string> UpdateTitle)
            {
                frame?.Navigate(typeof(kurema.BrowserControl.Views.BrowserPage), uri);
                if (frame?.Content is kurema.BrowserControl.Views.BrowserPage content)
                {
                    content.Control.Control.NewWindowRequested += (s, e) =>
                    {
                        OpenTabWeb?.Invoke(e.Uri.ToString());
                        e.Handled = true;
                    };

                    content.Control.Control.UnviewableContentIdentified += async (s, e) =>
                    {
                        string extension = "";
                        foreach (var ext in BookManager.AvailableExtensionsArchive)
                        {
                            if (Path.GetExtension(e.Uri.ToString()).ToLower() == ext)
                            {
                                extension = ext;
                                goto success;
                            }
                        }
                    success:;
                        try
                        {
                            var namebody = Path.GetFileNameWithoutExtension(e.Uri.ToString());
                            namebody = namebody.Length > 32 ? namebody.Substring(0, 32) : namebody;
                            var item = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                                Path.Combine("Download", namebody + extension)
                                , Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                            var downloader = new BackgroundDownloader();
                            var download = downloader.CreateDownload(e.Uri, item);

                            var vmb = content.Control.DataContext as kurema.BrowserControl.ViewModels.BrowserControlViewModel;
                            var dlitem = new kurema.BrowserControl.ViewModels.DownloadItemViewModel(item.Name);
                            vmb?.Downloads.Add(dlitem);

                            await download.StartAsync().AsTask(new Progress<DownloadOperation>((t) =>
                            {
                                try
                                {
                                    //https://stackoverflow.com/questions/38939720/backgrounddownloader-progress-doesnt-update-in-uwp-c-sharp
                                    if (t.Progress.TotalBytesToReceive > 0)
                                    {
                                        double br = t.Progress.TotalBytesToReceive;
                                        dlitem.DownloadedRate = br / t.Progress.TotalBytesToReceive;
                                    }
                                }
                                catch { }
                            }));

                            dlitem.DownloadedRate = 1.0;
                            dlitem.OpenCommand = new DelegateCommand((a) => OpenTabBook?.Invoke(item));

                            OpenTabBook?.Invoke(item);
                            BookViewerApp.App.Current.Suspending += async (s2, e2) =>
                            {
                                try
                                {
                                    await item.DeleteAsync();
                                }
                                catch { }
                            };
                        }
                        catch { }
                        return;
                    };
                }

                if ((frame.Content as kurema.BrowserControl.Views.BrowserPage)?.Control.DataContext is kurema.BrowserControl.ViewModels.BrowserControlViewModel vm && vm != null)
                {
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
