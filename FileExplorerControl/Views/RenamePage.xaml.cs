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

namespace kurema.FileExplorerControl.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class RenamePage : Page
{
    public RenamePage()
    {
        this.InitializeComponent();
    }

    private async void MenuFlyoutItem_Click_Help_Regex(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://aka.ms/powertoysRegExHelp"));
    }

    private async void MenuFlyoutItem_Click_Help_About(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot=this.XamlRoot,
        };
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock() { Text = "This tool was modeled after PowerRename (PowerToys family).\nTry PowerRename if you like this." ,TextWrapping= TextWrapping.Wrap});
            stack.Children.Add(new HyperlinkButton() { Content = "PowerToys", NavigateUri = new Uri("https://docs.microsoft.com/windows/powertoys/install") });
            dialog.Content = stack;
        }
        {
            var loader = Application.ResourceLoader.Loader;
            dialog.CloseButtonText = loader.GetString("Command/OK");
        }
        try
        {
            await dialog.ShowAsync();
        }
        catch { }
    }
}
