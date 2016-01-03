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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class BookShelfControl : UserControl
    {
        public event ItemClickedEventHandler ItemClicked;
        public delegate void ItemClickedEventHandler(object sender, ItemClickedEventArgs e);
        public class ItemClickedEventArgs : EventArgs
        {
            public BookShelfViewModels.IItemViewModel SelectedItem;
        }
        public void OnItemClicked (BookShelfViewModels.IItemViewModel vm)
        {
            if (ItemClicked != null)
                ItemClicked(this, new ItemClickedEventArgs() { SelectedItem = vm });
        }


        public BookShelfControl()
        {
            this.InitializeComponent();

            SetBookShelfItemSize(300, 300);
        }

        public void SetSource(params BookShelfViewModels.BookContainerViewModel[] vms)
        {
            BookShelfItemsSource.Source = vms;
        }

        public async Task OpenAsync()
        {
            SetSource(await BookShelfViewModels.BookContainerViewModel.GetFromBookShelfStorage(0));
        }

        public async Task OpenAsync(params BookShelfStorage.BookContainer[] storage)
        {
            SetSource(await BookShelfViewModels.BookContainerViewModel.GetFromBookShelfStorage(storage));
        }

        private void SetBookShelfItemSize(double width,double height)
        {
            BookShelfItemStyle.Setters.Clear();
            BookShelfItemStyle.Setters.Add(new Setter(WidthProperty, width));
            BookShelfItemStyle.Setters.Add(new Setter(HeightProperty, height));
        }

        private void GridViewMain_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is BookShelfViewModels.IItemViewModel)
            {
                OnItemClicked(e.ClickedItem as BookShelfViewModels.IItemViewModel);
            }
        }

    }
}
