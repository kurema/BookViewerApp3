using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.ViewModels;
public class AdBlockerSettingViewModel : Helper.ViewModelBase
{
    public IGrouping<Storages.ExtensionAdBlockerItems.itemsGroup, Storages.ExtensionAdBlockerItems.itemsGroupItem> FilterList { get; private set; }

    //public Task LoadFilterList()
    //{
    //    //var filter = Managers.ExtensionAdBlockerManager.LocalLists.
    //}
}
