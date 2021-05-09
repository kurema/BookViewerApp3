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

using BookViewerApp.Helper;

namespace BookViewerApp.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private ObservableCollection<Bookshelf2ViewModelMenuItem> menuItems = new ObservableCollection<Bookshelf2ViewModelMenuItem>();
        public ObservableCollection<Bookshelf2ViewModelMenuItem> MenuItems { get => menuItems; }
        public Bookshelf2ViewModelMenuItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (value != selectedItem)
                {
                    value?.Action?.Invoke(value);
                    SetProperty(ref selectedItem, value);
                }
            }
        }

        public void UpdateSelectedItem(Bookshelf2ViewModelMenuItem value)
        {
            //For "Back" button.
            SetProperty(ref selectedItem, value);
        }

        public void OpenSetting()
        {
            SettingItem?.Action?.Invoke(SettingItem);
        }

        public Bookshelf2ViewModelMenuItem SettingItem
        {
            get => settingItem;
            set => SetProperty(ref settingItem, value);
        }

        private Bookshelf2ViewModelMenuItem selectedItem = new Bookshelf2ViewModelMenuItem() { 
            Title="Setting",
        };

        private Bookshelf2ViewModelMenuItem settingItem;
    }

    public class Bookshelf2ViewModelMenuItem : ViewModelBase
    {
        private string title;
        private string resourceKey;
        private IconElement icon;
        private object tag;
        private Action<Bookshelf2ViewModelMenuItem> action;

        private bool isLocked;

        public string Title { get => title; set => SetProperty(ref title, value); }
        public string ResourceKey { get => resourceKey; set => SetProperty(ref resourceKey, value); }
        public IconElement Icon { get => icon; set => SetProperty(ref icon, value); }
        public object Tag { get => tag; set => SetProperty(ref tag, value); }
        public Action<Bookshelf2ViewModelMenuItem> Action { get => action; set => SetProperty(ref action, value); }
        public bool IsLocked { get => isLocked; set => SetProperty(ref isLocked, value); }
    }
}
