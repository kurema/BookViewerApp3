using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Data;
using System.Windows.Input;
using BookViewerApp.Helper;
using BookViewerApp.Managers;

using BookViewerApp.Storages;
using BookViewerApp.Views;

using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;

#nullable enable
namespace BookViewerApp.ViewModels;
public class CompositionEffectViewModel : ViewModelBase
{
    private UIElement? _Target;
    public UIElement? Target { get => _Target; set
        {
            SetProperty(ref _Target, value);
            TargetVisual = Windows.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(value);
            //https://docs.microsoft.com/en-us/windows/uwp/composition/composition-effects
            {
                //var graphicEffect = new Microsoft.Graphics.Canvas.Effects.ArithmeticCompositeEffect()
                //{
                //    Source1=new CompositionEffectSourceParameter("source1"),
                //};
                //var effect = TargetVisual.Compositor.CreateEffectFactory();
            }

        }
    }

    private Windows.UI.Composition.Visual? TargetVisual;
}

public class CompositionEffectItemViewModel : ViewModelBase
{
    public CompositionEffectItemViewModel Parent { get; }

    private double _MinValue;
    public double MinValue { get => _MinValue; set => SetProperty(ref _MinValue, value); }

    private double _MaxValue;
    public double MaxValue { get => _MaxValue; set => SetProperty(ref _MaxValue, value); }

    private double _Value;
    public double Value { get => _Value; set
        {
            SetProperty(ref _Value, value);
            //Parent.
        }
    }

    private string _Title;
    public string Title { get => _Title; set => SetProperty(ref _Title, value); }

    private double _DefaultValue;

    public CompositionEffectItemViewModel(CompositionEffectItemViewModel parent, double minValue, double maxValue, double defaultValue, string key)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _MinValue = minValue;
        _MaxValue = maxValue;
        _DefaultValue = defaultValue;
        _Title = Managers.ResourceManager.Loader.GetString($"Composition/{key}/Title");
    }

    public double DefaultValue { get => _DefaultValue; set => SetProperty(ref _DefaultValue, value); }


}
