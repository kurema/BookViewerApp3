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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views;

public sealed partial class SettingPanelControl : UserControl
{
    public CollectionViewSource SettingSource => this.settingSource;

    public SettingPanelControl()
    {
        this.InitializeComponent();
    }

    public IEnumerable<GroupItem> GetGroupsContainer()
    {
        if (itemsControlMain.ItemsPanelRoot is not Panel stackPanel) return new GroupItem[0];
        return stackPanel.Children.Select(a => a as GroupItem);
    }
}
