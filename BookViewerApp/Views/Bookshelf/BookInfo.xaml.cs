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

namespace BookViewerApp.Views.Bookshelf
{
    public sealed partial class BookInfo : UserControl
    {
        public UIElement ShadowTarget => BookMain.ShadowTarget;

        private double _BookHeight = 0;

        public double BookHeight
        {
            get { return _BookHeight; }
            set { _BookHeight = value; InvalidateArrange(); }
        }


        public BookInfo()
        {
            this.InitializeComponent();

            this.DataContext = new ViewModels.Bookshelf2BookViewModel();

            BookMain.Translation = new System.Numerics.Vector3(0, 0, 32);
        }

        private void BookMain_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            BookMain.Translation = new System.Numerics.Vector3(0, 0, 64);
        }

        private void BookMain_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            BookMain.Translation = new System.Numerics.Vector3(0, 0, 32);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(base.MeasureOverride(finalSize));
            //return base.ArrangeOverride(finalSize);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);

            //BookMainの幅に全体を合わせます。
            if (!double.IsPositiveInfinity(availableSize.Height) || BookHeight <= 0) return base.MeasureOverride(availableSize);
            BookMain.Measure(new Size(Math.Min(availableSize.Width, this.MaxWidth), BookHeight));
            return base.MeasureOverride(new Size(Math.Max(BookMain.DesiredSize.Width, MinWidth), double.PositiveInfinity));
            //return base.MeasureOverride(availableSize);
        }

        public event TappedEventHandler BookTapped;

        private void BookMain_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BookTapped?.Invoke(this, e);
        }
    }
}
