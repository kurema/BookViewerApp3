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
namespace BookViewerApp.Views.Bookshelf;

public sealed partial class Book : UserControl
{
    //https://docs.microsoft.com/windows/uwp/design/style/rounded-corner#keyboard-focus-rectangle-and-shadow
    //Shadow ignore corner radius by default. So it doesn't make much sense to use this. But it may in future.
    public UIElement ShadowTarget => BorderMain;

    public double Aspect
    {
        get { return (double)GetValue(AspectProperty); }
        set { SetValue(AspectProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Aspect.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AspectProperty =
        DependencyProperty.Register("Aspect", typeof(double), typeof(Book), new PropertyMetadata(0));

    public ImageSource Source
    {
        get { return (ImageSource)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SourceProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(Book), new PropertyMetadata(null, new PropertyChangedCallback(SourcePropertyChangedCallback)));

    static private void SourcePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is not Book db) return;
        if (args.NewValue is Windows.UI.Xaml.Media.Imaging.BitmapSource sn)
        {
            db.Aspect = GetAspect(sn);
            db.InvalidateMeasure();
        }
        else { db.Aspect = 0; }
    }

    private static double GetAspect(Windows.UI.Xaml.Media.Imaging.BitmapSource? source) => source is null || source.PixelWidth == 0 || source.PixelHeight == 0 ? 0 : source.PixelWidth / (double)source.PixelHeight;

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

        double aspect = Aspect;
        if (double.IsNaN(Aspect) || Aspect <= 0) aspect = Math.Sqrt(0.5);
        if (double.IsNaN(finalSize.Width))
        {
            if (double.IsNaN(finalSize.Height)) return base.ArrangeOverride(finalSize);
            return ArrangeToStretch(finalSize.Height * aspect, finalSize.Height, Content);
        }
        {
            if (double.IsNaN(finalSize.Height)) return new Size(finalSize.Width, finalSize.Width / aspect);
            double w = finalSize.Height * aspect;
            if (w <= finalSize.Width) return ArrangeToStretch(w, finalSize.Height, Content);
            return ArrangeToStretch(finalSize.Width, finalSize.Width / aspect, Content);
        }
        //return base.ArrangeOverride(finalSize);
    }

    private void mainPicture_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var aspect = GetAspect((sender as Image)?.Source as Windows.UI.Xaml.Media.Imaging.BitmapSource);
        if (aspect != Aspect)
        {
            Aspect = aspect;
            FrameworkElement? element = this;
            for (int i = 0; i < 3 && element is not null; i++)
            {
                element.InvalidateMeasure();
                element = element.Parent as FrameworkElement;
            }
        }
    }
}
