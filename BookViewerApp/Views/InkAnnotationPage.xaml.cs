using BookViewerApp.Helper;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class InkAnnotationPage : Page
{
    public IRandomAccessStream CurrentStream { get; private set; }

    public async Task SetSouceAsync(IRandomAccessStream stream)
    {
        try
        {
            if (inkCanvasBackground.Source is not BitmapImage bmi)
            {
                inkCanvasBackground.Source = bmi = new BitmapImage();
            }
            stream.Seek(0);
            await bmi.SetSourceAsync(stream);
            CurrentStream = stream;
        }
        catch { }
    }

    public async Task<CanvasRenderTarget> GetCanvasRenderTarget()
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

    public void Clear()
    {
        inkCanvas.InkPresenter.StrokeContainer.Clear();
    }

    public InkAnnotationPage()
    {
        this.InitializeComponent();

        inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen;
    }

    private bool TouchToDraw
    {
        get => inkCanvas.InkPresenter.InputDeviceTypes.HasFlag(Windows.UI.Core.CoreInputDeviceTypes.Touch);
        set
        {
            inkCanvas.InkPresenter.InputDeviceTypes = value ?
            inkCanvas.InkPresenter.InputDeviceTypes | Windows.UI.Core.CoreInputDeviceTypes.Touch
            : inkCanvas.InkPresenter.InputDeviceTypes & ~Windows.UI.Core.CoreInputDeviceTypes.Touch;
        }
    }

    private void Zoom(float x)
    {
        UIHelper.ChangeViewWithKeepCurrentCenter(inkScrollViewer, inkScrollViewer.ZoomFactor * x);
    }

    private void Button_Click_ZoomIn(object sender, RoutedEventArgs e) => Zoom(1.1f);

    private void Button_Click_ZoomOut(object sender, RoutedEventArgs e) => Zoom(1 / 1.1f);

    private void inkScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (inkParent.Width == 0 || inkParent.Height == 0 || double.IsNaN(inkParent.Width) || double.IsNaN(inkParent.Height)) return;
        if (inkCanvasBackground.Source is not BitmapImage bmi) return;
        float factor = (float)Math.Min(inkScrollViewer.ViewportWidth / bmi.PixelWidth, inkScrollViewer.ViewportHeight / bmi.PixelHeight);
        if (double.IsNaN(factor)) return;
        inkScrollViewer.MinZoomFactor = factor;
    }

    public event EventHandler Accepted;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Accepted?.Invoke(this, new EventArgs());
    }

    private void inkCanvasBackground_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (inkCanvasBackground.Source is not BitmapImage bmi) return;
        inkParent.Width = bmi.PixelWidth;
        inkParent.Height = bmi.PixelHeight;
        float factor = (float)Math.Min(inkScrollViewer.ViewportWidth / bmi.PixelWidth, inkScrollViewer.ViewportHeight / bmi.PixelHeight);
        inkScrollViewer.ChangeView(null, null, factor, true);
        inkScrollViewer.MinZoomFactor = factor;
    }
}
