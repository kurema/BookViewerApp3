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
    }

    public async void LoadFilters()
    {
        if (DataContext is ViewModels.AdBlockerSettingViewModel vm)
        {
            await vm.LoadFilterList();
        }
    }

    public Helper.DelegateCommand OpenWebCommand { get; }
}
