using BookViewerApp.Helper;
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
                    {
                        double w = double.IsNaN(image.ActualWidth) ? inkAnnotation.ActualWidth : image.ActualWidth;
                        double h = double.IsNaN(image.ActualHeight) ? inkAnnotation.ActualHeight : image.ActualHeight;

                        cropper.Visibility = Visibility.Visible;
                        image.Visibility = Visibility.Collapsed;
                        inkAnnotation.Visibility = Visibility.Collapsed;

                        if(!double.IsNaN(w) && !double.IsNaN(h))
                        {
                            cropper.Width = w;
                            cropper.Height = h;
                        }
                    }
                    break;
                case Modes.Ink:
                    cropper.Visibility = Visibility.Collapsed;
                    image.Visibility = Visibility.Collapsed;
                    inkAnnotation.Visibility = Visibility.Visible;
                    break;
                default:
                case Modes.Basic:
                    cropper.Visibility = Visibility.Collapsed;
                    image.Visibility = Visibility.Visible;
                    inkAnnotation.Visibility = Visibility.Collapsed;
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
                    await inkAnnotation.SetSouceAsync(CurrentStream);
                    return;
                }
        }
    }



    public CaptureContentDialog()
    {
        this.InitializeComponent();

        cropper.Source = new WriteableBitmap(500, 500);
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
                    await (await inkAnnotation.GetCanvasRenderTarget()).SaveAsync(stream, ImageManager.GetCanvasBitmapFileFormatFromExtension(Path.GetExtension(file.Path)) ?? CanvasBitmapFileFormat.Png);
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
                        await (await inkAnnotation.GetCanvasRenderTarget()).SaveAsync(ms, CanvasBitmapFileFormat.Png);
                        package.SetBitmap(RandomAccessStreamReference.CreateFromStream(ms));
                    }
                    break;
            }
            Clipboard.SetContent(package);
        }
        catch { }
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


}
