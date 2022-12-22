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

using Windows.Networking.BackgroundTransfer;
using kurema.BrowserControl.ViewModels;
using System.Threading.Tasks;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.BrowserControl.Views;

public sealed partial class BrowserControl : Page, IDisposable
{
    public WebView Control => this.webView;

    public UIElementCollection AddOnSpace => PanelOthers.Children;

    public BrowserControl()
    {
        this.InitializeComponent();

        //webView.Navigate(new Uri("http://www.google.co.jp/"));
        if (this.DataContext is ViewModels.BrowserControlViewModel vm)
        {
            vm.Content = this.webView;
        }

        webView.NavigationFailed += WebView_NavigationFailed;
    }

    private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
    {
        HideErrorStoryboard.Begin();
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            webView.Focus(FocusState.Programmatic);
        }
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        (sender as TextBox)?.SelectAll();
    }

    private async void Button_Click_OpenBrowser(object sender, RoutedEventArgs e)
    {
        if (webView.Source != null) await Windows.System.Launcher.LaunchUriAsync(webView.Source);
    }

    private void webView_ContainsFullScreenElementChanged(WebView sender, object args)
    {
        var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();

        if (sender?.ContainsFullScreenElement == true && !v.IsFullScreenMode)
        {
            if (v.TryEnterFullScreenMode())
            {
            }
        }
        else if (sender?.ContainsFullScreenElement == false && v.IsFullScreenMode)
        {
            v.ExitFullScreenMode();
        }
    }

    public void Dispose()
    {
        //To stop audio in background.
        //There should be the better way.
        webView.NavigateToString("");
        this.Content = new Grid();
    }

    private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var command = ((sender as FrameworkElement)?.DataContext as ViewModels.BrowserControlViewModel)?.OpenDownloadDirectoryCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }

    public Action<WebView, WebViewUnviewableContentIdentifiedEventArgs, ViewModels.BrowserControlViewModel> UnviewableContentIdentifiedOverride { get; set; }

    private async void webView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
    {
        if (DataContext is not ViewModels.BrowserControlViewModel dataContext) return;

        if (UnviewableContentIdentifiedOverride != null)
        {
            UnviewableContentIdentifiedOverride.Invoke(sender, args, dataContext);
            return;
        }

        try
        {
            var namebody = Path.GetFileNameWithoutExtension(args.Uri.AbsoluteUri);
            namebody = namebody.Length > 32 ? namebody.Substring(0, 32) : namebody;
            var extension = Path.GetExtension(args.Uri.AbsoluteUri);
            extension = string.IsNullOrEmpty(extension) ? MimeTypes.MimeTypeMap.GetExtension(args.MediaType) : extension;

            var item = await (await dataContext.FolderProvider?.Invoke())?.CreateFileAsync(
                namebody + extension
                , Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            if (item is null) return;
            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(args.Uri, item);

            var dlitem = new ViewModels.DownloadItemViewModel(item);
            dataContext?.Downloads.Add(dlitem);

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
            OpenFile(item);
        }
        catch { }
    }

    private void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if ((sender as FrameworkElement).DataContext is ViewModels.DownloadItemViewModel dlitem && dlitem.DownloadedRate == 1.0) OpenFile(dlitem.File);
    }

    private void OpenFile(Windows.Storage.StorageFile storageFile)
    {
        DownloadedFileOpenedEventHandler?.Invoke(this, storageFile);
    }

    public TypedEventHandler<BrowserControl, Windows.Storage.StorageFile> DownloadedFileOpenedEventHandler;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is null) return;
        else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
        {
        }
        else if (e.Parameter is string s)
        {
            if (DataContext is ViewModels.BrowserControlViewModel vm && vm != null)
            {
                vm.Uri = s;
            }
        }
    }

    private async void listViewBookmarks_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (sender is not ItemsControl || e.ClickedItem is not ViewModels.IBookmarkItem bookmarkItem || DataContext is not ViewModels.BrowserControlViewModel vm) return;
        if (bookmarkItem is ViewModels.BookmarkItemGoUp)
        {
            vm.BookmarkCurrent.Clear();
            foreach (var item in await bookmarkItem.GetChilderenAsync()) vm.BookmarkCurrent.Add(item);
            return;
        }
        else if (bookmarkItem.IsFolder)
        {
            var goup = new ViewModels.BookmarkItemGoUp(vm.BookmarkCurrent.ToArray());
            vm.BookmarkCurrent.Clear();
            vm.BookmarkCurrent.Add(goup);
            foreach (var item in await bookmarkItem.GetChilderenAsync())
            {
                if (item is not ViewModels.BookmarkItemGoUp) vm.BookmarkCurrent.Add(item);
            }
        }
        else
        {
            if (Uri.TryCreate(bookmarkItem.Address, UriKind.Absolute, out Uri uri))
            {
                webView.Navigate(uri);
            }
        }
    }

    private async void ListViewItem_Tapped_1(object sender, TappedRoutedEventArgs e)
    {
        if (DataContext is ViewModels.BrowserControlViewModel vm && vm?.BookmarkRoot != null && vm.BookmarkCurrent != null)
        {
            vm.BookmarkCurrent.Clear();
            foreach (var item in await vm.BookmarkRoot.GetChilderenAsync()) vm.BookmarkCurrent.Add(item);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var node = treeView_BookMarkAdd.SelectedNode;
        if (node == null || node.Content is null)
        {
            if (DataContext is ViewModels.BrowserControlViewModel vm && vm?.BookmarkRoot != null)
            {
                vm.BookmarkRoot.AddItem(new ViewModels.BookmarkItem(textBox_BookmarkAdd.Text, webView.Source.AbsoluteUri));
                button_favorite.Flyout.Hide();
            }
            return;
        }
        if (node.Content is ViewModels.IBookmarkItem bm)
        {
            bm.AddItem(new ViewModels.BookmarkItem(textBox_BookmarkAdd.Text, webView.Source.AbsoluteUri));
            button_favorite.Flyout.Hide();
        }
    }

    private async void TreeView_Expanding(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewExpandingEventArgs args)
    {
        if (!args.Node.HasUnrealizedChildren) return;

        sender.IsEnabled = false;

        try
        {
            if (args.Item is ViewModels.IBookmarkItem vm)
            {
                var container = sender.ContainerFromItem(args.Item);

                if (!vm.IsFolder) return;
                var child = (await vm.GetChilderenAsync())?.Where(a => a.IsFolder && !a.IsReadOnly);
                if (child is null) return;
                if (container is Microsoft.UI.Xaml.Controls.TreeViewItem tvi)
                {
                    tvi.ItemsSource = child;
                }
            }
        }
        finally
        {
            args.Node.HasUnrealizedChildren = false;
            sender.IsEnabled = true;
        }

    }

    private void Button_Click_ToggleCollapsed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowserControlViewModel vm) return;
        vm.ControllerCollapsed = !vm.ControllerCollapsed;
    }

    private void RestoreAddressBarTextBoxText()
    {
        if (this.DataContext is not BrowserControlViewModel vm) return;
        //{Binding Uri,UpdateSourceTrigger=Default,Mode=OneWay}
        addressBarBox.SetBinding(AutoSuggestBox.TextProperty, new Binding()
        {
            Path = new PropertyPath(nameof(vm.Uri)),
            UpdateSourceTrigger = UpdateSourceTrigger.Default,
            Mode = BindingMode.OneWay,
        });
    }

    private async void addressBarTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (this.DataContext is not IBrowserControlViewModel vm) return;
        string word = sender.Text;
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var list = new System.Collections.ObjectModel.ObservableCollection<ISearchEngineEntry>();
            //if (vm.SearchEngineDefault is not null)
            //{
            //    vm.SearchEngineDefault.Word = word;
            //    var candidates = (await vm.SearchEngineDefault.GetCandidates())?.ToArray();
            //    if (candidates?.Count() > 0) list.AddRange(candidates);
            //    list.Add(vm.SearchEngineDefault);
            //}
            if (vm.SearchEngines is not null && !string.IsNullOrEmpty(word))
            {
                foreach (var item in vm.SearchEngines)
                {
                    if (item is null) continue;
                    item.Word = word;
                    list.Add(item);
                }
            }
            sender.ItemsSource = list;

            var complitions = await Helper.SearchComplitions.SearchComplitionManager.UseCacheOrGet(word);
            foreach (var comp in complitions.Reverse())
            {
                var toAdd = new SearchEngineEntryDelegate(comp, (_, action) =>
                {
                    vm.Search(comp);
                    return Task.CompletedTask;
                });
                list.Insert(0, toAdd);
            }
        }
        else
        {
            sender.IsSuggestionListOpen = false;
        }
    }


    private void addressBarTextBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not ISearchEngineEntry entry) return;
        if (DataContext is not BrowserControlViewModel vm) return;

        entry.Open((url) =>
        {
            vm.Uri = url;
            return Task.CompletedTask;
        });

        RestoreAddressBarTextBoxText();
    }

    private void addressBarTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is null)
        {
            if (this.DataContext is BrowserControlViewModel vm && sender is AutoSuggestBox box)
            {
                vm.Uri = box.Text;
            }
        }
    }
}
