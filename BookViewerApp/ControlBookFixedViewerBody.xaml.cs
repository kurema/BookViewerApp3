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
            this.FlipView.SelectionChanged += (s,e) => { RaiseSelectedPageChanged(); };

        }

        //public void SelectPage(int i)
        //{
        //    FlipView.SelectedIndex = i;
        //}

        //public void SelectPagePrevious()
        //{
        //    FlipView.SelectedIndex = Math.Max(FlipView.SelectedIndex - 1, 0);
        //}

        //public void SelectPageNext()
        //{
        //    FlipView.SelectedIndex = Math.Min(FlipView.SelectedIndex + 1, PageCount);
        //}

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

        private void ControlBookViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Model != null)
            {
                FlipView.ItemsSource = GetPageAccessors(Model.Book);
                SelectedPage = 0;
                RaisePageCountChanged();
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

        public class CommandOpen : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;
            private ControlBookFixedViewerBody TargetControl;

            public CommandOpen(ControlBookFixedViewerBody TargetControl)
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
                var file = await picker.PickSingleFileAsync();//ToDo:Prepare for Exception.
                if (file == null) { }
                else if (Path.GetExtension(file.Path).ToLower() == ".pdf")
                {
                    var book = new Books.Pdf.PdfBook();
                    await book.Load(file);
                    TargetControl.DataContext = new ControlBookFixedViewerBody.BookFixedBodyViewModel(book);
                }
                else if (new String[]{".zip",".cbz" }.Contains( Path.GetExtension(file.Path).ToLower()))
                {
                    var book = new Books.Cbz.CbzBook();
                    await book.LoadAsync(WindowsRuntimeStreamExtensions.AsStream(await file.OpenReadAsync()));
                    TargetControl.DataContext = new BookFixedBodyViewModel(book);
                }
            }
        }

        #endregion Commands

        #region ViewModel
        public class BookFixedBodyViewModel : INotifyPropertyChanged
        {
            public BookFixedBodyViewModel(Books.IBookFixed book) { this.Book = book; }
            public BookFixedBodyViewModel() { }


            public event PropertyChangedEventHandler PropertyChanged;

            public Books.IBookFixed Book { get { return _Book; } set { _Book = value; RaisePropertyChanged("Book"); } }
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