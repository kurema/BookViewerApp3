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
namespace BookViewerApp.Views.BookShelf
{
    public sealed partial class Book : UserControl
    {
        private double aspect { get; set; }// Width/Height

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

            if (double.IsNaN(aspect) || aspect <= 0) return base.ArrangeOverride(finalSize); ;
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
