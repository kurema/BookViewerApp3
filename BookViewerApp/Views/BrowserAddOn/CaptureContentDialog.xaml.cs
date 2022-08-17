using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views.BrowserAddOn;
public sealed partial class CaptureContentDialog : ContentDialog
{
    public Func<IRandomAccessStream, Task> WriteToStreamAction { get; set; }
    private InMemoryRandomAccessStream _CurrentStream;
    public InMemoryRandomAccessStream CurrentStream
    {
        get => _CurrentStream; set
        {
            _CurrentStream = value;
            if (value is null) return;
            if (image.Source is not BitmapImage bmi)
            {
                image.Source = bmi = new BitmapImage();
            }
            bmi.SetSource(value);
        }
    }

    public CaptureContentDialog()
    {
        this.InitializeComponent();
    }

    public async Task Refresh()
    {
        try
        {
            if (WriteToStreamAction is null) return;
            var ms = new InMemoryRandomAccessStream();
            await WriteToStreamAction(ms);
            if (ms is null) return;
            CurrentStream = ms;
        }
        catch
        {
        }
    }

    public async Task Save()
    {
        try
        {
            if (CurrentStream is null) await Refresh();
            if (CurrentStream is null) return;
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            picker.SuggestedFileName = Managers.ResourceManager.Loader.GetString("Browser/Addon/Screenshot/Filename");
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            CurrentStream.Seek(0);
            await RandomAccessStream.CopyAsync(CurrentStream, stream);
        }
        catch { }
    }

    public async Task Copy()
    {
        try
        {
            if (CurrentStream is null) await Refresh();
            if (CurrentStream is null) return;
            var package = new DataPackage();
            package.RequestedOperation = DataPackageOperation.Copy;
            package.SetBitmap(RandomAccessStreamReference.CreateFromStream(CurrentStream));
            Clipboard.SetContent(package);
        }
        catch { }
    }
}
