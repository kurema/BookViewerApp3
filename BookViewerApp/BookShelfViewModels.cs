using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace BookViewerApp.BookShelfViewModels
{
    public class BookShelfModel : INotifyPropertyChanged,IEnumerable<ItemViewModel>
    {
        public BookShelfModel(string Title)
        {
            this.Title = Title;
        }
        public BookShelfModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public IEnumerator<ItemViewModel> GetEnumerator()
        {
            return ((IEnumerable<ItemViewModel>)Books).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ItemViewModel>)Books).GetEnumerator();
        }

        public string Title
        {
            get { return _Title; }
            set { _Title = value;OnPropertyChanged(nameof(Title)) ;}
        }
        private string _Title;

        public ObservableCollection<ItemViewModel> Books { get { return _Books; } set { _Books = value;OnPropertyChanged(nameof(Books)); } }
        private ObservableCollection<ItemViewModel> _Books = new ObservableCollection<ItemViewModel>();

        public void Add(ItemViewModel item)
        {
            Books.Add(item);
        }

        public static BookShelfModel GetBooksFromStorage(BookShelfStorage.BookShelf bs) {
            BookShelfModel result = new BookShelfModel();
            foreach(var item in bs.Contents)
            {
                var itemVM = new ItemViewModel();
                if (item is BookShelfStorage.BookShelf)
                {
                    var itemBS = (item as BookShelfStorage.BookShelf);
                    itemVM.Title = itemBS.Title;
                    itemVM.Children = GetBooksFromStorage(itemBS);
                    //itemVM.ImageSource
                }else if(item is BookShelfStorage.BookShelf.BookShelfBook){
                    var itemBI = item as BookShelfStorage.BookShelf.BookShelfBook;
                    itemVM.Title = itemBI.Title;
                    itemVM.ID = itemBI.ID;
                }
                result.Add(itemVM);
            }
            return result;
        }
    }

    public class ItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public string Title { get { return _Title; } set { _Title = value;OnPropertyChanged(nameof(Title)); } }
        private string _Title;

        public string ID { get { return _ID; } set { _ID = value; OnPropertyChanged(nameof(ID)); } }
        private string _ID;

        public ImageSource ImageSource { get { return _ImageSource; } set { _ImageSource = value;OnPropertyChanged(nameof(ImageSource)); } }
        private ImageSource _ImageSource;

        public BookShelfModel Children { get { return _Children; } set { _Children = value; OnPropertyChanged(nameof(Children)); } }
        private BookShelfModel _Children = null;

        public double ReadRate { get { return _ReadRate; } set { _ReadRate = value; OnPropertyChanged(nameof(ReadRate)); } }
        private double _ReadRate = 0.0;

        public bool Reversed { get { return _Reversed; } set { _Reversed = value; OnPropertyChanged(nameof(Reversed)); } }
        private bool _Reversed;

        public Books.IBook Book { get { return _Book; } set { _Book = value; OnPropertyChanged(nameof(Book)); } }
        private Books.IBook _Book = null;
    }
}
