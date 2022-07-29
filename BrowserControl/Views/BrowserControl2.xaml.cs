﻿using System;
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

using System.Threading.Tasks;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace kurema.BrowserControl.Views;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class BrowserControl2 : Page
{
    public Microsoft.UI.Xaml.Controls.WebView2 WebView2 => webView;

    public BrowserControl2()
    {
        this.InitializeComponent();

        if (DataContext is ViewModels.BrowserControl2ViewModel vm)
        {
            //vm.ActionNavigate = (s) => webView.NavigateToString(s);
            vm.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName is nameof(vm.Source))
                {

                }
            };
        }

        Task.Run(async () =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;
        });
        
    }

    private void CoreWebView2_ContainsFullScreenElementChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
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

    private async void listViewBookmarks_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (DataContext is not ViewModels.BrowserControl2ViewModel vm) return;

        if (sender is ItemsControl && e.ClickedItem is ViewModels.IBookmarkItem bookmarkItem)
        {
            if (bookmarkItem.IsFolder)
            {
                vm.BookmarkCurrent.Clear();
                foreach (var item in await bookmarkItem.GetChilderenAsync()) vm.BookmarkCurrent.Add(item);
            }
            else
            {
                if (Uri.TryCreate(bookmarkItem.Address, UriKind.Absolute, out Uri uri))
                {
                    vm.Source = uri;
                }
            }
        }
    }

    private async void ListViewItem_Tapped_1(object sender, TappedRoutedEventArgs e)
    {
        if (DataContext is ViewModels.BrowserControl2ViewModel vm && vm?.BookmarkRoot != null && vm.BookmarkCurrent != null)
        {
            vm.BookmarkCurrent.Clear();
            foreach (var item in await vm.BookmarkRoot.GetChilderenAsync()) vm.BookmarkCurrent.Add(item);
        }
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

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var node = treeView_BookMarkAdd.SelectedNode;
        if (node == null || node.Content is null)
        {
            if (DataContext is ViewModels.BrowserControl2ViewModel vm && vm?.BookmarkRoot != null)
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

    private void Button_Click_ToggleCollapsed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.BrowserControl2ViewModel vm) return;
        vm.ControllerCollapsed = !vm.ControllerCollapsed;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await webView.EnsureCoreWebView2Async();
        if (e.Parameter is null) return;
        else if (e.Parameter is Windows.ApplicationModel.Activation.IActivatedEventArgs)
        {
        }
        else if (e.Parameter is string s)
        {
            if (DataContext is ViewModels.BrowserControl2ViewModel vm && vm != null)
            {
                vm.SourceString = s;
            }
        }
    }

    private async void AppBarButton_Click_Reload(object sender, RoutedEventArgs e)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.Reload();
        }
        catch { }
    }

    private async void AppBarButton_Click_GoBack(object sender, RoutedEventArgs e)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.GoBack();
        }
        catch { }
    }

    private async void AppBarButton_Click_GoForward(object sender, RoutedEventArgs e)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.GoForward();
        }
        catch { }
    }
}