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
    public sealed partial class ControlBookFixedViewer : UserControl
    {
        private BookViewModel Model { get { return (BookViewModel)DataContext; } }

        public ControlBookFixedViewer()
        {
            this.InitializeComponent();

            this.DataContextChanged += ControlBookViewer_DataContextChanged;
        }

        private void ControlBookViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if(Model!=null)
            FlipView.ItemsSource = GetPageAccessors(Model.Book);
        }

        public ControlPageViewer.PageViewModel[] GetPageAccessors(Books.IBookFixed book) {
            var result = new ControlPageViewer.PageViewModel[book.PageCount];
            for (uint i = 0; i < book.PageCount; i++)
            {
                var item = new ControlPageViewer.PageViewModel();
                uint value = i;
                item.SetPageAccessor(new Func<Books.IPage>(() => { var page= book.GetPage(value); page.Option = new Books.PageOptionsControl(this);return page; }));
                result[i] = item;
            }
            return result;
        }

        public class BookViewModel : INotifyPropertyChanged
        {
            public BookViewModel(Books.IBookFixed book) { this.Book = book; }

            public event PropertyChangedEventHandler PropertyChanged;

            public Books.IBookFixed Book { get { return _Book; } set { _Book = value; RaisePropertyChanged("Book"); } }
            private Books.IBookFixed _Book;

            protected void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}