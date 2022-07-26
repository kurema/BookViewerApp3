using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace BookViewerApp.Views;
public class FlipViewEx : FlipView
{
    public FlipViewEx()
    {
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PreviousButtonHorizontal") is Button prev) prev.Opacity = 0.5;
        if (GetTemplateChild("NextButtonHorizontal") is Button next) next.Opacity = 0.5;
    }

    public void ScrollHorizontal(float offset)
    {
        if (GetTemplateChild("ScrollingHost") is ScrollViewer scroll)
        {
            if(scroll.Content is UIElement content)
            {
                //メモ：
                //マウス移動分Translationを調整すれば良いんだけど、Releaseした時どうすんの？
                //自前アニメーションは正直怠い。
                //アニメーション省略は違和感が大きい。
                //明日自前アニメでやるか…。
                content.Translation = new System.Numerics.Vector3(offset, 0, 0);
            }
        }
    }
}
