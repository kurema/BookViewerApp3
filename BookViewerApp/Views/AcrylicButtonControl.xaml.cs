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

public sealed partial class AcrylicButtonControl : Button
{
    public AcrylicButtonControl()
    {
        this.InitializeComponent();
    }



    //public IconElement Icon
    //{
    //    get
    //    {
    //        return this.Content as IconElement;
    //        //return (IconElement)GetValue(IconProperty);
    //    }
    //    set
    //    {
    //        SetValue(IconProperty, value);
    //        this.Content = value;
    //    }
    //}

    //public static readonly DependencyProperty IconProperty =
    //    DependencyProperty.Register("Icon", typeof(IconElement), typeof(AcrylicButtonControl), new PropertyMetadata(null));



    public IconElement Icon
    {
        get => this.Content as IconElement;
        set => this.Content = value;
    }
}
