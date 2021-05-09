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

using System.ComponentModel;
using System.Threading.Tasks;
using BookViewerApp.ViewModels;
using BookViewerApp.Storages;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp.Views
{
    public sealed partial class BookshelfControl : UserControl
    {
        public event ItemClickedEventHandler ItemClicked;
        public delegate void ItemClickedEventHandler(object sender, ItemClickedEventArgs e);
        public class ItemClickedEventArgs : EventArgs
        {
            public IItemViewModel SelectedItem;
        }
        public void OnItemClicked (IItemViewModel vm)
        {
            ItemClicked?.Invoke(this, new ItemClickedEventArgs() { SelectedItem = vm });
        }


        public BookshelfControl()
        {
            this.InitializeComponent();

            SetBookshelfItemSize((double)SettingStorage.GetValue("TileWidth"), (double)SettingStorage.GetValue("TileHeight"));
        }

        public void SetSource(params BookContainerViewModel[] vms)
        {
            BookshelfItemsSource.Source = vms;
        }

        private void SetBookshelfItemSize(double width,double height)
        {
            BookshelfItemStyle.Setters.Clear();
            BookshelfItemStyle.Setters.Add(new Setter(WidthProperty, width));
            BookshelfItemStyle.Setters.Add(new Setter(HeightProperty, height));
        }

        private void GridViewMain_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is IItemViewModel)
            {
                OnItemClicked(e.ClickedItem as IItemViewModel);
            }
        }
    }

    namespace TemplateSelectors
    {
        public sealed class BookshelfItemTemplateSelector : DataTemplateSelector
        {
            public DataTemplate ContainerTemplate { get; set; }
            public DataTemplate BookTemplate { get; set; }

            protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            {
                if (item is BookContainerViewModel)
                {
                    return ContainerTemplate;
                }
                else if (item is BookshelfBookViewModel)
                {
                    return BookTemplate;
                }
                return base.SelectTemplateCore(item, container);
            }

        }
    }
}
