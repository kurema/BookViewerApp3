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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class BookFixed2Page : UserControl
    {
        public BookFixed2Page()
        {
            this.InitializeComponent();
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grid.Width = this.ActualWidth;
            grid.Height = this.ActualHeight;

            (DataContext as BookFixed2ViewModels.PageViewModel)?.UpdateSource();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            scrollViewer.ChangeView(0, 0, 1.0f);

            (DataContext as BookFixed2ViewModels.PageViewModel)?.UpdateSource();
        }

        private void scrollViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            (DataContext as BookFixed2ViewModels.PageViewModel)?.UpdateSource();
        }

        private double InitialHorizontalOffset;
        private double InitialVerticalOffset;
        private Windows.UI.Input.PointerPoint InitialPoint;

        private void scrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            InitialHorizontalOffset = scrollViewer.HorizontalOffset;
            InitialVerticalOffset = scrollViewer.VerticalOffset;
            InitialPoint = e.GetCurrentPoint(scrollViewer);

        }

        private void scrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.Pointer.IsInContact)
            {
                var point = e.GetCurrentPoint(scrollViewer);
                scrollViewer.ChangeView(InitialHorizontalOffset - (point.Position.X - InitialPoint.Position.X), InitialVerticalOffset - (point.Position.Y - InitialPoint.Position.Y), null);
                e.Handled = true;
            }
        }
    }
}
