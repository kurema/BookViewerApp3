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

using ef = Microsoft.Graphics.Canvas.Effects;


// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

#nullable enable
namespace BookViewerApp.Views.Bookshelf
{
    public sealed partial class Book : UserControl
    {
        //https://docs.microsoft.com/windows/uwp/design/style/rounded-corner#keyboard-focus-rectangle-and-shadow
        //Shadow ignore corner radius by default. So it doesn't make much sense to use this. But it may in future.
        public UIElement ShadowTarget => BorderMain;

        private double aspect { get; set; } = 0;// Width/Height

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SourceProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(Book), new PropertyMetadata(null));



        public Book()
        {
            this.InitializeComponent();

            //https://stackoverflow.com/questions/41101198/how-to-combine-multiple-effects-in-uwp-composition-api
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return ArrangeOverride(availableSize);
            //return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            static Size ArrangeToStretch(double width, double height, UIElement content)
            {
                var size = new Size(width, height);
                content.Arrange(new Rect(new Point(), size));
                return size;
            }

            if (double.IsNaN(aspect) || aspect <= 0) return base.ArrangeOverride(finalSize);
            if (double.IsNaN(finalSize.Width))
            {
                if (double.IsNaN(finalSize.Height)) return base.ArrangeOverride(finalSize);
                return ArrangeToStretch(finalSize.Height * aspect, finalSize.Height, Content);
            }
            {
                if (double.IsNaN(finalSize.Height)) return new Size(finalSize.Width, finalSize.Width / aspect);
                double w = finalSize.Height * aspect;
                if (w <= finalSize.Width) return new Size(w, finalSize.Height);
                return ArrangeToStretch(finalSize.Width, finalSize.Width / aspect, Content);
            }
            //return base.ArrangeOverride(finalSize);
        }
    }
}
