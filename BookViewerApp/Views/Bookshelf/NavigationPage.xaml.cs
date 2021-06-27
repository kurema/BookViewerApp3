using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using BookViewerApp.ViewModels;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views.Bookshelf
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NavigationPage : Page
    {
        public NavigationPage()
        {
            this.InitializeComponent();

            SetUpItems();
        }

        private async void SetUpItems()
        {
            if (!(this.DataContext is ViewModels.Bookshelf2NavigationViewModel vm)) return;
            vm.MenuItems.Clear();
            var items = await Storages.LibraryStorage.GetItemLibrary()?.GetChildren();
            if (items is null) return;
            var list = new List<Bookshelf2NavigationItemViewModel>();
            foreach (var item in items)
            {
                list.Add(new Bookshelf2NavigationItemViewModel()
                {
                    Icon = new SymbolIcon(Symbol.Library),
                    ItemKind = Bookshelf2NavigationItemViewModel.ItemType.Item,
                    Tag = item,
                    Title = item.Name,
                    OpenAction = OpenItem
                });
            }
            if (list.Count > 0)
            {
                vm.MenuItems.Add(new Bookshelf2NavigationItemViewModel()
                {
                    Title = "Library",
                    ItemKind = Bookshelf2NavigationItemViewModel.ItemType.Header
                });
                foreach (var item in list) { vm.MenuItems.Add(item); }
            }
            vm.MenuItems.Add(new Bookshelf2NavigationItemViewModel()
            {
                ItemKind = Bookshelf2NavigationItemViewModel.ItemType.Separator
            });
            vm.MenuItems.Add(new Bookshelf2NavigationItemViewModel()
            {
                Title = "History",
                ItemKind = Bookshelf2NavigationItemViewModel.ItemType.Item,
                Icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uF738", },
                OpenAction = OpenItem,
                Tag = Storages.LibraryStorage.GetItemHistoryMRU(new Helper.InvalidCommand()),
            });
        }

        private void OpenItem(Bookshelf2NavigationItemViewModel vm)
        {
            if (!(vm.Tag is kurema.FileExplorerControl.Models.FileItems.IFileItem file)) return;
            FrameMain.Navigate(typeof(BookshelfPageFileItem), file);
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                FrameMain.Navigate(typeof(SettingPage));
                return;
            }
            if (!(args.SelectedItem is Bookshelf2NavigationItemViewModel itemVM)) return;
            itemVM.Open();
        }
    }
}

namespace BookViewerApp.Views.TemplateSelectors
{
    public sealed class Bookshelf2NavigationItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TemplateItem { get; set; }
        public DataTemplate TemplateSeparator { get; set; }
        public DataTemplate TemplateHeader { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (!(item is Bookshelf2NavigationItemViewModel vm)) return base.SelectTemplateCore(item);

            switch (vm.ItemKind)
            {
                default:
                case Bookshelf2NavigationItemViewModel.ItemType.Item:
                    return TemplateItem;
                case Bookshelf2NavigationItemViewModel.ItemType.Separator:
                    return TemplateSeparator;
                case Bookshelf2NavigationItemViewModel.ItemType.Header:
                    return TemplateHeader;
            }
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }

    }
}