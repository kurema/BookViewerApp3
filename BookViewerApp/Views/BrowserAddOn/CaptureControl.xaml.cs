using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views.BrowserAddOn;
public sealed partial class CaptureControl : UserControl
{
    public Func<IRandomAccessStream, Task> WriteToStreamAction { get; set; }
    public Func<XamlRoot> XamlRootProvider { get; set; }

    public CaptureControl()
    {
        this.InitializeComponent();
    }

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (WriteToStreamAction is null) return;
        var ms = new InMemoryRandomAccessStream();
        await WriteToStreamAction(ms);
        var content = new CaptureContentDialog() { WriteToStreamAction = WriteToStreamAction, CurrentStream = ms };
        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            content.XamlRoot = XamlRootProvider?.Invoke();
        }
        try
        {
            await content.ShowAsync();
        }
        catch { }
    }
}
