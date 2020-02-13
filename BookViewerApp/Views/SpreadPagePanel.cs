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

namespace BookViewerApp
{
    public class SpreadPagePanel : Panel
    {
        public static readonly DependencyProperty Source1Property = DependencyProperty.Register(
            nameof(Source1),
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null,OnSourceChanged)
            );

        public ImageSource Source1
        {
            get { return (ImageSource)this.GetValue(Source1Property); }
            set { this.SetValue(Source1Property, value); }
        }

        public static readonly DependencyProperty Source2Property = DependencyProperty.Register(
            nameof(Source2),
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null, OnSourceChanged)
            );

        public ImageSource Source2
        {
            get { return (ImageSource)this.GetValue(Source2Property); }
            set { this.SetValue(Source2Property, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(ModeEnum),
            typeof(SpreadPagePanel),
            new PropertyMetadata(ModeEnum.Default)
            );

        public ModeEnum Mode
        {
            get => (ModeEnum)this.GetValue(ModeProperty);
            set => this.SetValue(ModeProperty, value);
        }

        public enum ModeEnum
        {
            Spread, Single, Default, ForceHalfFirst, ForceHalfSecond
        }

        public static readonly DependencyProperty DisplayedStatusProperty = DependencyProperty.Register(
            nameof(DisplayedStatus),
            typeof(DisplayedStatusEnum),
            typeof(SpreadPagePanel),
            new PropertyMetadata(DisplayedStatusEnum.Single)
            );

        public DisplayedStatusEnum DisplayedStatus
        {
            get => (DisplayedStatusEnum)this.GetValue(DisplayedStatusProperty);
            set => this.SetValue(DisplayedStatusProperty, value);
        }

        public enum DisplayedStatusEnum
        {
            Single, Spread, HalfFirst, HalfSecond
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            Image GetImage()
            {
                var image= new Image()
                {
                    Stretch = Stretch.UniformToFill
                };
                image.ImageOpened += (s, e) => this.InvalidateArrange();
                image.ImageFailed += (s, e) => this.InvalidateArrange();
                return image;
            }
            void UpdateSource(Image image3,ImageSource source)
            {
                if (image3.Source != source) image3.Source = source;
            }
            void DesireChild(int count)
            {
                var cCount = Children.Count;
                for (int i = 0; i < Math.Min(cCount, count); i++)
                {
                    if (!(Children[i] is Image))
                    {
                        Children[i] = GetImage();
                    }
                }
                for(int i = cCount; i < count; i++)
                {
                    Children.Add(GetImage());
                }
                for(int i = count; i < cCount; i++)
                {
                    Children.RemoveAt(Children.Count - 1);
                }
            }

            var bmp1 = this.Source1 as BitmapSource;
            var bmp2 = this.Source2 as BitmapSource;

            double w1 = bmp1?.PixelWidth ?? 0;
            double h1 = bmp1?.PixelHeight ?? 0;
            double w2 = bmp2?.PixelWidth ?? 0;
            double h2 = bmp2?.PixelHeight ?? 0;
            double w = finalSize.Width;
            double h = finalSize.Height;

            if (w == 0 || h == 0) return finalSize;

            if (Mode == ModeEnum.Single) throw new NotImplementedException();
            if (bmp1 == null || w1 == 0 || h1 == 0)
            {
                goto Conclude;
            }
            else if (this.Mode == ModeEnum.Default)
            {
                goto Single;
            }
            else if (this.Mode == ModeEnum.ForceHalfFirst)
            {
                goto Half;
            }
            else if (this.Mode==ModeEnum.ForceHalfSecond)
            {
                goto HalfSecond;
            }
            else if (this.Mode==ModeEnum.Single && w1 > h1)
            {
                goto Half;
            }
            else if (bmp2 == null || w2 == 0 || h2 == 0)
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

        Half:
            DesireChild(1);
            throw new NotImplementedException();
            DisplayedStatus = DisplayedStatusEnum.HalfFirst;
            goto Conclude;

        HalfSecond:
            throw new NotImplementedException();
            DisplayedStatus = DisplayedStatusEnum.HalfSecond;
            goto Conclude;

        Double:
            {
                var wr1 = w1 * h / h1;
                var wr2 = w2 * h / h2;
                var wl = (w - wr1 - wr2) / 2.0;

                DesireChild(2);
                var image1 = this.Children[0] as Image;
                UpdateSource(image1, bmp1);
                var image2 = this.Children[1] as Image;
                UpdateSource(image2, bmp2);

                image1.Measure(new Size(wr1, h));
                image2.Measure(new Size(wr2, h));

                image1.Arrange(new Rect(wl, 0, wr1, h));
                image2.Arrange(new Rect(wl + wr1, 0, wr2, h));

                DisplayedStatus = DisplayedStatusEnum.Spread;
            }
            goto Conclude;

        Single:
            {
                DesireChild(1);
                var image = this.Children[0] as Image;
                UpdateSource(image, bmp1);

                if (w1 == 0 || h1 == 0) goto Conclude;

                if (w / h > w1 / h1)
                {
                    var wr = w1 * h / h1;//width result
                    image.Measure(new Size(wr, h));
                    image.Arrange(new Rect((w - wr) / 2.0, 0, wr, h));
                }
                else
                {
                    var hr = h1 * w / w1;
                    image.Measure(new Size(w, hr));
                    image.Arrange(new Rect(0, (h - hr) / 2.0, w, hr));
                }

                DisplayedStatus = DisplayedStatusEnum.Single;
            }
            goto Conclude;

        Conclude:
            return finalSize;
        }


        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var panel = d as SpreadPagePanel;
            if (panel == null)
            {
                return;
            }


            RoutedEventHandler routedEventHandler = (s2, e2) => panel.InvalidateArrange();
            ExceptionRoutedEventHandler exceptionRoutedEventHandler= (s2, e2) => panel.InvalidateArrange();

            {
                if (e.OldValue is BitmapImage bitmap) {
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

            //Image GetImage(ImageSource source)
            //{
            //    var image = new Image() { Stretch = Stretch.UniformToFill };
            //    image.Source = source;
            //    image.ImageOpened += (s2, e2) => panel.InvalidateArrange();
            //    image.ImageFailed += (s2, e2) => panel.InvalidateArrange();
            //    return image;
            //}
            //panel.Children.Clear();
            //panel.Children.Add(GetImage(panel.Source1));
            //panel.Children.Add(GetImage(panel.Source2));

            //panel.Children.Add(img1);
            //panel.Children.Add(img2);

            //panel
            //https://www.matatabi-ux.com/entry/2015/09/16/120000

            //crop
            //https://stackoverflow.com/questions/40537189/how-to-crop-bitmap-in-uwp-app
            //https://stackoverflow.com/questions/39514853/clipping-image-in-windows-store-app?rq=1
        }
    }
}
