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
        public int MaxLine
        {
            get { return (int)GetValue(MaxLineProperty); }
            set { SetValue(MaxLineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxLine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxLineProperty =
            DependencyProperty.Register("MaxLine", typeof(int), typeof(BookRowPanel), new PropertyMetadata(1));

        public bool AllowOverflow
        {
            get { return (bool)GetValue(AllowOverflowProperty); }
            set { SetValue(AllowOverflowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowOverflow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowOverflowProperty =
            DependencyProperty.Register("AllowOverflow", typeof(bool), typeof(BookRowPanel), new PropertyMetadata(false));

        public Size Spacing
        {
            get { return (Size)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Spacing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing", typeof(Size), typeof(BookRowPanel), new PropertyMetadata(new Size(0, 0)));



        public bool HasCollapsedItem
        {
            get { return (bool)GetValue(HasCollapsedItemProperty); }
            set { if (HasCollapsedItem != value) SetValue(HasCollapsedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasCollapsedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasCollapsedItemProperty =
            DependencyProperty.Register("HasCollapsedItem", typeof(bool), typeof(BookRowPanel), new PropertyMetadata(false));


        public UIElement[] ShadowTargets =>
            Children.Select(
                a =>
                {
                    //if (a is ContentPresenter cp && cp.Content is BookInfo bi) return bi;
                    if (a is BookInfo bi2) return bi2;
                    return null;
                }
                ).Where(a => !(a is null)).Select(a => a.ShadowTarget).ToArray();
        //Children.OfType<BookInfo>().Select(a => a.ShadowTarget).ToArray();

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
            double wmax = 0;
            int line = 0;
            int i = 0;
            bool collapsed = false;

            while (i < Children.Count)
            {
                var child = Children[i];
                collapsed = collapsed || (AllowOverflow && x + child.DesiredSize.Width > finalSize.Width);
                if (x + (AllowOverflow ? 0 : child.DesiredSize.Width) > finalSize.Width && x > 0)
                {
                    if (line + 1 >= MaxLine)
                    {
                        for (int j = i; j < Children.Count; j++)
                        {
                            collapsed = true;
                            //Is this fine? I doubt...
                            Children[j].Arrange(new Rect(0, 0, 0, 0));
                        }
                        break;
                    }
                    wmax = Math.Max(wmax, x - Spacing.Width);
                    x = 0;
                    y += hmax;
                    y += Spacing.Height;
                    hmax = 0;
                }
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                hmax = Math.Max(hmax, child.DesiredSize.Height);
                x += child.DesiredSize.Width;
                x += Spacing.Width;
                i++;
            }
            HasCollapsedItem = collapsed;
            return new Size(Math.Min(Math.Max(wmax, Math.Max(0, x - Spacing.Width)), finalSize.Width), hmax + y);
        }
    }
}
