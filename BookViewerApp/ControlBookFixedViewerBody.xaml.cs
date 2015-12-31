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
    public sealed partial class ControlBookFixedViewerBody : UserControl
    {
        private BookFixedBodyViewModel Model { get { return (BookFixedBodyViewModel)DataContext; } }

        public event EventHandler PageCountChanged;
        public event EventHandler SelectedPageChanged;


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


        public ControlBookFixedViewerBody()
        {
            this.InitializeComponent();

            this.DataContextChanged += ControlBookViewer_DataContextChanged;
            this.FlipView.SelectionChanged += async (s,e) => { RaiseSelectedPageChanged(); await SaveLastReadPage(); };

        }

        public bool LoadLastReadPageAsDefault = true;

        public async void LoadLastReadPage()
        {
            var bookInfo = await GetBookInfoAsync();
            if (bookInfo != null)
            {
                var lastpage = bookInfo.GetLastReadPage();
                if (lastpage != null) SelectedPage = (int)lastpage.Page;
            }
        }

        public async System.Threading.Tasks.Task<BookInfoStorage.BookInfo> GetBookInfoAsync() {
            if (this.Model != null && Model.Book!=null)
            {
                return (await BookInfoStorage.GetBookInfoByIDAsync(Model.Book.ID));
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
            get { return FlipView.SelectedIndex; }
            set { if(CanSelect(value)) FlipView.SelectedIndex = value; }
        }

        public bool CanSelect(int i)
        {
            return i >= 0 && i < PageCount;
        }

        private string OldID = null;

        private async void ControlBookViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Model != null)
            {
                if (OldID != null) { (await BookInfoStorage.GetBookInfoByIDAsync(OldID)).SetLastReadPage((uint)this.SelectedPage); }

                FlipView.ItemsSource = GetPageAccessors(Model.Book);
                SelectedPage = 0;
                RaisePageCountChanged();

                OldID = Model.Book.ID;

                if (LoadLastReadPageAsDefault) LoadLastReadPage();
            }
        }

        private async System.Threading.Tasks.Task SaveLastReadPage()
        {
            if (Model != null)
            {
                var bookInfo = await GetBookInfoAsync();
                if (bookInfo != null)
                {
                    bookInfo.SetLastReadPage((uint)this.SelectedPage);
                }
            }
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

        public async void Open(Windows.Storage.IStorageFile file)
        {
            if (file == null) { }
            else if (Path.GetExtension(file.Path).ToLower() == ".pdf")
            {
                var book = new Books.Pdf.PdfBook();
                await book.Load(file);
                this.DataContext = new ControlBookFixedViewerBody.BookFixedBodyViewModel(book);
            }
            else if (new String[] { ".zip", ".cbz" }.Contains(Path.GetExtension(file.Path).ToLower()))
            {
                var book = new Books.Cbz.CbzBook();
                await book.LoadAsync(WindowsRuntimeStreamExtensions.AsStream(await file.OpenReadAsync()));
                this.DataContext = new BookFixedBodyViewModel(book);
            }
        }
        #region Commands

        public class CommandAddPage : System.Windows.Input.ICommand
        {
            private ControlBookFixedViewerBody TargetControl;
            public event EventHandler CanExecuteChanged;
            public int ChangeValue;

            public CommandAddPage(ControlBookFixedViewerBody TargetControl,int ChangeValue)
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
                return TargetControl.CanSelect(TargetControl.SelectedPage+ ChangeValue);
            }

            public void Execute(object parameter)
            {
                if (TargetControl.CanSelect(TargetControl.SelectedPage + ChangeValue))
                {
                    TargetControl.SelectedPage += ChangeValue;
                }
            }
        }

        public class CommandSetPage : System.Windows.Input.ICommand
        {
            private ControlBookFixedViewerBody TargetControl;
            public event EventHandler CanExecuteChanged;
            public int TargetPage;

            public CommandSetPage(ControlBookFixedViewerBody TargetControl, int TargetPage)
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
                return TargetControl.CanSelect(TargetPage) && TargetControl.SelectedPage!=TargetPage;
            }

            public void Execute(object parameter)
            {
                if (TargetControl.CanSelect(TargetControl.SelectedPage + TargetPage))
                {
                    TargetControl.SelectedPage = TargetPage;
                }
            }
        }


        public class CommandOpenPicker : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;
            private ControlBookFixedViewerBody TargetControl;

            public CommandOpenPicker(ControlBookFixedViewerBody TargetControl)
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
                picker.FileTypeFilter.Add(".pdf");
                picker.FileTypeFilter.Add(".zip");
                picker.FileTypeFilter.Add(".cbz");
                var file = await picker.PickSingleFileAsync();
                TargetControl.Open(file);
            }
        }
        #endregion Commands


        #region ViewModel

        public class BookFixedBodyViewModel : INotifyPropertyChanged
        {
            public BookFixedBodyViewModel(Books.IBookFixed book) { this.Book = book; }
            public BookFixedBodyViewModel() { }

            public event PropertyChangedEventHandler PropertyChanged;

            public Books.IBookFixed Book { get { return _Book; } set { _Book = value; RaisePropertyChanged(nameof(Book)); } }
            private Books.IBookFixed _Book;

            //public int SelectedPage { get { return _SelectedPage; } set { _Book = value; RaisePropertyChanged(nameof(SelectedPage)); } }
            //private int _SelectedPage;

            protected void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

}