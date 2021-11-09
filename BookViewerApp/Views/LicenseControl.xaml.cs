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

namespace BookViewerApp.Views;

public sealed partial class LicenseControl : UserControl
{
    public LicenseControl()
    {
        this.InitializeComponent();
    }

    public object Source
    {
        get => itemsSource?.Source;
        set => itemsSource.Source = value;
    }

    public System.Windows.Input.ICommand OpenWebCommand { get; set; }

    private void AcrylicButtonControl_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var address = (sender as Button)?.CommandParameter?.ToString();
        OpenWebCommand?.Execute(address);
    }

    //private async void AcrylicButtonControl_Tapped_1(object sender, TappedRoutedEventArgs e)
    //{
    //    //You can't open ContentDialog in ContentDialog.
    //    var term = (sender as Button)?.CommandParameter?.ToString();
    //    var dialog = new ContentDialog();
    //    dialog.Content = new TextBlock()
    //    {
    //        Text = term,
    //    };
    //    dialog.CloseButtonText = Managers.ResourceManager.Loader.GetString("Word/OK");
    //    await dialog.ShowAsync();
    //}
}
