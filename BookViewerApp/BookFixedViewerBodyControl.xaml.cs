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
    public sealed partial class BookFixedViewerBodyControl : UserControl
    {
        private BookFixedBodyViewModel Model { get { return (BookFixedBodyViewModel)DataContext; } }

        public event EventHandler PageCountChanged;
        public event EventHandler SelectedPageChanged;

        public bool Reversed {
            get {
                var book = Model.Book;
                bool result = false;
                while(book!=null && book is Books.ReversedBook)
                {
                    book = (book as Books.ReversedBook).Origin;
                    result = !result;
                }
                return result;
            }
            set
            {
                if (Reversed != value && CanReverse)
                {
                    CanReverse = false;
                    SwapReverse();
                    CanReverse = true;
                }
            }
        }

        private bool CanReverse = true;

        private void SwapReverse()
        {
            if (Model != null && Model.Book != null)
            {
                this.DataContextChanged -= ControlBookViewer_DataContextChanged;
                var page = this.SelectedPage;
                this.DataContext = new BookFixedBodyViewModel(new Books.ReversedBook(Model.Book));
                UpdateDataContext();
                this.SelectedPage = page;
                this.DataContextChanged += ControlBookViewer_DataContextChanged;
            }
        }


        private void RaisePageCountChanged()
        {
            if (PageCountChanged != null)
                PageCountChanged(this, new EventArgs());
        }

        private void RaiseSelectedPageChanged()
        {
            if (SelectedPageChanged != null)
                SelectedPageChanged(this, new EventArgs());
        }


        public BookFixedViewerBodyControl()
        {
            this.InitializeComponent();

            this.DataContextChanged += ControlBookViewer_DataContextChanged;
            this.FlipView.SelectionChanged += async (s,e) => { RaiseSelectedPageChanged(); await SaveBookInfoAsync(); };

        }

        public bool LoadLastReadPageAsDefault = true;

        public async System.Threading.Tasks.Task<BookInfoStorage.BookInfo> GetBookInfoAsync() {
            if (this.Model != null && Model.Book!=null)
            {
                return (await BookInfoStorage.GetBookInfoByIDOrCreateAsync(Model.Book.ID));
            }
            else return null;
        }

        public int PageCount
        {
            get
            {
                return FlipView.Items.Count();
            }
        }

        public int SelectedPage
        {
            get {
                return Reversed ? PageCount - FlipView.SelectedIndex : FlipView.SelectedIndex + 1; }
            set { if (CanSelect(value)) { FlipView.SelectedIndex = Reversed ? PageCount - value : value - 1; } }
        }

        public int SelectedPageVisual
        {
            get { return FlipView.SelectedIndex + 1; }
            set { if (CanSelect(value)) FlipView.SelectedIndex = value - 1; }
        }

        public bool CanSelect(int i)
        {
            return i > 0 && i <= PageCount;
        }

        private void ControlBookViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateDataContext();
        }

        private async void UpdateDataContext()
        {
            if (Model != null)
            {
                FlipView.ItemsSource = GetPageAccessors(Model.Book);
                //SelectedPage = 1;
                RaisePageCountChanged();

                await LoadBookInfoAsync();
            }
        }


        public async System.Threading.Tasks.Task LoadBookInfoAsync(BookInfoStorage.BookInfo bi = null)
        {
            var bookInfo = await GetBookInfoAsync();
            if (bookInfo != null)
            {
                var selectedPage = bookInfo.GetLastReadPage()?.Page;
                this.Reversed = bookInfo.PageReversed;
                if (LoadLastReadPageAsDefault)
                {
                    if (selectedPage != null) SelectedPage = (int)selectedPage;
                }
            }
            else
            {
                this.SelectedPage = 1;
            }

        }

        private async System.Threading.Tasks.Task SaveBookInfoAsync()
        {
            if (Model != null)
            {
                var bookInfo = await GetBookInfoAsync();
                if (bookInfo != null)
                {
                    if (Model !=null)
                    {
                        if(this.SelectedPageVisual>1 && this.SelectedPage>1)
                        {
                            bookInfo.SetLastReadPage((uint)this.SelectedPage);
                            bookInfo.PageReversed = this.Reversed;
                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        public PageViewerControl.PageViewModel[] GetPageAccessors(Books.IBookFixed book) {
            var result = new PageViewerControl.PageViewModel[book.PageCount];
            for (uint i = 0; i < book.PageCount; i++)
            {
                var item = new PageViewerControl.PageViewModel();
                uint value = i;
                item.SetPageAccessor(new Func<Books.IPageFixed>(() => { var page= book.GetPage(value); page.Option = new Books.PageOptionsControl(this);return page; }));
                result[i] = item;
            }
            return result;
        }

        public async void Open(Windows.Storage.IStorageFile file)
        {
            var book=(await Books.BookManager.GetBookFromFile(file));
            if(book is Books.IBookFixed)
            {
                Open(book as Books.IBookFixed);
            }
        }

        public void Open(Books.IBookFixed book)
        {
            if (book != null)
                this.DataContext = new BookFixedBodyViewModel(book as Books.IBookFixed);
        }

        #region Commands

        public class CommandAddPage : System.Windows.Input.ICommand
        {
            private BookFixedViewerBodyControl TargetControl;
            public event EventHandler CanExecuteChanged;
            public int ChangeValue;

            public CommandAddPage(BookFixedViewerBodyControl TargetControl,int ChangeValue)
            {
                this.TargetControl = TargetControl;
                this.ChangeValue = ChangeValue;

                TargetControl.SelectedPageChanged += (s, e) => { RaiseCanExecuteChanged(); };
                TargetControl.PageCountChanged += (s, e) => { RaiseCanExecuteChanged(); };
            }

            protected void RaiseCanExecuteChanged()
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, new EventArgs());
            }

            public bool CanExecute(object parameter)
            {
                return TargetControl.CanSelect(TargetControl.SelectedPageVisual+ ChangeValue);
            }

            public void Execute(object parameter)
            {
                if (TargetControl.CanSelect(TargetControl.SelectedPageVisual + ChangeValue))
                {
                    TargetControl.SelectedPageVisual += ChangeValue;
                }
            }
        }

        public class CommandSetPage : System.Windows.Input.ICommand
        {
            private BookFixedViewerBodyControl TargetControl;
            public event EventHandler CanExecuteChanged;
            public int TargetPage;

            public CommandSetPage(BookFixedViewerBodyControl TargetControl, int TargetPage)
            {
                this.TargetControl = TargetControl;
                this.TargetPage = TargetPage;

                TargetControl.SelectedPageChanged += (s, e) => { RaiseCanExecuteChanged(); };
                TargetControl.PageCountChanged += (s, e) => { RaiseCanExecuteChanged(); };
            }

            protected void RaiseCanExecuteChanged()
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, new EventArgs());
            }

            public bool CanExecute(object parameter)
            {
                return TargetControl.CanSelect(TargetPage) && TargetControl.SelectedPageVisual != TargetPage;
            }

            public void Execute(object parameter)
            {
                if (TargetControl.CanSelect(TargetPage))
                {
                    TargetControl.SelectedPageVisual = TargetPage;
                }
            }
        }

        public class CommandLastPage : System.Windows.Input.ICommand
        {
            private BookFixedViewerBodyControl TargetControl;
            public event EventHandler CanExecuteChanged;

            public CommandLastPage(BookFixedViewerBodyControl TargetControl)
            {
                this.TargetControl = TargetControl;

                TargetControl.SelectedPageChanged += (s, e) => { RaiseCanExecuteChanged(); };
                TargetControl.PageCountChanged += (s, e) => { RaiseCanExecuteChanged(); };
            }

            protected void RaiseCanExecuteChanged()
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, new EventArgs());
            }

            public bool CanExecute(object parameter)
            {
                return TargetControl.SelectedPageVisual != TargetControl.PageCount;
            }

            public void Execute(object parameter)
            {
                TargetControl.SelectedPageVisual = TargetControl.PageCount;
            }
        }

        public class CommandSwapReverse : System.Windows.Input.ICommand
        {
            private BookFixedViewerBodyControl TargetControl;
            public event EventHandler CanExecuteChanged;

            public CommandSwapReverse(BookFixedViewerBodyControl TargetControl)
            {
                this.TargetControl = TargetControl;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                TargetControl.Reversed = !TargetControl.Reversed;
            }
        }

        public class CommandOpenPicker : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;
            private BookFixedViewerBodyControl TargetControl;

            public CommandOpenPicker(BookFixedViewerBodyControl TargetControl)
            {
                this.TargetControl = TargetControl;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public async void Execute(object parameter)
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                foreach(var ext in Books.BookManager.AvailableExtensions)
                {
                    picker.FileTypeFilter.Add(ext);
                }
                var file = await picker.PickSingleFileAsync();
                TargetControl.Open(file);
            }
        }
        #endregion Commands

        #region ViewModel
        //Not fully used...
        public class BookFixedBodyViewModel : INotifyPropertyChanged
        {
            public BookFixedBodyViewModel(Books.IBookFixed book) { this.Book = book; }
            public BookFixedBodyViewModel() { }

            public event PropertyChangedEventHandler PropertyChanged;

            public Books.IBookFixed Book { get { return _Book; } set { _Book = value; RaisePropertyChanged(nameof(Book)); } }
            private Books.IBookFixed _Book;

            protected void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

}