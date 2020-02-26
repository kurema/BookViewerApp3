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

using System.Threading.Tasks;
using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using BookViewerApp.Views;

namespace BookViewerApp.Helper
{
    public static class UIHelper
    {
        public static void SetTitle(FrameworkElement targetElement, string title)
        {
            if (((targetElement as Page)?.Frame)?.Parent is winui.Controls.TabViewItem item2)
            {
                item2.Header = title;
            }
            else if ((targetElement.Parent as Frame)?.Parent is winui.Controls.TabViewItem item)
            {
                item.Header = title;
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = title;
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

                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(kurema.BrowserControl.ViewModels.BrowserControlViewModel.Title))
                    {
                        UpdateTitle(vm.Title);
                    }
                };
            }
        }

        public static Views.TabPage GetCurrentTabPage(UIElement ui)
        {
            if ((ui?.XamlRoot?.Content as Frame).Content is TabPage tab)
            {
                return tab;
            }
            else if (ui?.XamlRoot.Content is TabPage tab3)
            {
                return tab3;
            }
            //else if (Window.Current?.Content is Frame f && f.Content is TabPage tab2)
            //{
            //    return tab2;
            //}
            return null;
        }
    }
}
