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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class BookShelfControl : UserControl
    {
        public BookShelfControl()
        {
            this.InitializeComponent();

            BookShelfItemsSource.Source = new BookShelfViewModels.BookShelfModel[]
            {
                new BookShelfViewModels.BookShelfModel("本棚1") {

                new BookShelfViewModels.ItemViewModel() {  ReadRate=0.50,Reversed=true,Title="sample Book" },
                new BookShelfViewModels.ItemViewModel() {  ReadRate=0.80,Reversed=false,Title="test Book" }
                },
                new BookShelfViewModels.BookShelfModel("本棚2") {
                new BookShelfViewModels.ItemViewModel() {  ReadRate=0.20,Reversed=true,Title="gaga Book" },
                new BookShelfViewModels.ItemViewModel() {  ReadRate=0.60,Reversed=false,Title="ee Book" }
                }
            };
            SetBookShelfItemSize(300, 300);
        }

        private void SetBookShelfItemSize(double width,double height)
        {
            BookShelfItemStyle.Setters.Clear();
            BookShelfItemStyle.Setters.Add(new Setter(BookShelfItemControl.WidthProperty, width));
            BookShelfItemStyle.Setters.Add(new Setter(BookShelfItemControl.HeightProperty, height));
        }


    }
}
