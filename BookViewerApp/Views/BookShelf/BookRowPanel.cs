using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace BookViewerApp.Views.BookShelf
{
    public class BookRowPanel : Panel
    {
        private Size _UnitSize;
        public Size UnitSize { get => _UnitSize; set { _UnitSize = value; this.InvalidateArrange(); } }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0)
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                return ArrangeOverride(availableSize);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0) return finalSize;
            if (double.IsNaN(finalSize.Width)) return base.ArrangeOverride(finalSize);
            int columnCount = (int)Math.Max(1, Math.Floor(finalSize.Width / UnitSize.Width));
            double columnWidth = finalSize.Width / columnCount;

            foreach (var item in Children)
            {
                item.Measure(new Size(columnWidth, double.PositiveInfinity));
            }
            int r = 0;
            double y = 0;
            while (Children.Count > r * columnCount)
            {
                int i;
                double hMax = double.NegativeInfinity;
                for (int c = 0; c < columnCount && (i = r * columnCount + c) < Children.Count; c++)
                {
                    hMax = Math.Max(Children[i].DesiredSize.Height, hMax);
                }
                for (int c = 0; c < columnCount && (i = r * columnCount + c) < Children.Count; c++)
                {
                    Children[i].Arrange(new Rect(c * columnWidth, y, columnWidth, hMax));
                }
                y += hMax;
                r++;
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}
