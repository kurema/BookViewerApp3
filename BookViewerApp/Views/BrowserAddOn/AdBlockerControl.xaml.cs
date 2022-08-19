using BookViewerApp.Storages;
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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views.BrowserAddOn;
public sealed partial class AdBlockerControl : UserControl
{
    public bool IsAdBlockerEnabled
    {
        get => (bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled);
        set => SettingStorage.SetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled, value);
    }

    public AdBlockerControl()
    {
        this.InitializeComponent();
    }

    public void OpenConfig()
    {
        var tab = Helper.UIHelper.GetCurrentTabPage(this);
        tab.OpenTab("AdBlocker", typeof(AdBlockerSetting), null);
    }
}
