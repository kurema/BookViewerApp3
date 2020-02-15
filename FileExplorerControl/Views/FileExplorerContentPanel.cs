using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;


// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views
{
    public class FileExplorerContentPanel : Panel
    {
        public ViewModels.ContentViewModel.ContentStyles ContentStyle { get; set; }

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

            {
                var max = Children.Aggregate(Size.Empty, (a, b) => new Size(Math.Max(a.Width, b.DesiredSize.Width), Math.Max(a.Height, b.DesiredSize.Height)));
                int cCnt = (int)Math.Max(Math.Floor(finalSize.Width / Math.Max(max.Width, 1)), 1);
                if (Children.Count < cCnt)
                {
                    double wscale = (max.Width * cCnt) / Math.Min(finalSize.Width, max.Width * cCnt);
                    for (int i = 0; i < Children.Count; i++)
                    {
                        Children[i].Arrange(new Rect(max.Width * wscale * i, 0, max.Width * wscale, max.Height));
                    }
                    if (double.IsPositiveInfinity(finalSize.Height))
                    {
                        return new Size(finalSize.Width, max.Height);
                    }
                    else
                    {
                        return new Size(finalSize.Width, Math.Max(finalSize.Height, max.Height));
                    }
                }
                else
                {
                    double y = 0;

                    for (int i = 0; ;)
                    {
                        double maxHeight = 0;
                        for (int j = 0; j < cCnt && i + j < Children.Count; j++)
                        {
                            maxHeight = Math.Max(maxHeight, Children[i + j].DesiredSize.Height);
                        }

                        for (int j = 0; j < cCnt; j++)
                        {
                            if (i + j >= Children.Count)
                            {
                                y += maxHeight;
                                goto OutOfFor;
                            }
                            Children[i + j].Arrange(new Rect(finalSize.Width / cCnt * j, y, finalSize.Width / cCnt, maxHeight));
                        }
                        i += cCnt;
                        y += maxHeight;
                    }

                OutOfFor:;
                    if (double.IsPositiveInfinity(finalSize.Height))
                    {
                        return new Size(finalSize.Width, y);
                    }
                    return new Size(finalSize.Width, Math.Max(finalSize.Height, y));
                }
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}
