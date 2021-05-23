using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml;


namespace BookViewerApp.Views.Bookshelf
{
    public class BookRowPanel : Panel
    {
        public UIElement[] ShadowTargets => Children.OfType<BookInfo>().Select(a => a.ShadowTarget).ToArray();

        protected override Size MeasureOverride(Size availableSize)
        {
            return ArrangeOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var item in Children) item.Measure(finalSize);
            double y = 0;
            double x = 0;
            double hmax = 0;
            int i = 0;
            while (x < finalSize.Width && i<Children.Count)
            {
                var child = Children[i];
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                hmax = Math.Max(hmax, child.DesiredSize.Height);
                x += child.DesiredSize.Width;
                i++;
            }
            return new Size(x, hmax + y);
        }
    }
}
