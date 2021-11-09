using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace BookViewerApp.ViewModels;

public class Bookshelf2NavigationViewModel
{
    private ObservableCollection<Bookshelf2NavigationItemViewModel> _MenuItems = new ObservableCollection<Bookshelf2NavigationItemViewModel>()
    {
    };
    public ObservableCollection<Bookshelf2NavigationItemViewModel> MenuItems { get => _MenuItems; }
}
