using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BookViewerApp.ViewModels;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp.Views;

public sealed partial class BookFixed2Page : UserControl, INotifyPropertyChanged
{
    public void OnPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    public event PropertyChangedEventHandler PropertyChanged;
    public float? ZoomFactor
    {
        get { return scrollViewer.ZoomFactor; }
        set
        {
            if (!value.HasValue) return;
            var resultFactor = Math.Max(Math.Min(scrollViewer.MaxZoomFactor, value.Value), scrollViewer.MinZoomFactor);
            if (value != resultFactor) { throw new ArgumentOutOfRangeException(); }
            scrollViewer.ChangeView(null, null, resultFactor);
            //changeViewWithKeepCurrentCenter(scrollViewer, resultFactor);
            OnPropertyChanged(nameof(ZoomFactor));
        }
    }

    public BookFixed2Page()
    {
        this.InitializeComponent();

        this.DataContextChanged += (s3, e3) =>
        {
            this.scrollViewer.ViewChanged += (s2, e2) =>
            {
                if (this.DataContext is PageViewModel pvm)
                    pvm.ZoomFactor = scrollViewer.ZoomFactor;
            };
            if (this.DataContext is PageViewModel pv)
            {
                pv.ZoomRequested += (s2, e2) =>
                {
                    if (e2.ZoomFactor != null)
                    {
                        var value = e2.ZoomFactor;
                        var resultFactor = Math.Max(Math.Min(scrollViewer.MaxZoomFactor, value ?? 1),
                            scrollViewer.MinZoomFactor);

                        var currentFactor = this.ZoomFactor;
                        changeViewWithKeepCurrentCenter(scrollViewer, resultFactor);
                            //ズーム時は移動量は無視される。
                            //You can't zoom and move.
                        }
                    else
                    {
                            //移動量は表示範囲に対する割合で。
                            //offset is rate of viewport.
                            var viewportMin = Math.Min(scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        int direction = scrollViewer.FlowDirection switch
                        {
                            FlowDirection.LeftToRight => 1,
                            FlowDirection.RightToLeft => -1,
                            _ => 1,
                        };

                        scrollViewer.ChangeView(scrollViewer.HorizontalOffset + viewportMin * e2.MoveHorizontal * direction, scrollViewer.VerticalOffset - viewportMin * e2.MoveVertical, null, true);
                    }
                };
                pv.PropertyChanged += (s2, e2) =>
                {
                    if (e2.PropertyName == nameof(PageViewModel.ZoomFactor))
                    {
                        try
                        {
                            this.ZoomFactor = (this.DataContext as PageViewModel)?.ZoomFactor;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            if (this.ZoomFactor.HasValue)
                                pv.ZoomFactor = this.ZoomFactor.Value;
                        }
                    }
                };
            }
        };
    }

    private void changeViewWithKeepCurrentCenter(ScrollViewer sv, float zoomFactor)
    {
        double originalCenterX;
        if (sv.ViewportWidth < sv.ExtentWidth)
        {
            double eCenterX = sv.HorizontalOffset + sv.ViewportWidth / 2;
            originalCenterX = eCenterX / sv.ZoomFactor;
        }
        else
        {
            double eCenterX = sv.HorizontalOffset + sv.ExtentWidth / 2;
            originalCenterX = eCenterX / sv.ZoomFactor;
        }

        double originalCenterY;
        if (sv.ViewportHeight < sv.ExtentHeight)
        {
            double eCenterY = sv.VerticalOffset + sv.ViewportHeight / 2;
            originalCenterY = eCenterY / sv.ZoomFactor;
        }
        else
        {
            double eCenterY = sv.VerticalOffset + sv.ExtentHeight / 2;
            originalCenterY = eCenterY / sv.ZoomFactor;
        }


        double newExtentCenterX = originalCenterX * zoomFactor;
        double newExtentCenterY = originalCenterY * zoomFactor;

        double newExtentOffsetX = newExtentCenterX - sv.ViewportWidth / 2;
        double newExtentOffsetY = newExtentCenterY - sv.ViewportHeight / 2;
        sv.ChangeView(newExtentOffsetX, newExtentOffsetY, zoomFactor, true);
    }


    private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        grid.Width = this.ActualWidth;
        grid.Height = this.ActualHeight;

        spreadPanel.Width = this.ActualWidth;
        spreadPanel.Height = this.ActualHeight;

        UpdateSourceIfRequired();

        if (!(DataContext is PageViewModel pv)) return;
        spreadPanel.InvalidateArrange();
        if (spreadPanel.ActualWidth == 0 || spreadPanel.ActualHeight == 0) pv.AspectCanvas = -1;
        else pv.AspectCanvas = spreadPanel.ActualWidth / spreadPanel.ActualHeight;
    }

    private async void UpdateSourceIfRequired()
    {
        if (!(DataContext is PageViewModel pv)) return;

        if (await pv?.Content?.UpdateRequiredAsync(this.ActualWidth, this.ActualHeight) == true)
        {
            {
                UpdateCancellationTokenSource(ref CancellationTokenSource1);
                pv?.SetImageNoWait(spreadPanel.Source1 as Windows.UI.Xaml.Media.Imaging.BitmapImage, CancellationTokenSource1.Token, Semaphore1, this.ActualWidth, this.ActualHeight);
            }
            var spreadMode = (this.DataContext as PageViewModel)?.Parent?.SpreadMode;
            if (spreadMode == SpreadPagePanel.ModeEnum.Spread || spreadMode == SpreadPagePanel.ModeEnum.ForceSpread || spreadMode == SpreadPagePanel.ModeEnum.ForceSpreadFirstSingle
                || spreadPanel.Mode == SpreadPagePanel.ModeEnum.Spread)
            {
                UpdateCancellationTokenSource(ref CancellationTokenSource2);
                pv?.NextPage?.SetImageNoWait(spreadPanel.Source2 as Windows.UI.Xaml.Media.Imaging.BitmapImage, CancellationTokenSource2.Token, Semaphore2, this.ActualWidth, this.ActualHeight);
            }
        }
    }

