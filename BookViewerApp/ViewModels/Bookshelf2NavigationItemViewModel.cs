using System;
using Windows.UI.Xaml.Controls;

namespace BookViewerApp.ViewModels;

public class Bookshelf2NavigationItemViewModel : Helper.ViewModelBase
{
    private ItemType _ItemKind;
    public ItemType ItemKind { get => _ItemKind; set => SetProperty(ref _ItemKind, value); }

    public enum ItemType
    {
        Item, Separator, Header
    }

    private object _Tag;
    public object Tag { get => _Tag; set => SetProperty(ref _Tag, value); }

    private string _Title;
    public string Title { get => _Title; set => SetProperty(ref _Title, value); }

    private IconElement _Icon;
    public IconElement Icon { get => _Icon; set => SetProperty(ref _Icon, value); }


    private Action<Bookshelf2NavigationItemViewModel> _OpenAction;
    public Action<Bookshelf2NavigationItemViewModel> OpenAction { get => _OpenAction; set => SetProperty(ref _OpenAction, value); }

    public void Open() => OpenAction?.Invoke(this);

}
