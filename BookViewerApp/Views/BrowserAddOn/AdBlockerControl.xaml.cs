﻿using BookViewerApp.Storages;
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
    public string Url
    {
        get { return (string)GetValue(UrlProperty); }
        set { SetValue(UrlProperty, value); }
    }

    public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(nameof(Url), typeof(string), typeof(AdBlockerControl), new PropertyMetadata(""));


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

    private void CheckBox_Whitelist_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void CheckBox_Whitelist_Unchecked(object sender, RoutedEventArgs e)
    {

    }
}
