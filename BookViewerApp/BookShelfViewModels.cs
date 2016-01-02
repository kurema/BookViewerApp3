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
    public class BookShelfViewModel : INotifyPropertyChanged, IEnumerable<IItemViewModel>, IItemViewModel
    {
        public BookShelfViewModel(string Title)
        {
            this.Title = Title;
        }
        public BookShelfViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public IEnumerator<IItemViewModel> GetEnumerator()
        {
            return ((IEnumerable<IItemViewModel>)Books).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IItemViewModel>)Books).GetEnumerator();
        }

        public string Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged(nameof(Title)); }
        }
        private string _Title;

        public ObservableCollection<IItemViewModel> Books { get { return _Books; } set { _Books = value; OnPropertyChanged(nameof(Books)); } }
        private ObservableCollection<IItemViewModel> _Books = new ObservableCollection<IItemViewModel>();

        public void Add(IItemViewModel item)
        {
            Books.Add(item);
        }

        public async static Task<BookShelfViewModel[]> GetFromBookShelfStorage()
        {
            var storages= await BookShelfStorage.GetBookShelves();
            return await GetFromBookShelfStorage(storages);
        }

        public async static Task<BookShelfViewModel[]> GetFromBookShelfStorage(IEnumerable<BookShelfStorage.BookShelf> storages)
        {
            var result = new List<BookShelfViewModel>();

            foreach (var item in storages)
            {
                result.Add(await GetFromBookShelfStorage(item));
            }
            return result.ToArray();
        }


        public async static Task<BookShelfViewModel> GetFromBookShelfStorage(BookShelfStorage.BookShelf storage)
        {
            BookShelfViewModel result = new BookShelfViewModel(storage.Title);
            foreach (var item in storage.Folders)
            {
                result.Add(await GetFromBookShelfStorage(item as BookShelfStorage.BookShelf));
            }
            foreach (var item in storage.Files)
            {
                result.Add(await BookViewModel.GetFromBookShelfStorage(item as BookShelfStorage.BookShelf.BookShelfBook));
            }
            return result;
        }
    }

    public interface IItemViewModel : INotifyPropertyChanged
    {
        string Title { get; set; }
    }

    public class BookViewModel : IItemViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public int BookSize { get; private set; }
        public string ID { get; private set; }
        private BookShelfStorage.BookAccessInfo AccessInfo;

        public BookViewModel(string ID, int BookSize,BookShelfStorage.BookAccessInfo accessInfo)
        {
            this.ID = ID;
            this.BookSize = BookSize;
            AccessInfo = accessInfo;
        }

        public async Task<Books.IBook> TryGetBook()
        {
            var item=(await AccessInfo.TryGetItem());
            if(item!= null && item is Windows.Storage.IStorageFile)
            {
                return await Books.BookManager.GetBookFromFile(item as Windows.Storage.IStorageFile);
            }
            return null;
        }

        public async Task GetFromBookInfoStorageAsync()
        {
            await GetFromBookInfoStorageAsync(this.ID);
        }

        private async Task GetFromBookInfoStorageAsync(string ID)
        {
            var bookInfo = await BookInfoStorage.GetBookInfoByIDAsync(ID);
            if (bookInfo != null)
            {
                this.Reversed = bookInfo.PageReversed;
                this.ReadRate = bookInfo.GetLastReadPage().Page / (double)BookSize;
            }
            else
            {
                this.Reversed = false;
                this.ReadRate = 0;
            }
        }

        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(nameof(Title)); } }
        private string _Title;

        public ImageSource ImageSource { get { return _ImageSource; } set { _ImageSource = value; OnPropertyChanged(nameof(ImageSource)); } }
        private ImageSource _ImageSource;

        public double ReadRate { get { return _ReadRate; } set { _ReadRate = value; OnPropertyChanged(nameof(ReadRate)); } }
        private double _ReadRate = 0.0;

        public bool Reversed { get { return _Reversed; } set { _Reversed = value; OnPropertyChanged(nameof(Reversed)); } }
        private bool _Reversed;

        public async static Task<BookViewModel> GetFromBookShelfStorage(BookShelfStorage.BookShelf.BookShelfBook storage)
        {
            var result= new BookViewModel(storage.ID, storage.Size,storage.Access) { Title = storage.Title };
            await result.GetFromBookInfoStorageAsync();
            return result;
        }
    }
}
