﻿using System;
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


    public static readonly DependencyProperty PrevNextButtonVisibilityProperty = DependencyProperty.Register(nameof(PrevNextButtonVisibility)
        , typeof(Visibility), typeof(FlipViewEx), new PropertyMetadata(Visibility.Visible, (s, e) =>
        {
            if (s is not FlipViewEx ex) return;
            if (e.NewValue is not Visibility v) return;
            if (ex.GetTemplateChild("PreviousButtonHorizontal") is Button prev) prev.Visibility = v;
            if (ex.GetTemplateChild("NextButtonHorizontal") is Button next) next.Visibility = v;
        }));

    public Visibility PrevNextButtonVisibility
    {
        get
        {
            if (GetTemplateChild("PreviousButtonHorizontal") is Button prev) return prev.Visibility;
            if (GetTemplateChild("NextButtonHorizontal") is Button next) return next.Visibility;
            return Visibility.Visible;
        }
        set
        {
            SetValue(PrevNextButtonVisibilityProperty,value);
        }
    }

    public float HorizontalOffset
    {
        get
        {
            if (GetTemplateChild("ScrollingHost") is ScrollViewer scroll)
            {
                if (scroll.Content is UIElement content)
                {
                    return content.Translation.X;
                }
            }
            return 0;
        }
        set
        {
            if (GetTemplateChild("ScrollingHost") is ScrollViewer scroll)
            {
                if (scroll.Content is UIElement content)
                {
                    content.Translation = new System.Numerics.Vector3(value, 0, 0);
                }
            }
        }
    }
}
