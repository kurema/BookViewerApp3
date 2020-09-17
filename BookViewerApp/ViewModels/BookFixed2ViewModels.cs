﻿using System;
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
using System.Windows.Input;
using BookViewerApp.Helper;
using BookViewerApp.Managers;

using BookViewerApp.Storages;
using BookViewerApp.Views;

#nullable enable
namespace BookViewerApp.ViewModels
{
    public class BookViewModel : INotifyPropertyChanged, IBookViewModel, Helper.IDisposableBasic
    {
        public BookViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                (PageVisualAddCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
                (PageVisualSetCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
                (PageVisualMaxCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
            };
        }

        private Commands.ICommandEventRaiseable? _PageVisualAddCommand;
        private Commands.ICommandEventRaiseable? _PageVisualSetCommand;
        private Commands.ICommandEventRaiseable? _PageVisualMaxCommand;
        private Commands.ICommandEventRaiseable? _SwapReverseCommand;

        //private ICommand? _GoNextBookCommand;
        //private ICommand? _GoPreviousBookCommand;
        private ICommand? _ToggleFullScreenCommand;
        private ICommand? _GoToHomeCommand;


        //private ICommand _TogglePinCommand;
        //public ICommand TogglePinCommand => _TogglePinCommand = _TogglePinCommand ?? new DelegateCommand((a) => IsControlPinned = !IsControlPinned);


        public ICommand PageVisualAddCommand { get { return _PageVisualAddCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => a.PageSelectedVisual + b, (a, b) => a.PageSelectedVisual = b); } }
        public ICommand PageVisualSetCommand { get { return _PageVisualSetCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => b, (a, b) => a.PageSelectedVisual = b, a => a.PageSelectedVisual); } }
        public ICommand PageVisualMaxCommand { get { return _PageVisualMaxCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => a.PagesCount - 1, (a, b) => a.PageSelectedVisual = b, a => a.PageSelectedVisual); } }
        public ICommand SwapReverseCommand { get { return _SwapReverseCommand ??= new Commands.CommandBase((a) => { return true; }, (a) => { this.Reversed = !this.Reversed; }); } }
        public ICommand ToggleFullScreenCommand { get => _ToggleFullScreenCommand ??= new InvalidCommand(); set => _ToggleFullScreenCommand = value; }
        public ICommand GoToHomeCommand { get => _GoToHomeCommand ??= new InvalidCommand(); set => _GoToHomeCommand = value; }

        //public System.Windows.Input.ICommand GoNextBookCommand { get { return _GoNextBookCommand = _GoNextBookCommand ?? new Commands.AddNumberToSelectedBook(this, 1); } }
        //public System.Windows.Input.ICommand GoPreviousBookCommand { get { return _GoPreviousBookCommand = _GoPreviousBookCommand ?? new Commands.AddNumberToSelectedBook(this, -1); } }

        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(nameof(Title)); } }
        private string _Title = "";

        public bool IsControlPinned { get => _IsControlPinned; set { _IsControlPinned = value; OnPropertyChanged(nameof(IsControlPinned)); } }
        private bool _IsControlPinned = false;


        private SpreadPagePanel.ModeEnum _SpreadMode = SpreadPagePanel.ModeEnum.Single;
        public SpreadPagePanel.ModeEnum SpreadMode { get => _SpreadMode; set { _SpreadMode = value; OnPropertyChanged(nameof(SpreadMode)); } }

        public async void Initialize(Windows.Storage.IStorageFile value, Control? target = null)
        {
            this.Loading = true;
            var book = (await BookManager.GetBookFromFile(value));
            if (book is Books.IBookFixed bookf && bookf.PageCount > 0)
            {
                Initialize(bookf, target);
                this.Title = System.IO.Path.GetFileNameWithoutExtension(value.Name);
                this.Loading = false;

                if (await ThumbnailManager.GetImageFileAsync(bookf.ID) == null)
                {
                    var fileThumb = await ThumbnailManager.CreateImageFileAsync(bookf.ID);
                    try
                    {
                        var page = bookf.GetPage(0);
                        if (page != null) await page.SaveImageAsync(fileThumb, 500);
                        //if ((await fileThumb.GetBasicPropertiesAsync()).Size == 0) await fileThumb.DeleteAsync();
                    }
                    catch
                    {
                        //サムネイル作成が失敗しても大した問題はない。
                    }
                }
                {
                    await PathStorage.Content.GetContentAsync();
                    PathStorage.AddOrReplace(value.Path, bookf.ID);
                    await PathStorage.Content.SaveAsync();
                }
                //await HistoryStorage.AddHistory(value, bookf.ID);
                HistoryManager.AddEntry(value, bookf.ID);
            }
            this.Loading = false;
        }

        private string? Password = null;

        //To Dispose
        private Books.IBookFixed? Content;

        public async void Initialize(Books.IBookFixed value, Control? target = null)
        {
            this.Loading = true;
            if (BookInfo != null) SaveInfo();
            this.DisposeBasic();//変なタイミングだがこれだ正しい。
            this.Title = "";

            Content = value;

            var pages = new ObservableCollection<PageViewModel>();
            var option = OptionCache = target == null ? OptionCache : new Books.PageOptionsControl(target);
            for (uint i = 0; i < value.PageCount; i++)
            {
                uint page = i;
                pages.Add(new PageViewModel(new Books.VirtualPage(() =>
                {
                    var p = value.GetPage(page);
                    if (p == null) throw new ArgumentOutOfRangeException();
                    p.Option = option;
                    return p;
                }), this));
                if (i > 0) pages[(int)i - 1].NextPage = pages[(int)i];
            }
            this._Reversed = false;
            this._PageSelected = 0;
            ID = value.ID;
            Password = null;
            if (value is Books.IPasswordPdovider pp && pp.PasswordRemember) Password = pp.Password;
            this.PagesOriginal = pages;
            BookInfo = await BookInfoStorage.GetBookInfoByIDOrCreateAsync(value.ID);
            var tempPageSelected = (bool)SettingStorage.GetValue("SaveLastReadPage") ? (int)(BookInfo?.GetLastReadPage()?.Page ?? 1) : 1;
            this.PageSelectedDisplay = tempPageSelected == this.PagesCount ? 1 : tempPageSelected;
            {
                switch (BookInfo?.PageDirection)
                {
                    case Books.Direction.L2R:
                        this.Reversed = false;
                        break;
                    case Books.Direction.R2L:
                        this.Reversed = true;
                        break;
                    case Books.Direction.Default:
                    case null:
                    default:
                        bool defaultRev = (bool)SettingStorage.GetValue("DefaultPageReverse");
                        if ((bool)SettingStorage.GetValue("RespectPageDirectionInfo") && value is Books.IDirectionProvider dp)
                        {
                            switch (dp.Direction)
                            {
                                case Books.Direction.L2R:
                                    this.Reversed = false;
                                    break;
                                case Books.Direction.R2L:
                                    this.Reversed = true;
                                    break;
                                case Books.Direction.Default:
                                default:
                                    this.Reversed = defaultRev;
                                    break;
                            }
                        }
                        else
                        {
                            this.Reversed = defaultRev;
                        }
                        break;
                }
            }
            OnPropertyChanged(nameof(Reversed));
            //this.AsBookShelfBook = null;

            this.Bookmarks = new ObservableCollection<BookmarkViewModel>();
            {
                var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                var bm = new BookmarkViewModel() { Page = 1, AutoGenerated = true, Title = rl.GetString("BookmarkTop/Title") };
                this.Bookmarks.Add(bm);
            }
            if (BookInfo != null)
            {
                foreach (var bm in BookInfo.Bookmarks)
                {
                    this.Bookmarks.Add(new BookmarkViewModel(bm));
                }
            }
            {
                var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                var bm = new BookmarkViewModel() { Page = this.PagesCount, AutoGenerated = true, Title = rl.GetString("BookmarkLast/Title") };
                this.Bookmarks.Add(bm);
            }
            if (value is Books.ITocProvider pv)
            {
                this.Toc = new ObservableCollection<TocEntryViewModes>(pv?.Toc?.Select(a => new TocEntryViewModes(a)) ?? Array.Empty<TocEntryViewModes>());
            }
            this.Loading = false;
        }

        private Books.PageOptionsControl? OptionCache;

        private ObservableCollection<TocEntryViewModes> _Toc = new ObservableCollection<TocEntryViewModes>();
        public ObservableCollection<TocEntryViewModes> Toc { get => _Toc; set { _Toc = value; OnPropertyChanged(nameof(Toc)); } }


        public bool Loading { get { return _Loading; } set { _Loading = value; OnPropertyChanged(nameof(Loading)); } }
        private bool _Loading = true;

        private BookInfoStorage.BookInfo? BookInfo = null;
        public void SaveInfo()
        {
            if (BookInfo == null) return;
            BookInfo.Bookmarks.Clear();
            BookInfo.SetLastReadPage((uint)this.PageSelectedDisplay);
            foreach (var bm in this.Bookmarks)
            {
                if (!bm.AutoGenerated)
                    BookInfo.Bookmarks.Add(new BookInfoStorage.BookInfo.BookmarkItem() { Page = (uint)bm.Page, Title = bm.Title, Type = BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.UserDefined });
            }
            BookInfo.PageDirection = this.Reversed ? Books.Direction.R2L : Books.Direction.L2R;
            BookInfo.Password = this.Password;
        }

        public string? ID { get { return _ID; } private set { _ID = value; OnPropertyChanged(nameof(ID)); } }
        private string? _ID;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Windows.Foundation.TypedEventHandler<BookViewModel, (PageViewModel, PropertyChangedEventArgs)>? PagePropertyChanged;

        public void OnPagePropertyChanged(PageViewModel sender, PropertyChangedEventArgs args)
        {
            PagePropertyChanged?.Invoke(this, (sender, args));
        }


        public IPageViewModel? PageSelectedViewModel
        {
            get
            {
                if (PageSelected < 0 || PagesCount <= PageSelected) return null;
                return Pages[PageSelected];
            }
        }

        public int PageSelected
        {
            get { return _PageSelected; }
            set
            {
                if (value >= PagesCount) return;
                if (value < 0) _PageSelected = 0;
                else _PageSelected = value;
                OnPropertyChanged(nameof(PageSelectedDisplay));
                OnPropertyChanged(nameof(PageSelectedVisual));
                OnPropertyChanged(nameof(ReadRate));
                OnPropertyChanged(nameof(CurrentBookmarkName));
                OnPropertyChanged(nameof(PageSelected));
                OnPropertyChanged(nameof(PageSelectedViewModel));
            }
        }

        public int PageSelectedDisplay
        {
            get
            {
                //return _PageSelected + 1; 
                return PagesOriginal.ToList().FindIndex(a => a.Content == (PageSelectedViewModel as PageViewModel)?.Content) + 1;
            }
            set
            {
                //PageSelected = value - 1;
                var original = PagesOriginal.ToList();
                if (0 <= value - 1 && value - 1 < original.Count)
                {
                    var result = Pages.ToList().FindIndex(a => a.Content == original[value - 1].Content);
                    if (result != -1)
                    {
                        PageSelected = result;
                        return;
                    }
                    else if (1 <= value - 1)
                    {
                        result = Pages.ToList().FindIndex(a => a.Content == original[value - 2].Content);
                        if (result != -1)
                        {
                            PageSelected = result;
                            return;
                        }
                    }
                }
            }
        }
        private int _PageSelected = -1;

        public int PageSelectedVisual
        {
            get { return Reversed ? Math.Max(PagesCount - PageSelected - 1, 0) : _PageSelected; }
            set
            {
                PageSelected = Reversed ? PagesCount - value - 1 : value;
            }
        }

        public ObservableCollection<PageViewModel> Pages
        {
            get { return _Pages; }
            set { _Pages = value; OnPropertyChanged(nameof(Pages)); PageSelected = 0; OnPropertyChanged(nameof(PagesCount)); OnPropertyChanged(nameof(ReadRate)); }
        }
        private ObservableCollection<PageViewModel> _Pages = new ObservableCollection<PageViewModel>();

        public IEnumerable<PageViewModel> PagesOriginal
        {
            get { return _PagesOriginal; }
            set
            {
                _PagesOriginal = value;
                Pages = new ObservableCollection<PageViewModel>(value);
                OnPropertyChanged(nameof(PagesOriginal));
            }
        }
        IEnumerable<PageViewModel> _PagesOriginal = new PageViewModel[0];

        protected void RestorePages(PageViewModel[]? pagesToExclude = null, PageViewModel[]? pagesToInclude = null)
        {
            pagesToExclude ??= new PageViewModel[0];
            pagesToInclude ??= new PageViewModel[0];
            int count = 0;
            var currentPage = this.PageSelectedViewModel;
            foreach (var item in PagesOriginal)
            {
                if (pagesToInclude?.Contains(item) == true) continue;
                while (Pages.Count > count && pagesToInclude?.Contains(Pages[count]) == true) count++;

                if (Pages.Count > count)
                {
                    if (pagesToExclude?.Contains(Pages[count]) == true) Pages.RemoveAt(count);
                    else if (Pages[count].SpreadModeOverride == SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond)
                    {
                        if (count == PageSelected - 1) count++;
                        else Pages.RemoveAt(count);
                    }
                }
                if (pagesToExclude?.Contains(item) == true) continue;
                if (count >= Pages.Count) Pages.Add(item);
                else if (Pages[count] != item) Pages.Insert(count, item);
                count++;
            }
            if (currentPage is PageViewModel page) this._PageSelected = Pages.IndexOf(page);
#if DEBUG
            System.Diagnostics.Debug.Assert(this.SpreadMode == SpreadPagePanel.ModeEnum.Single || pagesToInclude.Count() > 0 || pagesToExclude.Count() > 0 || Enumerable.SequenceEqual(PagesOriginal.ToArray(), Pages.ToArray()));
#endif
        }

        public async void UpdatePages(Windows.UI.Core.CoreDispatcher dispatcher, Windows.UI.Core.CoreDispatcherPriority priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
            => await dispatcher.RunAsync(priority, () => this.UpdatePages());

        private void UpdatePages()
        {
            //ja:直接呼ばれると落ちたりするのでprivateにしました。
            //en:Calling this may crash the application, so this is private.
            switch (this.SpreadMode)
            {
                case SpreadPagePanel.ModeEnum.Spread:
                    {
                        if (!(this.PageSelectedViewModel is PageViewModel pageView)) return;
                        var pagesList = PagesOriginal.ToList();
                        var excluded = new List<PageViewModel>();
                        bool pageOverridePrevious = false;
                        int page = pagesList.IndexOf(pageView);
                        if (page >= 2 && pagesList[page - 2].SpreadDisplayedStatus == SpreadPagePanel.DisplayedStatusEnum.Spread) excluded.Add(pagesList[page - 1]);
                        else pageOverridePrevious = true;
                        if (pageView.SpreadDisplayedStatus == SpreadPagePanel.DisplayedStatusEnum.Spread && pageView.NextPage != null) excluded.Add(pageView.NextPage);
                        RestorePages(excluded.ToArray());

                        foreach (var item in Pages)
                        {
                            if (item == PageSelectedViewModel) continue;
                            if (pageOverridePrevious && page >= 1 && item == pagesList[page - 1]) item.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.ForceSingle;
                            else if (item.SpreadModeOverride != SpreadPagePanel.ModeOverrideEnum.Default) item.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.Default;
                        }
                    }
                    break;
                case SpreadPagePanel.ModeEnum.Single:
                    {
                        if (!(this.PageSelectedViewModel is PageViewModel pageView)) return;
                        var pagesList = PagesOriginal.ToList();
                        var included = new List<PageViewModel>();
                        int pageOrig = pagesList.IndexOf(pageView);
                        if (pageOrig >= 1 && pagesList[pageOrig - 1].SpreadDisplayedStatus == SpreadPagePanel.DisplayedStatusEnum.HalfFirst)
                        {
                            int pageCurrent = Pages.IndexOf(pagesList[pageOrig]);
                            int pagePrev = Pages.IndexOf(pagesList[pageOrig - 1]);
                            if (pagePrev != -1 && pageCurrent - pagePrev == 1)
                            {
                                var cloned = pagesList[pageOrig - 1].CloneBasic();
                                cloned.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond;
                                Pages.Insert(pageCurrent, cloned);
                                included.Add(cloned);
                            }
                            else if (pagePrev != -1 && pageCurrent - pagePrev == 2 && pageCurrent >= 1)
                            {
                                included.Add(Pages[pageCurrent - 1]);
                            }
                            this._PageSelected = Pages.IndexOf(pageView);
                        }

                        switch (pageView.SpreadDisplayedStatus)
                        {
                            case SpreadPagePanel.DisplayedStatusEnum.Single:
                                RestorePages(null, included.ToArray());
                                return;
                            case SpreadPagePanel.DisplayedStatusEnum.HalfFirst:
                                RestorePages(null, included.ToArray());
                                {
                                    var cloned = pageView.CloneBasic();
                                    cloned.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond;
                                    Pages.Insert(this._PageSelected + 1, cloned);
                                }
                                break;
                            case SpreadPagePanel.DisplayedStatusEnum.HalfSecond:
                                RestorePages(null, new PageViewModel[] { pageView });
                                return;
                            case SpreadPagePanel.DisplayedStatusEnum.Spread:
                            default:
                                return;
                        }
                    }
                    break;
                case SpreadPagePanel.ModeEnum.Default:
                    {
                        RestorePages();
                        Pages
                            .Where(a => a.SpreadModeOverride != SpreadPagePanel.ModeOverrideEnum.Default)
                            .ToList().ForEach(a => a.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.Default);
                    }
                    return;
                default:
                    return;
            }
        }

        public int PagesCount { get { return _Pages.Count; } }

        public double ReadRate
        {
            get { return Math.Min((double)PageSelectedDisplay / Pages.Count(), 1.0); }
            set { PageSelectedDisplay = (int)(value * Pages.Count()); OnPropertyChanged(nameof(ReadRate)); }
        }

        public string CurrentBookmarkName
        {
            get
            {
                foreach (var bm in this.Bookmarks)
                {
                    if (bm.Page == this.PageSelectedDisplay && bm.AutoGenerated == false)
                    {
                        return bm.Title;
                    }
                }
                return "";
            }
            set
            {
                foreach (var bm in this.Bookmarks)
                {
                    if (bm.Page == this.PageSelectedDisplay && bm.AutoGenerated == false)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            this.Bookmarks.Remove(bm);
                            goto BookmarkUpdate;
                        }
                        else
                        {
                            bm.Title = value;
                            goto BookmarkUpdate;
                        }
                    }
                }
                if (string.IsNullOrEmpty(value)) return;
                this.Bookmarks.Add(new BookmarkViewModel() { AutoGenerated = false, Page = this.PageSelectedDisplay, Title = value });
                BookmarksSort();

            BookmarkUpdate:
                OnPropertyChanged(nameof(CurrentBookmarkName));
                OnPropertyChanged(nameof(Bookmarks));
            }
        }

        public bool Reversed
        {
            get { return _Reversed; }
            set
            {
                if (Reversed != value)
                {
                    _Reversed = value;
                    OnPropertyChanged(nameof(Reversed));
                }
            }

        }
        private bool _Reversed = false;

        public ObservableCollection<BookmarkViewModel> Bookmarks
        {
            get { return _Bookmarks; }
            set { _Bookmarks = value; BookmarksSort(); OnPropertyChanged(nameof(Bookmarks)); OnPropertyChanged(nameof(CurrentBookmarkName)); }
        }


        private ObservableCollection<BookmarkViewModel> _Bookmarks = new ObservableCollection<BookmarkViewModel>();

        private void BookmarksSort()
        {
            _Bookmarks = new ObservableCollection<BookmarkViewModel>(_Bookmarks.ToList().OrderBy((a) => { return a.AutoGenerated ? -1 : a.Page; }));
        }

        public bool PageWithinRange(int page)
        {
            return this.PagesCount > page && page >= 0;
        }

        public void DisposeBasic()
        {
            if (PagesOriginal != null)
                foreach (var item in PagesOriginal)
                {
                    (item.Content as IDisposable)?.Dispose();
                    (item.Content as IDisposableBasic)?.DisposeBasic();
                }
            (this.Content as IDisposable)?.Dispose();
            (this.Content as IDisposableBasic)?.DisposeBasic();
        }
    }

    public class PageViewModel : INotifyPropertyChanged, IPageViewModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            Parent?.OnPagePropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public PageViewModel(Books.IPageFixed page, BookViewModel parent)
        {
            this.Content = page;
            this.Parent = parent;
        }

        public PageViewModel CloneBasic()
        {
            return new PageViewModel(this.Content, this.Parent) { NextPage = this.NextPage };
        }

        public Books.IPageFixed Content { get; set; }

        public BookViewModel Parent { get; private set; }


        private ICommand? _MoveCommand = null;
        public ICommand MoveCommand => _MoveCommand = _MoveCommand ?? new DelegateCommand((parameter) =>
        {
            var split = parameter.ToString().Split(',');
            if (split.Length >= 2)
            {
                float x, y;
                if (float.TryParse(split[0], out x) && float.TryParse(split[1], out y))
                {
                    OnMoveRequested(x, y);
                }
            }
        });


        private ICommand? _ZoomFactorMultiplyCommand = null;
        public ICommand ZoomFactorMultiplyCommand
        {
            get => _ZoomFactorMultiplyCommand = _ZoomFactorMultiplyCommand ?? new DelegateCommand((a) =>
            {
                var d = double.Parse(a.ToString());
                OnZoomRequested(ZoomFactor * (float)d);
            }
//,
//    (a) =>
//    {
//        var d = double.Parse(a.ToString());
//        return !(d < 1.0 && ZoomFactor <= 1.0);
//    }
);
        }

        public float ZoomFactor
        {
            get
            {
                return this._ZoomFactor;
            }
            set
            {
                this._ZoomFactor = MathF.Max(1.0f, value);
                OnPropertyChanged(nameof(ZoomFactor));
            }
        }

        private PageViewModel? _NextPage = null;
        public PageViewModel? NextPage
        {
            get => _NextPage;
            set
            {
                _NextPage = value;
                OnPropertyChanged(nameof(NextPage));
            }
        }


        private SpreadPagePanel.ModeOverrideEnum _SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.Default;
        public SpreadPagePanel.ModeOverrideEnum SpreadModeOverride
        {
            get => _SpreadModeOverride; set
            {
                _SpreadModeOverride = value;
                OnPropertyChanged(nameof(SpreadModeOverride));
            }
        }


        private SpreadPagePanel.DisplayedStatusEnum _SpreadDisplayedStatus = SpreadPagePanel.DisplayedStatusEnum.Single;
        public SpreadPagePanel.DisplayedStatusEnum SpreadDisplayedStatus
        {
            get => _SpreadDisplayedStatus; set
            {
                _SpreadDisplayedStatus = value;
                OnPropertyChanged(nameof(SpreadDisplayedStatus));
            }
        }

        public void OnZoomRequested(float factor)
        {
            ZoomRequested?.Invoke(this, new ZoomRequestedEventArgs(factor));
        }

        public void OnMoveRequested(float AddX, float AddY)
        {
            ZoomRequested?.Invoke(this, new ZoomRequestedEventArgs(AddX, AddY));
        }


        public ZoomRequestedEventHandler? ZoomRequested;
        public delegate void ZoomRequestedEventHandler(object sender, ZoomRequestedEventArgs e);
        public class ZoomRequestedEventArgs : EventArgs
        {
            public float? ZoomFactor = null;
            public ZoomRequestedEventArgs(float factor)
            {
                this.ZoomFactor = factor;
            }

            public float MoveHorizontal = 0;
            public float MoveVertical = 0;

            public ZoomRequestedEventArgs(float AddX, float AddY)
            {
                this.MoveHorizontal = AddX;
                this.MoveVertical = AddY;
            }
        }

        private float _ZoomFactor = 1.0f;

        public async void SetImageNoWait(BitmapImage im, System.Threading.CancellationToken token, System.Threading.SemaphoreSlim Semaphore)
        {
            if (im == null) return;
            await Semaphore.WaitAsync();
            try
            {
                token.ThrowIfCancellationRequested();
                await Content.SetBitmapAsync(im);
            }
            catch
            {
                // ignored
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }


    public class Commands
    {
        public interface ICommandEventRaiseable : System.Windows.Input.ICommand
        {
            void OnCanExecuteChanged();
        }

        public class PageSetGeneralCommand : ICommandEventRaiseable
        {

            public event EventHandler? CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }
            private BookViewModel model;

            public PageSetGeneralCommand(BookViewModel model, Func<BookViewModel, int, int> getPage, Action<BookViewModel, int> setPage, Func<BookViewModel, int>? checkSamePage = null)
            {
                this.model = model ?? throw new ArgumentNullException(nameof(model));
                GetPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
                SetPage = setPage ?? throw new ArgumentNullException(nameof(setPage));
                CheckSamePage = checkSamePage;
            }

            public Func<BookViewModel, int, int> GetPage { get; private set; }
            public Action<BookViewModel, int> SetPage { get; private set; }
            public Func<BookViewModel, int>? CheckSamePage { get; private set; }

            public bool CanExecute(object? parameter)
            {
                int number = 0;
                int.TryParse(parameter?.ToString(), out number);
                int result = GetPage(model, number);
                if (CheckSamePage != null && CheckSamePage(model) == result) return false;
                return model.PageWithinRange(result);
            }

            public void Execute(object? parameter)
            {
                int number = 0;
                int.TryParse(parameter?.ToString(), out number);
                SetPage(model, GetPage(model, number));
            }
        }

        public class AddCurrentPageToBookmark : ICommandEventRaiseable
        {
            public event EventHandler? CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private BookViewModel model;

            public AddCurrentPageToBookmark(BookViewModel model) { this.model = model; model.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(model.PageSelectedDisplay)) { OnCanExecuteChanged(); } }; }

            public bool CanExecute(object? parameter)
            {
                foreach (var bm in model.Bookmarks)
                {
                    if (bm.Page == model.PageSelectedDisplay && bm.AutoGenerated == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Execute(object? parameter)
            {
                model.Bookmarks.Add(new BookmarkViewModel() { AutoGenerated = false, Page = model.PageSelectedDisplay, Title = parameter as string ?? "" });
            }
        }

        public class CommandBase : ICommandEventRaiseable
        {
            public event EventHandler? CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            private Func<object?, bool> CanExecuteFunc;
            private Action<object?> ExecuteAction;

            public CommandBase(Func<object?, bool> CanExecuteFunc, Action<object?> ExecuteAction)
            {
                this.CanExecuteFunc = CanExecuteFunc;
                this.ExecuteAction = ExecuteAction;
            }

            public bool CanExecute(object? parameter)
            {
                return CanExecuteFunc(parameter);
            }

            public void Execute(object? parameter)
            {
                ExecuteAction(parameter);
            }
        }
    }

    public class BookmarkViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public BookmarkViewModel()
        {
            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
            _Title = rl.GetString("BookmarkBasic/Title");
            _Page = 1;
        }
        public BookmarkViewModel(BookInfoStorage.BookInfo.BookmarkItem item)
        {
            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
            this.Page = (int)item.Page;
            _Title = string.IsNullOrEmpty(item.Title) ? item.Type == BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.UserDefined ? rl.GetString("BookmarkBasic/Title") : rl.GetString("BookmarkLastread/Title") : item.Title;
            this.AutoGenerated = item.Type == BookInfoStorage.BookInfo.BookmarkItem.BookmarkItemType.LastRead;
        }

        public int Page
        {
            get { return _Page; }
            set { _Page = value; OnPropertyChanged(nameof(Page)); OnPropertyChanged(nameof(PageDisplay)); }
        }
        private int _Page;

        public string PageDisplay { get => Page.ToString(); }

        public string Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged(nameof(Title)); }
        }
        private string _Title;

        public bool AutoGenerated { get { return _AutoGenerated; } set { _AutoGenerated = value; OnPropertyChanged(nameof(_AutoGenerated)); } }
        private bool _AutoGenerated;
    }

    public class TocEntryViewModes : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
            Action? onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion


        private string _Title = "";
        public string Title { get => _Title; set => SetProperty(ref _Title, value); }


        private int _Page;
        public int Page { get => _Page; set => SetProperty(ref _Page, value); }


        private ObservableCollection<TocEntryViewModes> _Children = new ObservableCollection<TocEntryViewModes>();

        public TocEntryViewModes()
        {
        }

        public TocEntryViewModes(Books.TocItem tocItem)
        {
            this.Title = tocItem.Title;
            this.Page = tocItem.Page;
            this.Children = tocItem.Children == null ? new ObservableCollection<TocEntryViewModes>() : new ObservableCollection<TocEntryViewModes>(tocItem.Children.Select(a => new TocEntryViewModes(a)));
        }

        public ObservableCollection<TocEntryViewModes> Children { get => _Children; set => SetProperty(ref _Children, value); }
    }

    public interface IBookViewModel
    {
        ICommand PageVisualAddCommand { get; }
        ICommand PageVisualSetCommand { get; }
        ICommand PageVisualMaxCommand { get; }
        ICommand GoToHomeCommand { get; }
        ICommand SwapReverseCommand { get; }
        ICommand ToggleFullScreenCommand { get; }

        double ReadRate { get; }
        bool Reversed { get; }
        int PagesCount { get; }
        int PageSelectedDisplay { get; }
        bool IsControlPinned { get; }
        IPageViewModel? PageSelectedViewModel { get; }
        ObservableCollection<BookmarkViewModel> Bookmarks { get; }
        string CurrentBookmarkName { get; }
    }

    public interface IPageViewModel
    {
        ICommand ZoomFactorMultiplyCommand { get; }
        ICommand MoveCommand { get; }
        float ZoomFactor { get; set; }
    }
}
