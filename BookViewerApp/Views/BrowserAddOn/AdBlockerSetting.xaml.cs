﻿using BookViewerApp.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

#nullable enable
namespace BookViewerApp.Views.BrowserAddOn;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class AdBlockerSetting : Page
{
    public AdBlockerSetting()
    {
        this.InitializeComponent();

        //https://docs.microsoft.com/en-us/archive/msdn-magazine/2014/april/mvvm-multithreading-and-dispatching-in-mvvm-applications
        //You can not Task.Run() here.
        LoadFilters();
        OpenWebCommand = new Helper.DelegateCommand(async address =>
        {
            var tab = Helper.UIHelper.GetCurrentTabPage(this);
            if (tab is null) return;
            await tab.OpenTabWebPreferedBrowser(address?.ToString());
        }, address => Uri.TryCreate(address?.ToString(), UriKind.Absolute, out var _));

        async void LoadTextEditors()
        {
            await textEditorCustomFilters.LoadFile(await Managers.ExtensionAdBlockerManager.GetCustomFiltersFileAsync(true));
            await textEditorTrustedSites.LoadFile(await Managers.ExtensionAdBlockerManager.GetWhiteListFileAsync(true));
        }

        LoadTextEditors();
    }

    public async void LoadFilters()
    {
        if (DataContext is ViewModels.AdBlockerSettingViewModel vm)
        {
            await vm.LoadFilterList();
        }
    }

    public Helper.DelegateCommand OpenWebCommand { get; }

    private void textEditorCustomFilters_FileSaving(kurema.FileExplorerControl.Views.Viewers.TextEditorPage sender, kurema.FileExplorerControl.Views.Viewers.TextEditorPage.SavingFileEventArgs args)
    {
        var parser = new DistillNET.AbpFormatRuleParser();
        try
        {
            parser.ParseAbpFormattedRule(sender.Text, 0);
        }
        catch (Exception e)
        {
            var loader = Managers.ResourceManager.Loader;
            args.Cancel(string.Format(loader.GetString("Extension/AdBlocker/CustomFilter/Error/Message"), e.Message), loader.GetString("Extension/AdBlocker/CustomFilter/Error/Title"));
        }
    }

    private async void textEditorCustomFilters_FileSaved(object sender, EventArgs e)
    {
        await Managers.ExtensionAdBlockerManager.LoadUserWhitelist();
    }


    private void Button_Click_Open_Document(object sender, RoutedEventArgs e)
    {
        //if ((sender as FrameworkElement)?.Tag?.ToString() is not string s || Uri.TryCreate(s.ToString(), UriKind.Absolute, out var targetUri)) return;
        OpenWebCommand?.Execute("https://github.com/kurema/BookViewerApp3/blob/master/res/Docs/AdBlocker/readme.md");
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag?.ToString() is not string s) return;
        switch (s)
        {
            case "Cache":
                await Windows.System.Launcher.LaunchFolderAsync(await Managers.ExtensionAdBlockerManager.GetDataFolderCache());
                return;
            case "Local":
                await Windows.System.Launcher.LaunchFolderAsync(await Managers.ExtensionAdBlockerManager.GetDataFolderLocal());
                return;
        }
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ViewModels.AdBlockerSettingFilterViewModel vmItem) return;
        if (this.DataContext is not ViewModels.AdBlockerSettingViewModel vm) return;
        if (vmItem.DeleteCommand?.CanExecute(null) != true) return;
        vmItem.DeleteCommand?.Execute(null);
        await vm.SaveCustomFilters();
    }

    public string CustomFilterFileNameHeader => Managers.ExtensionAdBlockerManager.CustomFilterFileNameHeader;

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not ViewModels.AdBlockerSettingFilterViewModel vm) return;
        var tab = UIHelper.GetCurrentTabPage(this);
        if (tab is null) return;
        var content = vm.GetContent();
        if (!content.IsValidEntry || string.IsNullOrWhiteSpace(content.filename)) return;
        var df = await Managers.ExtensionAdBlockerManager.GetDataFolderCache();
        Windows.Storage.StorageFile item;
        try
        {
            item = await df.GetFileAsync(content.filename);
        }
        catch { return; }
        var te = tab.OpenTabTextEditor(item);
        var loader = Managers.ResourceManager.Loader;
        if (te is not null)
        {
            te.CanOverwrite = false;
            te.CanChageSavePath = false;
            te.Info = string.Format(loader.GetString("Extension/AdBlocker/Filters/Notepad/Info"), vm.Title, vm.FileName, vm.Source);
        }
    }
}
