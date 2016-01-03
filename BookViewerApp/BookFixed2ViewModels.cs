using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Data;

namespace BookViewerApp.BookFixed2ViewModels
{
    public class BookViewModel : INotifyPropertyChanged
    {
        public BookViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                PageVisualAddCommand.OnCanExecuteChanged();
                PageVisualSetCommand.OnCanExecuteChanged();
                PageVisualMaxCommand.OnCanExecuteChanged();
            };
        }

        private ICommandEventRaiseable _PageVisualAddCommand;
        private ICommandEventRaiseable _PageVisualSetCommand;
        private ICommandEventRaiseable _PageVisualMaxCommand;
        private ICommandEventRaiseable _SwapReverseCommand;

        public ICommandEventRaiseable PageVisualAddCommand { get {return _PageVisualAddCommand = _PageVisualAddCommand?? new PageVisualAddCommand(this); } }
        public ICommandEventRaiseable PageVisualSetCommand { get { return _PageVisualSetCommand = _PageVisualSetCommand ?? new PageVisualSetCommand(this); } }
        public ICommandEventRaiseable PageVisualMaxCommand { get { return _PageVisualMaxCommand = _PageVisualMaxCommand ?? new PageVisualMaxCommand(this); } }
        public ICommandEventRaiseable SwapReverseCommand { get { return _SwapReverseCommand = _SwapReverseCommand ?? new CommandBase((a)=> { return true; },(a)=> { this.Reversed = !this.Reversed; }); } }

        public async void Initialize(Books.IBookFixed value, Control target)
        {
            if (BookInfo != null) SaveInfo();

            var pages = new ObservableCollection<PageViewModel>();
            var option = new Books.PageOptionsControl(target);
            for (uint i = 0; i < value.PageCount; i++)
            {
                uint page = i;
                pages.Add(new PageViewModel(new Books.VirtualPage(() => { var p = value.GetPage(page); p.Option = option; return p; })));
            }
            this._Reversed = false;
            this._PageSelected = 0;
            this.Pages = pages;
            BookInfo = await BookInfoStorage.GetBookInfoByIDOrCreateAsync(value.ID);
            this.PageSelected = (int)(BookInfo?.GetLastReadPage()?.Page ?? 1);
            this.Reversed = BookInfo?.PageReversed ?? false;
        }

        private BookInfoStorage.BookInfo BookInfo=null;//ID to save
        public void SaveInfo()
        {
            BookInfo.SetLastReadPage((uint)this.PageSelected);
            BookInfo.PageReversed = this.Reversed;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public int PageSelected
        {
            get { return _PageSelected+1; }
            set { _PageSelected = value-1; OnPropertyChanged(nameof(PageSelected)); OnPropertyChanged(nameof(PageSelectedVisual)); }
        }
        private int _PageSelected = -1;

        public int PageSelectedVisual
        {
            get { return Reversed ? Pages.Count() - _PageSelected - 1 : _PageSelected; }
            set { _PageSelected = Reversed ? Pages.Count() - value - 1 : value; OnPropertyChanged(nameof(PageSelected)); OnPropertyChanged(nameof(PageSelectedVisual)); }
        }

        public ObservableCollection<PageViewModel> Pages
        {
            get { return _Pages; }
            set { _Pages = value;OnPropertyChanged(nameof(Pages));OnPropertyChanged(nameof(PagesCount)); }
        }
        private ObservableCollection<PageViewModel> _Pages = new ObservableCollection<PageViewModel>();

        public int PagesCount { get { return _Pages.Count(); } }

        public double ReadRate
        {
            get { return (double)PageSelected / Pages.Count(); }
            set { PageSelected = (int)(value * Pages.Count);  OnPropertyChanged(nameof(ReadRate)); }
        }

        public bool Reversed
        {
            get { return _Reversed; }
            set
            {
                if (Reversed != value)
                {
                    var oldPage = this.PageSelected;
                    _Reversed = value;
                    OnPropertyChanged(nameof(Reversed));
                    Pages = new ObservableCollection<PageViewModel>(Pages.Reverse());
                    PageSelected = oldPage;
                }
            }

        }
        private bool _Reversed = false;
    }

    public class PageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public PageViewModel(Books.IPageFixed page) { this.Page = page; }

        private Books.IPageFixed Page;

        public ImageSource Source { get { var src = new BitmapImage();SetImageNoWait(src); return src; } }

        private async void SetImageNoWait(BitmapImage im)
        {
            await Page.SetBitmapAsync(im);
        }
    }

    public interface ICommandEventRaiseable: System.Windows.Input.ICommand
    {
        void OnCanExecuteChanged();
    }

    public class PageVisualAddCommand : ICommandEventRaiseable
    {
        public event EventHandler CanExecuteChanged;
        public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

        private BookViewModel model;

        public PageVisualAddCommand(BookViewModel model) { this.model = model; }

        public bool CanExecute(object parameter)
        {
            return model.PagesCount > model.PageSelectedVisual + int.Parse(parameter as string) && model.PageSelectedVisual >= -int.Parse(parameter as string);
        }

        public void Execute(object parameter)
        {
            model.PageSelectedVisual += int.Parse(parameter as string);
        }
    }

    public class PageVisualSetCommand : ICommandEventRaiseable
    {
        public event EventHandler CanExecuteChanged;
        public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

        private BookViewModel model;

        public PageVisualSetCommand(BookViewModel model) { this.model = model; }

        public bool CanExecute(object parameter)
        {
            return model.PagesCount > int.Parse(parameter as string) && 0 <= int.Parse(parameter as string) && model.PageSelectedVisual != int.Parse(parameter as string);
        }

        public void Execute(object parameter)
        {
            model.PageSelectedVisual = int.Parse(parameter as string);
        }
    }

    public class PageVisualMaxCommand : ICommandEventRaiseable
    {
        public event EventHandler CanExecuteChanged;
        public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

        private BookViewModel model;

        public PageVisualMaxCommand(BookViewModel model) { this.model = model; }

        public bool CanExecute(object parameter)
        {
            return model.PagesCount > 0 && model.PageSelectedVisual != model.PagesCount - 1;
        }

        public void Execute(object parameter)
        {
            model.PageSelectedVisual = model.PagesCount - 1;
        }
    }

    public class CommandBase : ICommandEventRaiseable
    {
        public event EventHandler CanExecuteChanged;
        public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

        private Func<object, bool> CanExecuteFunc;
        private Action<object> ExecuteAction;

        public CommandBase(Func<object, bool> CanExecuteFunc, Action<object> ExecuteAction)
        {
            this.CanExecuteFunc = CanExecuteFunc;
            this.ExecuteAction = ExecuteAction;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc(parameter);
        }

        public void Execute(object parameter)
        {
            ExecuteAction(parameter);
        }
    }

}
