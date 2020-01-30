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

namespace BookViewerApp.Views
{
    public class SpreadPagePanel : Panel
    {
        public static readonly DependencyProperty Source1Property = DependencyProperty.Register(
            "Source1",
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null)
            );

        public ImageSource Source1
        {
            get { return (ImageSource)this.GetValue(Source1Property); }
            set { this.SetValue(Source1Property, value); }
        }

        public static readonly DependencyProperty Source2Property = DependencyProperty.Register(
            "Source2",
            typeof(ImageSource),
            typeof(SpreadPagePanel),
            new PropertyMetadata(null)
            );

        public ImageSource Source2
        {
            get { return (ImageSource)this.GetValue(Source2Property); }
            set { this.SetValue(Source2Property, value); }
        }

        private Image[] sourceImages;

        protected override Size ArrangeOverride(Size finalSize)
        {
            throw new NotImplementedException();
            return base.ArrangeOverride(finalSize);
        }


        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as SpreadPagePanel;
            if (panel == null)
            {
                return;
            }
            var img1 = new Image
            {
                Source = panel.Source1,
                Stretch = Stretch.UniformToFill
            };
            var img2 = new Image
            {
                Source = panel.Source1,
                Stretch = Stretch.UniformToFill
            };

            //img1.ImageOpened += panel.OnSourceImageOpened;
            //img1.ImageFailed += panel.OnSourceImageFailed;
            //img2.ImageOpened += panel.OnSourceImageOpened;
            //img2.ImageFailed += panel.OnSourceImageFailed;
            panel.sourceImages = new[] { img1, img2 };

            panel.Children.Add(img1);
            panel.Children.Add(img2);

            //panel
            //https://www.matatabi-ux.com/entry/2015/09/16/120000

            //crop
            //https://stackoverflow.com/questions/40537189/how-to-crop-bitmap-in-uwp-app
            //https://stackoverflow.com/questions/39514853/clipping-image-in-windows-store-app?rq=1
        }
    }
}
