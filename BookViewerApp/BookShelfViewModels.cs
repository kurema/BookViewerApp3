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
    public class BookShelfViewModel : INotifyPropertyChanged, IEnumerable<BookContainerViewModel>
    {
        public async static Task<ObservableCollection<BookShelfViewModel>> GetBookShelfViewModels (bool addSecretShelf){
            var storages = await BookShelfStorage.GetBookShelves();
            var result= new ObservableCollection<BookShelfViewModel>();
            if (storages == null) return null;
            foreach(var item in storages)
            {
                if (addSecretShelf || item.Secret == false)
                {
                    var content = new BookShelfViewModel();
                    content.Title = item.Title;
                    content.Containers = new ObservableCollection<BookContainerViewModel>(await BookContainerViewModel.GetFromBookShelfStorage(item.Folders,content));
                    result.Add(content);
                }
            }
            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged(nameof(Title)); }
        }
        private string _Title;
        public string TitleID { get
            {
                if (Containers.Count > 0) return Containers[0].TitleID;
                else return null;
            }
        }

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public IEnumerator<BookContainerViewModel> GetEnumerator()
        {
            return ((IEnumerable<BookContainerViewModel>)Containers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<BookContainerViewModel>)Containers).GetEnumerator();
        }

        public ObservableCollection<BookContainerViewModel> Containers
        {
            get { return _Containers; }
            set
            {
                _Containers.CollectionChanged -= _Containers_CollectionChanged;
                foreach(var item in _Containers)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                _Containers = value;
                foreach (var item in value)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                _Containers.CollectionChanged += _Containers_CollectionChanged;
                OnPropertyChanged(nameof(Containers));
                OnPropertyChanged(nameof(TitleID));
            }
        }
        private ObservableCollection<BookContainerViewModel> _Containers = new ObservableCollection<BookContainerViewModel>();

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(TitleID))
            {
                OnPropertyChanged(nameof(TitleID));
            }
        }

        private void _Containers_CollectionChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(TitleID));
        }
    }

    public class BookContainerViewModel : INotifyPropertyChanged, IEnumerable<IItemViewModel>, IItemViewModel
    {
        public BookContainerViewModel(string Title,BookShelfViewModel Shelf, BookContainerViewModel Parent=null)
        {
            this.Title = Title;
            this.Parent = Parent;
            this.BookShelf = Shelf;
        }

        public string TitleID
        {
            get
            {
                string result = null;
                foreach(var item in this)
                {
                    if(item is BookViewModel)
                    {
                        return (item as BookViewModel).ID;
                    }else if(item is BookContainerViewModel && result==null)
                    {
                        result = (item as BookContainerViewModel).TitleID;
                    }
                }
                return result;
            }
        }

        public BookContainerViewModel Parent { get; private set; }
        public BookShelfViewModel BookShelf { get; private set; }

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

        public ObservableCollection<IItemViewModel> Books
        {
            get { return _Books; }
            set {
                _Books.CollectionChanged -= BooksCollectionChanged;
                _Books = value;
                _Books.CollectionChanged += BooksCollectionChanged;
                OnPropertyChanged(nameof(Books));
                OnPropertyChanged(nameof(TitleID));
            }
        }

        private void BooksCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TitleID));
        }

        private ObservableCollection<IItemViewModel> _Books = new ObservableCollection<IItemViewModel>();

        public void Add(IItemViewModel item)
        {
            Books.Add(item);
        }

        public async static Task<BookContainerViewModel[]> GetFromBookShelfStorage(int index, BookShelfViewModel Shelf)
        {
            var storages= await BookShelfStorage.GetBookShelves();
            if (storages.Count > index)
            {
                return await GetFromBookShelfStorage(storages[index].Folders,Shelf);
            }
            else {
                return new BookContainerViewModel[0];
            }
        }

        public async static Task<BookContainerViewModel[]> GetFromBookShelfStorage(IEnumerable<BookShelfStorage.BookContainer> storages, BookShelfViewModel Shelf)
        {
            var result = new List<BookContainerViewModel>();

            foreach (var item in storages)
            {
                result.Add(await GetFromBookShelfStorage(item,Shelf));
            }
            return result.ToArray();
        }


        public async static Task<BookContainerViewModel> GetFromBookShelfStorage(BookShelfStorage.BookContainer storage,BookShelfViewModel Shelf,BookContainerViewModel Parent=null)
        {
            BookContainerViewModel result = new BookContainerViewModel(storage.Title,Shelf,Parent);
            foreach (var item in storage.Folders)
            {
                result.Add(await GetFromBookShelfStorage(item as BookShelfStorage.BookContainer,Shelf,result));
            }
            foreach (var item in storage.Files)
            {
                result.Add(await BookViewModel.GetFromBookShelfStorage(item as BookShelfStorage.BookContainer.BookShelfBook,result));
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

        public BookViewModel(string ID, int BookSize,BookShelfStorage.BookAccessInfo accessInfo,BookContainerViewModel Parent)
        {
            this.ID = ID;
            this.BookSize = BookSize;
            this.AccessInfo = accessInfo;
            this.Parent = Parent;
        }

        public BookContainerViewModel Parent { get; private set; }

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
                this.ReadRate = (bookInfo.GetLastReadPage()?.Page ?? 0) / (double)BookSize;
                //Issue: asyncの関係でSetLastReadPage()の前に GetLastReadPage()が実行されることがあるようだ。
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
        private bool _Reversed = false;

        public async static Task<BookViewModel> GetFromBookShelfStorage(BookShelfStorage.BookContainer.BookShelfBook storage,BookContainerViewModel parent)
        {
            var result= new BookViewModel(storage.ID, storage.Size,storage.Access,parent) { Title = storage.Title };
            await result.GetFromBookInfoStorageAsync();
            return result;
        }
    }
}
