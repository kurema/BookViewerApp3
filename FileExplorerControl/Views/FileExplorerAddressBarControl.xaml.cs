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

namespace kurema.FileExplorerControl.Views;

public sealed partial class FileExplorerAddressBarControl : UserControl
{
    public FileExplorerAddressBarControl()
    {
        this.InitializeComponent();
    }

    public DataTemplate AddressItemDataTemplate => (DataTemplate)this.Resources["addressTemplate"];

    public void SetAddress(ViewModels.FileItemViewModel file)
    {
        var list = new List<UIElement>();

        var cfile = file;
        while (true)
        {
            var elem = AddressItemDataTemplate.LoadContent() as FrameworkElement;
            if (elem != null)
            {
                elem.DataContext = cfile;
                elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var temp = elem.DesiredSize;
                list.Add(elem);
            }


            if (cfile.Parent is null)
            {
                break;
            }
            else
            {
                cfile = cfile.Parent;
            }
        }
        list.Reverse();
        stack.Children.Clear();
        foreach (var item in list)
        {
            stack.Children.Add(item);
        }
    }

    private void Button_ClickGoTo(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is ViewModels.FileExplorerViewModel fvm && (sender as FrameworkElement)?.DataContext is ViewModels.FileItemViewModel bvm)
        {
            fvm.Content.GoToCommand.Execute(bvm);
        }
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (this.DataContext is ViewModels.FileExplorerViewModel fvm && e.ClickedItem is ViewModels.FileItemViewModel bvm)
        {
            fvm.Content.GoToCommand.Execute(bvm);
        }

    }

    public event EventHandler FocusLostRequested;
    private void Button_ClickFocusOff(object sender, PointerRoutedEventArgs e)
    {
        FocusLostRequested?.Invoke(this, new EventArgs());
    }
}
