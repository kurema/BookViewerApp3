using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace BookViewerApp.Views.BookShelf
{
    public class BookRowPanel:Panel
    {
        private Size _UnitSize;
        public Size UnitSize { get => _UnitSize; set { _UnitSize = value;this.InvalidateArrange(); } }

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
            foreach (var item in Children)
            {
                item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}
