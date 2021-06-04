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
using Windows.UI.Xaml.Media.Imaging;

namespace BookViewerApp.Views
{
    public class SpreadPagePanel : Panel
    {
        public static readonly DependencyProperty Source1Property = DependencyProperty.Register(
            nameof(Source1),
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null, (s, e) =>
            {
                if (e.OldValue != e.NewValue) (s as SpreadPagePanel)?.SetChildrenCache(0, e.NewValue as ImageSource);
                OnSourceChanged(s, e);
            })
            );

        public ImageSource Source1
        {
            get
            {
                return (ImageSource)GetValue(Source1Property);
            }
            set
            {
                SetValue(Source1Property, value);
            }
        }

        public void SetChildrenCache(int count, ImageSource source)
        {
            if (ChildrenCache.Count() > count && ChildrenCache[count] != null) ChildrenCache[count].Source = source;
            //if (Children.Count > count && Children[count] is Image image) image.Source = source;
        }

        public static readonly DependencyProperty Source2Property = DependencyProperty.Register(
            nameof(Source2),
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null, (s, e) =>
            {
                if (e.OldValue != e.NewValue) (s as SpreadPagePanel)?.SetChildrenCache(1, e.NewValue as ImageSource);
                OnSourceChanged(s, e);
            })
            );

        public ImageSource Source2
        {
            get
            {
                return (ImageSource)GetValue(Source2Property);
            }
            set
            {
                SetValue(Source2Property, value);
            }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(ModeEnum),
            typeof(SpreadPagePanel),
            new PropertyMetadata(ModeEnum.Default, new PropertyChangedCallback((s, e) =>
            {
                if (e.OldValue != e.NewValue) (s as SpreadPagePanel)?.InvalidateArrange();
            }))
            );

        public ModeEnum Mode
        {
            get => (ModeEnum)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public ModeOverrideEnum ModeOverride
        {
            get { return (ModeOverrideEnum)GetValue(ModeOverrideProperty); }
            set { SetValue(ModeOverrideProperty, value); }
        }

        public static readonly DependencyProperty ModeOverrideProperty =
            DependencyProperty.Register(nameof(ModeOverride), typeof(ModeOverrideEnum), typeof(SpreadPagePanel), new PropertyMetadata(ModeOverrideEnum.Default,
                new PropertyChangedCallback((s, e) => { if (e.NewValue != e.OldValue) (s as SpreadPagePanel)?.InvalidateArrange(); })));


        public enum ModeEnum
        {
            Spread, Single, Default, ForceSpread, ForceSpreadFirstSingle
        }

        public enum ModeOverrideEnum
        {
            Default, ForceHalfFirst, ForceHalfSecond, ForceSingle
        }

        public static readonly DependencyProperty DisplayedStatusProperty = DependencyProperty.Register(
            nameof(DisplayedStatus),
            typeof(DisplayedStatusEnum),
            typeof(SpreadPagePanel),
            new PropertyMetadata(DisplayedStatusEnum.Single)
            );

        public DisplayedStatusEnum DisplayedStatus
        {
            get => (DisplayedStatusEnum)GetValue(DisplayedStatusProperty);
            set => SetValue(DisplayedStatusProperty, value);
        }

        public enum DisplayedStatusEnum
        {
            Single, Spread, HalfFirst, HalfSecond
        }

        protected Image[] ChildrenCache;

        public SpreadPagePanel()
        {
            ChildrenCache = new[] { GetImage(), GetImage() };
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            void UpdateSource(Image imageArg, ImageSource source)
            {
                if (imageArg.Source != source) imageArg.Source = source;
            }
            Image[] DesireChild(int count)
            {
                var cCount = Children.Count;

                if (count > ChildrenCache.Length) Array.Resize(ref ChildrenCache, count);

                for (int i = 0; i < Math.Min(cCount, count); i++)
                {
                    if (!(Children[i] is Image))
                    {
                        Children[i] = ChildrenCache[i] = ChildrenCache[i] ?? GetImage();
                    }
                }
                for (int i = cCount; i < count; i++)
                {
                    Children.Add(ChildrenCache[i] = ChildrenCache[i] ?? GetImage());
                }
                for (int i = count; i < cCount; i++)
                {
                    Children.RemoveAt(Children.Count - 1);
                }
                foreach (Image image in Children)
                {
                    image.Clip = null;
                    image.Translation = new System.Numerics.Vector3();
                }
                return Children.Select(a => a as Image).ToArray();
            }

            var bmp1 = Source1 as BitmapSource;
            var bmp2 = Source2 as BitmapSource;

            double w1 = bmp1?.PixelWidth ?? 0;
            double h1 = bmp1?.PixelHeight ?? 0;
            double w2 = bmp2?.PixelWidth ?? 0;
            double h2 = bmp2?.PixelHeight ?? 0;
            double w = finalSize.Width;
            double h = finalSize.Height;


            switch (ModeOverride)
            {
                case ModeOverrideEnum.ForceHalfFirst: goto Half;
                case ModeOverrideEnum.ForceHalfSecond: goto HalfSecond;
                case ModeOverrideEnum.ForceSingle: goto Single;
            }

            if (w == 0 || h == 0) return finalSize;

            if (w1 == 0 || h1 == 0)
            {
                goto Conclude;
            }

            switch (Mode)
            {
                case ModeEnum.Default:
                    goto Single;
                case ModeEnum.Spread:
                    break;
                case ModeEnum.Single when w1 > h1:
                    goto Half;
                case ModeEnum.Single:
                    goto Single;
                case ModeEnum.ForceSpread:
                case ModeEnum.ForceSpreadFirstSingle:
                    // ForceSpreadFirstSingle should goto Default if this is first page. But SpreadPagePanel does not know if this page is first or not.
                    // So BookFixed2ViewModels deal with it using ModeOverride;
                    //goto Double;
                    if (w2 == 0 || h2 == 0) goto Single; else goto Double;
                default:
                    break;
            }

            if (w2 == 0 || h2 == 0)
            {
                goto Single;
            }
            else if (w1 > h1 || w2 > h2)
            {
                goto Single;
            }
            else if (w / h > w1 / h1 + w2 / h2)
            {
                goto Double;
            }
            else
            {
                goto Single;
            }

            static void SetupRectangle(ref Windows.UI.Xaml.Shapes.Rectangle rectangleTarget, Rect desiredRect, AlignmentX alignment, BitmapSource source, SpreadPagePanel panel)
            {
                rectangleTarget.Width = desiredRect.Width;
                rectangleTarget.Height = desiredRect.Height;
                {
                    var brush = new ImageBrush()
                    {
                        AlignmentX = alignment,
                        AlignmentY = AlignmentY.Center,
                        ImageSource = source,
                        Stretch = Stretch.UniformToFill
                    };
                    brush.ImageFailed += (s, e) => panel.InvalidateArrange();
                    brush.ImageOpened += (s, e) => panel.InvalidateArrange();
                    rectangleTarget.Fill = brush;
                }

                rectangleTarget.FlowDirection = FlowDirection.LeftToRight;
            }

        Half:
            {
                DisplayedStatus = DisplayedStatusEnum.HalfFirst;

                if (w1 == 0 || h1 == 0) { Children.Clear(); goto Conclude; }

                var rect = GetFilledItemSize(w, h, w1 / 2.0, h1);

                if (Children.Count != 1 || !(Children[0] is Windows.UI.Xaml.Shapes.Rectangle rectangle))
                {
                    Children.Clear();
                    rectangle = new Windows.UI.Xaml.Shapes.Rectangle();
                    Children.Add(rectangle);
                }
                SetupRectangle(ref rectangle, rect, this.FlowDirection switch
                {
                    FlowDirection.LeftToRight => AlignmentX.Left,
                    FlowDirection.RightToLeft => AlignmentX.Right,
                    _ => AlignmentX.Left,
                }, bmp1, this);

                rectangle.Measure(GetSizeFromRect(rect));
                rectangle.Arrange(rect);
            }
            goto Conclude;

        HalfSecond:
            {
                DisplayedStatus = DisplayedStatusEnum.HalfSecond;

                if (w1 == 0 || h1 == 0) { Children.Clear(); goto Conclude; }

                var rect = GetFilledItemSize(w, h, w1 / 2.0, h1);

                if (Children.Count != 1 || !(Children[0] is Windows.UI.Xaml.Shapes.Rectangle rectangle))
                {
                    Children.Clear();
                    rectangle = new Windows.UI.Xaml.Shapes.Rectangle();
                    Children.Add(rectangle);
                }
                SetupRectangle(ref rectangle, rect, this.FlowDirection switch
                {
                    FlowDirection.LeftToRight => AlignmentX.Right,
                    FlowDirection.RightToLeft => AlignmentX.Left,
                    _ => AlignmentX.Right,
                }, bmp1, this);

                rectangle.Measure(GetSizeFromRect(rect));
                rectangle.Arrange(rect);
            }
            goto Conclude;

        Double:
            {
                var wr1 = w1 * h / h1;
                var wr2 = w2 * h / h2;
                var wl = (w - wr1 - wr2) / 2.0;

                var images = DesireChild(2);
                UpdateSource(images[0], bmp1);
                UpdateSource(images[1], bmp2);

                if (wr1 + wr2 < w)
                {
                    images[0].Measure(new Size(wr1, h));
                    images[1].Measure(new Size(wr2, h));

                    images[0].Arrange(new Rect(wl, 0, wr1, h));
                    images[1].Arrange(new Rect(wl + wr1, 0, wr2, h));
                }
                else
                {
                    var wscale = w/ (wr1 + wr2);
                    var top = (1 - wscale) * h / 2.0;

                    images[0].Measure(new Size(wr1*wscale, h * wscale));
                    images[1].Measure(new Size(wr2 * wscale, h * wscale));

                    images[0].Arrange(new Rect(0, top, wr1*wscale, h * wscale));
                    images[1].Arrange(new Rect(wr1*wscale, top, wr2 * wscale, h * wscale));
                }

                DisplayedStatus = DisplayedStatusEnum.Spread;
            }
            goto Conclude;

        Single:
            {
                var image = DesireChild(1)[0];
                UpdateSource(image, bmp1);

                if (w1 > 0 && h1 > 0)
                {
                    var rect = GetFilledItemSize(w, h, w1, h1);
                    image.Measure(GetSizeFromRect(rect));
                    image.Arrange(rect);
                }

                DisplayedStatus = DisplayedStatusEnum.Single;
            }
            goto Conclude;

        Conclude:
            return finalSize;
        }

        private Image GetImage()
        {
            var image = new Image()
            {
                Stretch = Stretch.UniformToFill
            };
            image.ImageOpened += (s, e) => InvalidateArrange();
            image.ImageFailed += (s, e) => InvalidateArrange();
            return image;
        }


        public static Rect GetFilledItemSize(double w, double h, double w1, double h1)
        {
            if (w / h > w1 / h1)
            {
                var wr = w1 * h / h1;//width result
                return new Rect((w - wr) / 2.0, 0, wr, h);
            }
            else
            {
                var hr = h1 * w / w1;
                return new Rect(0, (h - hr) / 2.0, w, hr);
            }
        }

        public static Size GetSizeFromRect(Rect arg)
        {
            return new Size(arg.Width, arg.Height);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var panel = d as SpreadPagePanel;
            if (panel is null) return;

            panel.InvalidateArrange();

            RoutedEventHandler routedEventHandler = (s2, e2) => panel.InvalidateArrange();
            ExceptionRoutedEventHandler exceptionRoutedEventHandler = (s2, e2) => panel.InvalidateArrange();

            var callback = new DependencyPropertyChangedCallback((s, e) => panel.InvalidateArrange());
            {
                if (e.OldValue is BitmapImage bitmap)
                {
                    bitmap.ImageOpened -= routedEventHandler;
                    bitmap.ImageFailed -= exceptionRoutedEventHandler;
                }
            }
            {
                if (e.NewValue is BitmapImage bitmap)
                {
                    bitmap.ImageOpened += routedEventHandler;
                    bitmap.ImageFailed += exceptionRoutedEventHandler;
                }
            }

            //panel
            //https://www.matatabi-ux.com/entry/2015/09/16/120000

            //crop
            //https://stackoverflow.com/questions/40537189/how-to-crop-bitmap-in-uwp-app
            //https://stackoverflow.com/questions/39514853/clipping-image-in-windows-store-app?rq=1
        }
    }
}
