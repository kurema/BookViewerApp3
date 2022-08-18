using BookViewerApp.Managers;
using Microsoft.Toolkit.Uwp.UI.Controls;
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
            LoadSource();
        }
    }
    private bool _IsCropperEnebled;
    public bool IsCropperEnebled
    {
        get => _IsCropperEnebled;
        set
        {
            if (_IsCropperEnebled == value) return;
            _IsCropperEnebled = value;
            if (value)
            {
                cropper.Visibility = Visibility.Visible;
                image.Visibility = Visibility.Collapsed;
            }
            else
            {
                cropper.Visibility = Visibility.Collapsed;
                image.Visibility = Visibility.Visible;
            }
            LoadSource();
        }
    }
    private async void LoadSource()
    {
        if (IsCropperEnebled)
        {
            //CurrentStream.Seek(0);
            //var bitmap = new BitmapImage();
            //bitmap.SetSource(CurrentStream);
            CurrentStream.Seek(0);
            if (cropper.Source is not WriteableBitmap) cropper.Source = new WriteableBitmap(500, 500);
            await cropper.Source.SetSourceAsync(CurrentStream);
            cropper.Reset();
        }
        else
        {
            if (image.Source is not BitmapImage bmi)
            {
                image.Source = bmi = new BitmapImage();
            }
            CurrentStream.Seek(0);
            await bmi.SetSourceAsync(CurrentStream);
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
            if (IsCropperEnebled)
            {
                await cropper.SaveAsync(stream, ImageManager.GetBitmapFileFormatFromExtension(Path.GetExtension(file.Path)) ?? BitmapFileFormat.Png);
                //await Helper.Functions.ResizeImage(CurrentStream, stream, uint.MaxValue, cropper.CroppedRegion,
                //    async () => await RandomAccessStream.CopyAsync(CurrentStream, stream), Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId);
            }
            else
            {
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
            if (IsCropperEnebled)
            {
                var ms = new InMemoryRandomAccessStream();
                await cropper.SaveAsync(ms, BitmapFileFormat.Png);
                package.SetBitmap(RandomAccessStreamReference.CreateFromStream(ms));
            }
            else
            {
                package.SetBitmap(RandomAccessStreamReference.CreateFromStream(CurrentStream));
            }
            Clipboard.SetContent(package);
        }
        catch { }
    }

    private void ToggleCropper()
    {
        IsCropperEnebled = !IsCropperEnebled;
    }
}
