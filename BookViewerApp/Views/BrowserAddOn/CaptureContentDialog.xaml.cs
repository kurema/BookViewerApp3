using BookViewerApp.Managers;
using Microsoft.Graphics.Canvas;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Web.WebView2.Core;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Graphics.Printing3D;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static System.Net.WebRequestMethods;

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
            LoadSource();
        }
    }

    private Modes _Mode;
    public Modes Mode
    {
        get => _Mode;
        set
        {
            if (_Mode == value) return;
            _Mode = value;
            switch (_Mode)
            {
                case Modes.Crop:
                    cropper.Visibility = Visibility.Visible;
                    image.Visibility = Visibility.Collapsed;
                    inkPanel.Visibility = Visibility.Collapsed;
                    break;
                case Modes.Ink:
                    cropper.Visibility = Visibility.Collapsed;
                    image.Visibility = Visibility.Collapsed;
                    inkPanel.Visibility = Visibility.Visible;
                    break;
                default:
                case Modes.Basic:
                    cropper.Visibility = Visibility.Collapsed;
                    image.Visibility = Visibility.Visible;
                    inkPanel.Visibility = Visibility.Collapsed;
                    break;
            }
            LoadSource();
        }
    }

    public enum Modes
    {
        Basic, Crop, Ink
    }

    private async void LoadSource()
    {
        switch (Mode)
        {
            default:
            case Modes.Basic:
                {
                    if (image.Source is not BitmapImage bmi)
                    {
                        image.Source = bmi = new BitmapImage();
                    }
                    CurrentStream.Seek(0);
                    try
                    {
                        await bmi.SetSourceAsync(CurrentStream);
                    }
                    catch { }
                }
                break;
            case Modes.Crop:
                {
                    CurrentStream.Seek(0);
                    if (cropper.Source is not WriteableBitmap) cropper.Source = new WriteableBitmap(500, 500);
                    try
                    {
                        await cropper.Source.SetSourceAsync(CurrentStream);
                    }
                    catch { }
                    cropper.Reset();
                }
                break;
            case Modes.Ink:
                {
                    if (inkCanvasBackground.Source is not BitmapImage bmi)
                    {
                        inkCanvasBackground.Source = bmi = new BitmapImage();
                    }
                    CurrentStream.Seek(0);
                    await bmi.SetSourceAsync(CurrentStream);
                    inkParent.Width = bmi.PixelWidth;
                    inkParent.Height = bmi.PixelHeight;
                    float factor = (float)Math.Min(inkScrollViewer.ActualWidth / bmi.PixelWidth, inkScrollViewer.ActualHeight / bmi.PixelHeight);
                    inkScrollViewer.ChangeView(null, null, factor);
                    return;
                }
        }
    }

    private async Task<CanvasRenderTarget> GetCanvasRenderTarget()
    {
        //https://stackoverflow.com/questions/43390816/uwp-how-can-i-attach-an-image-to-an-inkcanvas
        var device = CanvasDevice.GetSharedDevice();
        var target = new CanvasRenderTarget(device, (float)inkCanvasBackground.ActualWidth, (float)inkCanvasBackground.ActualHeight, 96);
        using var ds = target.CreateDrawingSession();
        ds.Clear(Windows.UI.Colors.White);
        var image = await CanvasBitmap.LoadAsync(device, CurrentStream);
        ds.DrawImage(image);
        ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
        return target;
    }

    public CaptureContentDialog()
    {
        this.InitializeComponent();

        cropper.Source = new WriteableBitmap(500, 500);
        inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen;
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
            foreach (var choice in ImageManager.GetFileExtensionsEncodable()) picker.FileTypeChoices.Add(choice.description, choice.extensions.ToList());
            picker.SuggestedFileName = ResourceManager.Loader.GetString("Browser/Addon/Screenshot/Filename");
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            CurrentStream.Seek(0);
            switch (Mode)
            {
                case Modes.Basic:
                    var format = ImageManager.GetBitmapEncoderGuidFromExtension(Path.GetExtension(file.Path)) ?? BitmapEncoder.PngEncoderId;
                    if (format == BitmapEncoder.PngEncoderId) await RandomAccessStream.CopyAsync(CurrentStream, stream);
                    else
                    {
                        var decoder = await BitmapDecoder.CreateAsync(CurrentStream);
                        using var softwareBmp = await decoder.GetSoftwareBitmapAsync();
                        var encoder = await BitmapEncoder.CreateAsync(format, stream);
                        encoder.SetSoftwareBitmap(softwareBmp);
                        await encoder.FlushAsync();
                    }
                    break;
                case Modes.Crop:
                    await cropper.SaveAsync(stream, ImageManager.GetBitmapFileFormatFromExtension(Path.GetExtension(file.Path)) ?? BitmapFileFormat.Png);
                    break;
                case Modes.Ink:
                    await (await GetCanvasRenderTarget()).SaveAsync(stream, ImageManager.GetCanvasBitmapFileFormatFromExtension(Path.GetExtension(file.Path)) ?? CanvasBitmapFileFormat.Png);
                    break;
            }
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
            CurrentStream.Seek(0);
            switch (Mode)
            {
                case Modes.Basic:
                    package.SetBitmap(RandomAccessStreamReference.CreateFromStream(CurrentStream));
                    break;
                case Modes.Crop:
                    {
                        var ms = new InMemoryRandomAccessStream();
                        await cropper.SaveAsync(ms, BitmapFileFormat.Png);
                        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(ms));
                    }
                    break;
                case Modes.Ink:
                    {
                        var ms = new InMemoryRandomAccessStream();
                        await (await GetCanvasRenderTarget()).SaveAsync(ms, CanvasBitmapFileFormat.Png);
                        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(ms));
                    }
                    break;
            }
            Clipboard.SetContent(package);
        }
        catch { }
    }

    private void Zoom(float x)
    {
        inkScrollViewer.ChangeView(null, null, inkScrollViewer.ZoomFactor + x);
    }

    private void ToggleCropper()
    {
        Mode = Mode switch
        {
            Modes.Crop => Modes.Basic,
            Modes.Ink => Modes.Basic,
            Modes.Basic or _ => Modes.Crop,
        };
    }

    private void ToggleInkCanvas()
    {
        Mode = Mode switch
        {
            Modes.Ink => Modes.Basic,
            _ => Modes.Ink,
        };
    }

    private void Button_Click_ZoomIn(object sender, RoutedEventArgs e)
    {
        Zoom(0.1f);
    }

    private void Button_Click_ZoomOut(object sender, RoutedEventArgs e)
    {
        Zoom(-0.1f);
    }
}
