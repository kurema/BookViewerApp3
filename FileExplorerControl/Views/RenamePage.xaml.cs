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

using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;

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

        var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
         {
             {
                 //Add DateTime format
                 foreach (var item in Windows.System.UserProfile.GlobalizationPreferences.Languages)
                 {
                     var culture = new System.Globalization.CultureInfo(item);
                     MenuBarItemRegion.Items.Add(new ToggleMenuFlyoutItem()
                     {
                         Tag = culture,
                         Text = culture.DisplayName,
                     });
                 }
                 MenuBarItemRegion.Items.Add(new MenuFlyoutSeparator());
                 var others = new MenuFlyoutSubItem() { Text = "Others" };
                 MenuBarItemRegion.Items.Add(others);
                 foreach (var item in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures))
                 {
                     others.Items.Add(new ToggleMenuFlyoutItem()
                     {
                         Tag = item,
                         Text = item.DisplayName,
                     });
                 }
             }
         });
    }

    void SetupWindow()
    {
        {
            // Extend into the titlebar
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            Window.Current.SetTitleBar(CustomDragRegion);
        }
    }

    private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
    {
        if (FlowDirection == FlowDirection.LeftToRight)
        {
            CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
            ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
        }
        else
        {
            CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
            ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
        }

        CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        SetupWindow();

        base.OnNavigatedTo(e);
    }

    private async void MenuFlyoutItem_Click_Help_Regex(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://aka.ms/powertoysRegExHelp"));
    }

    private async void MenuFlyoutItem_Click_Help_About(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,// You need this for AppWindow. Now not.
        };
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock() { Text = "This tool was modeled after PowerRename (PowerToys family).\nTry PowerRename if you like this.", TextWrapping = TextWrapping.Wrap });
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
