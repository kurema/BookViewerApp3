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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class PasswordRequestContentDialog : ContentDialog
{
    public bool Remember { get; set; } = false;

    public PasswordRequestContentDialog()
    {
        this.InitializeComponent();

        this.Title = Managers.ResourceManager.Loader.GetString("Password/Title");
        this.PrimaryButtonText = Managers.ResourceManager.Loader.GetString("Word/OK");
        this.SecondaryButtonText = Managers.ResourceManager.Loader.GetString("Word/Cancel");
    }

    public string Password
    {
        get { return (string)GetValue(PasswordProperty); }
        set { SetValue(PasswordProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Password.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register("Password", typeof(string), typeof(PasswordRequestContentDialog), new PropertyMetadata(null));

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrEmpty(Password))
            args.Cancel = true;
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

    }
}
