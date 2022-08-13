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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views.BrowserAddOn;
public sealed partial class UserAgentOverride : UserControl
{
    public UserAgentOverride()
    {
        this.InitializeComponent();
        ResetUA();
    }

    public event EventHandler UserAgentUpdated;

    private void Button_Click_Ok(object sender, RoutedEventArgs e)
    {
        var ua = textBoxUA.Text;
        ua = string.IsNullOrWhiteSpace(ua) ? string.Empty : ua;
        Storages.SettingStorage.SetValue(Storages.SettingStorage.SettingKeys.BrowserUserAgent, ua);
        UserAgentUpdated?.Invoke(this, new EventArgs());
    }

    private void ResetUA()
    {
        textBoxUA.Text = (string)Storages.SettingStorage.GetValue(Storages.SettingStorage.SettingKeys.BrowserUserAgent);
    }
}