    private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        scrollViewer.ChangeView(0, 0, 1.0f, true);

        if (args.NewValue is PageViewModel dc)
        {
            //This caused problem.
            //void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
            //{
            //    if (e.PropertyName == nameof(dc.Parent.SpreadMode))
            //    {
            //        UpdateSource1();
            //        UpdateSource2();
            //        spreadPanel.InvalidateArrange();
            //    }
            //}

            void UpdateSource2()
            {
                var spreadMode = dc?.Parent?.SpreadMode;
                if (spreadMode == SpreadPagePanel.ModeEnum.Spread || spreadMode == SpreadPagePanel.ModeEnum.ForceSpread || spreadMode == SpreadPagePanel.ModeEnum.ForceSpreadFirstSingle ||
                    spreadPanel.Mode == SpreadPagePanel.ModeEnum.Spread)
                {

                    if (dc.NextPage is null) spreadPanel.Source2 = null;
                    else
                    {
                        UpdateCancellationTokenSource(ref CancellationTokenSource2);
                        if (!(spreadPanel.Source2 is Windows.UI.Xaml.Media.Imaging.BitmapImage source2)) spreadPanel.Source2 = source2 = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        dc.NextPage.SetImageNoWait(source2, CancellationTokenSource2.Token, Semaphore2, this.ActualWidth, this.ActualHeight);
                    }
                }
            }

            void UpdateSource1()
            {
                UpdateCancellationTokenSource(ref CancellationTokenSource1);
                //Note: dc.Sourceを使うと複数ページに同じBitmapSourceが適用されるバグがあった。
                if (!(spreadPanel.Source1 is Windows.UI.Xaml.Media.Imaging.BitmapImage)) spreadPanel.Source1 = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                dc.SetImageNoWait(spreadPanel.Source1 as Windows.UI.Xaml.Media.Imaging.BitmapImage, CancellationTokenSource1.Token, Semaphore1, this.ActualWidth, this.ActualHeight);
            }

            UpdateSource1();
            UpdateSource2();

            if (dc.Parent != null && dc.Parent != UserControl_DataContextChanged_CurrentParent)
            {
                //if (UserControl_DataContextChanged_CurrentParent != null) UserControl_DataContextChanged_CurrentParent.PropertyChanged -= Parent_PropertyChanged;
                //dc.Parent.PropertyChanged += Parent_PropertyChanged;
                UserControl_DataContextChanged_CurrentParent = dc.Parent;
            }
        }
    }

    private BookViewModel UserControl_DataContextChanged_CurrentParent = null;

    private System.Threading.CancellationTokenSource CancellationTokenSource1;
    private System.Threading.CancellationTokenSource CancellationTokenSource2;
    private System.Threading.SemaphoreSlim Semaphore1 = new System.Threading.SemaphoreSlim(1, 1);
    private System.Threading.SemaphoreSlim Semaphore2 = new System.Threading.SemaphoreSlim(1, 1);

    private void UpdateCancellationTokenSource(ref System.Threading.CancellationTokenSource tokenSource)
    {
        tokenSource?.Cancel();
        tokenSource?.Dispose();
        tokenSource = new System.Threading.CancellationTokenSource();
    }

    private double _initialHorizontalOffset;
    private double _initialVerticalOffset;
    private Windows.UI.Input.PointerPoint _initialPoint;
    private DateTime _lastClickedTime;

    private void scrollViewer_PointerInit(PointerRoutedEventArgs e)
    {
        _initialHorizontalOffset = scrollViewer.HorizontalOffset;
        _initialVerticalOffset = scrollViewer.VerticalOffset;
        _initialPoint = e.GetCurrentPoint(scrollViewer);
        _lastClickedTime = DateTime.Now;
    }

    private void scrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        scrollViewer_PointerInit(e);
    }

    private void scrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer?.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse &&
            e.Pointer?.IsInContact == true)
        {
            //Workaround. PointerPressed not fired in some case.
            if ((DateTime.Now - _lastClickedTime).TotalSeconds > 1) { scrollViewer_PointerInit(e); }

            var point = e.GetCurrentPoint(scrollViewer);
            if (point == null || _initialPoint is null) return;
            scrollViewer.ChangeView(_initialHorizontalOffset - (point.Position.X - _initialPoint.Position.X),
                _initialVerticalOffset - (point.Position.Y - _initialPoint.Position.Y), null);
            e.Handled = true;

            _lastClickedTime = DateTime.Now;
        }
        else
        {
            e.Handled = false;
        }
    }

    private async void ScrollViewer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

        if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
        {
            //これなんでだっけ？
            await Task.Delay(100);
        }
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer is null) return;

        var doubleTapPoint = e.GetPosition(scrollViewer);
        if (scrollViewer.ZoomFactor != 1.0)
        {
            scrollViewer.ChangeView(null, null, 1);
        }
        else
        {
            scrollViewer.ChangeView(doubleTapPoint.X, doubleTapPoint.Y, 2);
        }
        e.Handled = true;
    }

}
