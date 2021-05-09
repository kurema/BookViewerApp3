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
        private Commands.ICommandEventRaiseable? _PageSetCommand;
        private Commands.ICommandEventRaiseable? _PageMaxCommand;
        private Commands.ICommandEventRaiseable? _SwapReverseCommand;

        private Commands.ICommandEventRaiseable? _ShiftBookCommand;
        private ICommand? _ToggleFullScreenCommand;
        private ICommand? _GoToHomeCommand;

        public ObservableCollection<kurema.FileExplorerControl.Models.FileItems.IFileItem> ContainerItems { get; } = new ObservableCollection<kurema.FileExplorerControl.Models.FileItems.IFileItem>();

        private kurema.FileExplorerControl.Models.FileItems.IFileItem? _FileItem;
        public kurema.FileExplorerControl.Models.FileItems.IFileItem? FileItem
        {
            get => _FileItem; set
            {
                if (_FileItem == value) return;
                if (value?.IsFolder == true)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"{nameof(FileItem)} should not be folder!");
#endif
                    return;
                }
                _FileItem = value;
                OnPropertyChanged(nameof(FileItem));
            }
        }


        //private ICommand _TogglePinCommand;
        //public ICommand TogglePinCommand => _TogglePinCommand = _TogglePinCommand ?? new DelegateCommand((a) => IsControlPinned = !IsControlPinned);


        public ICommand PageVisualAddCommand { get { return _PageVisualAddCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => a.PageSelectedVisual + b, (a, b) => a.PageSelectedVisual = b); } }
        public ICommand PageVisualSetCommand { get { return _PageVisualSetCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => b, (a, b) => a.PageSelectedVisual = b, a => a.PageSelectedVisual); } }
        public ICommand PageVisualMaxCommand { get { return _PageVisualMaxCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => a.Pages.Count - 1, (a, b) => a.PageSelectedVisual = b, a => a.PageSelectedVisual); } }
        public ICommand SwapReverseCommand { get { return _SwapReverseCommand ??= new Commands.CommandBase((a) => { return true; }, (a) => { this.Reversed = !this.Reversed; }); } }
        public ICommand ToggleFullScreenCommand { get => _ToggleFullScreenCommand ??= new InvalidCommand(); set => _ToggleFullScreenCommand = value; }
        public ICommand GoToHomeCommand { get => _GoToHomeCommand ??= new InvalidCommand(); set => _GoToHomeCommand = value; }

        public ICommand PageSetCommand { get { return _PageSetCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => b, (a, b) => a.PageSelected = b, a => a.PageSelected); } }
        public ICommand PageMaxCommand { get { return _PageMaxCommand ??= new Commands.PageSetGeneralCommand(this, (a, b) => a.Pages.Count - 1, (a, b) => a.PageSelected = b, a => a.PageSelected); } }


        public Commands.ICommandEventRaiseable ShiftBookCommand { get { return _ShiftBookCommand = _ShiftBookCommand ?? new Commands.ShiftSelectedBook(this); } }

        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(nameof(Title)); } }
        private string _Title = "";

        public bool IsControlPinned { get => _IsControlPinned; set { _IsControlPinned = value; OnPropertyChanged(nameof(IsControlPinned)); } }
        private bool _IsControlPinned = false;


        private SpreadPagePanel.ModeEnum _SpreadMode = SpreadPagePanel.ModeEnum.Default;
        public SpreadPagePanel.ModeEnum SpreadMode { get => _SpreadMode; set { _SpreadMode = value; OnPropertyChanged(nameof(SpreadMode)); } }

        public async Task UpdateContainerInfo(Windows.Storage.IStorageFile value)
        {
            if (value is null) return;

            ContainerItems.Clear();
            var parent = await (value as Windows.Storage.StorageFile)?.GetParentAsync();
            if (parent is null)
            {
                FileItem = new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(value);
                return;
            }
            var children = (await new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(parent).GetChildren()).Where(a => BookManager.IsFileAvailabe(a.Path) && !BookManager.IsEpub(a.Path));
            foreach (var item in children)
            {
                ContainerItems.Add(item);
            }
            FileItem = ContainerItems.FirstOrDefault(a => (a as kurema.FileExplorerControl.Models.FileItems.StorageFileItem)?.Content.Path == value.Path) ??
                new kurema.FileExplorerControl.Models.FileItems.StorageFileItem(value);
        }

        public async void Initialize(Windows.Storage.IStorageFile value, Control? target = null)
        {
            this.Loading = true;

            if (value is null) return;

            var book = (await BookManager.GetBookFromFile(value));
            if (book is Books.IBookFixed bookf && bookf.PageCount > 0)
            {
                Initialize(bookf, target);
                this.Title = System.IO.Path.GetFileNameWithoutExtension(value.Name);
                this.Loading = false;

                if (await ThumbnailManager.GetImageFileAsync(bookf.ID) is null)
                {
                    try
                    {
                        var fileThumb = await ThumbnailManager.CreateImageFileAsync(bookf.ID);
                        var page = bookf.GetPage(0);
                        if (page != null) await page.SaveImageAsync(fileThumb, 500);
                        //if ((await fileThumb.GetBasicPropertiesAsync()).Size == 0) await fileThumb.DeleteAsync();
                    }
                    catch
                    {
                        //サムネイル作成が失敗しても大した問題はない。
                        //Failing to create thumbnail is not a big deal.
                    }
                }
                if (bookf.ID != null)
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

        private bool ShouldBeReversed(Books.IBookFixed value)
        {
            bool respectInfo= (bool)SettingStorage.GetValue("RememberPageDirection"); 

            switch (BookInfo?.PageDirection)
            {
                case Books.Direction.L2R when respectInfo:
                    return false;
                case Books.Direction.R2L when respectInfo:
                    return true;
                case Books.Direction.Default:
                case null:
                default:
                    bool defaultRev = (bool)SettingStorage.GetValue("DefaultPageReverse");
                    if ((bool)SettingStorage.GetValue("RespectPageDirectionInfo") && value is Books.IDirectionProvider dp)
                    {
                        switch (dp.Direction)
                        {
                            case Books.Direction.L2R:
                                return false;
                            case Books.Direction.R2L:
                                return true;
                            case Books.Direction.Default:
                            default:
                                return defaultRev;
                        }
                    }
                    else
                    {
                        return defaultRev;
                    }
            }
        }

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
                    if (p is null) throw new ArgumentOutOfRangeException();
                    //if (p is Books.PdfPage pdf) pdf.Option = option;// Is this OK?
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
            BookInfo = value.ID == null ? null : await BookInfoStorage.GetBookInfoByIDOrCreateAsync(value.ID);
            var tempPageSelected = (bool)SettingStorage.GetValue("SaveLastReadPage") ? (int)(BookInfo?.GetLastReadPage()?.Page ?? 1) : 1;
            if (SettingStorage.GetValue("DefaultSpreadType") is SpreadPagePanel.ModeEnum modeSpread) this.SpreadMode = modeSpread;
            this.PageSelectedDisplay = tempPageSelected == this.PagesCount ? 1 : tempPageSelected;
            this.Reversed = ShouldBeReversed(value);
            OnPropertyChanged(nameof(Reversed));
            //this.AsBookshelfBook = null;

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

            _ShiftBookCommand?.OnCanExecuteChanged();
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
            if (BookInfo is null) return;
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
                if (PageSelected < 0 || Pages.Count <= PageSelected) return null;
                return Pages[PageSelected];
            }
        }

        public int PageSelected
        {
            get { return _PageSelected; }
            set
            {
                if (value >= Pages.Count()) return;
                if (_PageSelected == value) return;
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

        //Use this if you need actual page displayed (in spread view, this add 2 each time you scroll). This start from 1 for compatibility reason.
        //こっちは実際に画面に表示されているページ数です。例えば見開きモードだとPageSelectedは見開き一つを1ページカウントしますが、PageSelectedDisplayの場合画面に表示されている実際のページを示します。
        //その為にわざわざLinqを使って検索してるわけですね。泥臭いけど大して遅くない。歴史的な理由(ViewModelとして画面表示で利用していた/いる)で1始まりです。
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
                value = Math.Max(value, 1);
                //if (value == original.Count)
                //{
                //    PageSelected = Pages.Count - 1;
                //    return;
                //}
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
            get { return Reversed ? Math.Max(Pages.Count - PageSelected - 1, 0) : _PageSelected; }
            set
            {
                PageSelected = Reversed ? Pages.Count - value - 1 : value;
            }
        }

        public int PagesCount { get { return PagesOriginal.Count(); } }

        public double ReadRate
        {
            get { return Math.Min((double)PageSelectedDisplay / PagesOriginal.Count(), 1.0); }
            set { PageSelectedDisplay = (int)Math.Clamp(Math.Round(value * PagesOriginal.Count()), 1, PagesOriginal.Count()); OnPropertyChanged(nameof(ReadRate)); }
        }


        public ObservableCollection<PageViewModel> Pages
        {
            get { return _Pages; }
            set { _Pages = value; OnPropertyChanged(nameof(Pages)); PageSelected = 0; }
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
                OnPropertyChanged(nameof(PagesCount));
                OnPropertyChanged(nameof(ReadRate));
            }
        }
        IEnumerable<PageViewModel> _PagesOriginal = new PageViewModel[0];

        protected void RestorePages(PageViewModel[]? pagesToExclude = null, PageViewModel[]? pagesToInclude = null)
        {
            if (PagesOriginal.Count() == 0)
            {
                Pages.Clear();
                return;
            }
            pagesToExclude ??= new PageViewModel[0];
            pagesToInclude ??= new PageViewModel[0];
            int count = 0;
            var currentPage = this.PageSelectedViewModel;
            foreach (var item in PagesOriginal)
            {
                if (pagesToInclude?.Contains(item) == true) continue;
                while (Pages.Count > count && pagesToInclude?.Contains(Pages[count]) == true) count++;
                //while (Pages.Count > count && Pages[count].SpreadModeOverride == SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond) Pages.RemoveAt(count);
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
            while (!pagesToExclude.Contains(PagesOriginal.Last()) && Pages.Last() != PagesOriginal.Last() && !pagesToInclude.Contains(Pages.Last())) Pages.RemoveAt(Pages.Count - 1);
            if (currentPage is PageViewModel page)
            {
                int index = Pages.IndexOf(page);
                if (index != -1) this._PageSelected = index;
            }
#if DEBUG
            //System.Diagnostics.Debug.Assert(this.SpreadMode == SpreadPagePanel.ModeEnum.Single || pagesToInclude.Count() > 0 || pagesToExclude.Count() > 0 || Enumerable.SequenceEqual(PagesOriginal.ToArray(), Pages.ToArray()));
#endif
        }

        public async Task UpdatePages(Windows.UI.Core.CoreDispatcher dispatcher, Windows.UI.Core.CoreDispatcherPriority priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            if (this.SpreadMode == SpreadPagePanel.ModeEnum.Spread) await Task.Delay(500);//Is 500msec good?
            await dispatcher.RunAsync(priority, () => this.UpdatePages());
        }

        private void UpdatePages()
        {
            //ja:直接呼ばれると落ちたりするのでprivateにしました。
            //en:Calling this may crash the application, so this is private.

            void CommandCanExecuteUpdate()
            {
                (PageVisualAddCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
                (PageVisualSetCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
                (PageVisualMaxCommand as Commands.ICommandEventRaiseable)?.OnCanExecuteChanged();
            }

            switch (this.SpreadMode)
            {
                case SpreadPagePanel.ModeEnum.ForceSpread:
                    {
                        int i = 0;
                        int pageCount = PagesOriginal.Count();
                        foreach (var item in PagesOriginal)
                        {
                            var contains = Pages.Contains(item);
                            if (i % 2 == 0)
                            {
                                if(!contains) Pages.Add(item);
                                item.SpreadModeOverride = i== pageCount -1?
                                    SpreadPagePanel.ModeOverrideEnum.ForceSingle :
                                    SpreadPagePanel.ModeOverrideEnum.Default;
                            }
                            else if (i%2 == 1 && contains){
                                Pages.Remove(item);
                            }
                            i++;
                        }
                    }
                    break;
                case SpreadPagePanel.ModeEnum.ForceSpreadFirstSingle:
                    {
                        int i = 0;
                        int pageCount = PagesOriginal.Count();

                        foreach (var item in PagesOriginal)
                        {
                            var contains = Pages.Contains(item);
                            if (i == 0)
                            {
                                if (!contains)
                                {
                                    Pages.Add(item);
                                }
                                item.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.ForceSingle;
                            }
                            else if (i % 2 == 1)
                            {
                                if (!contains) Pages.Add(item);
                                item.SpreadModeOverride = i == pageCount - 1 ?
                                    SpreadPagePanel.ModeOverrideEnum.ForceSingle :
                                    SpreadPagePanel.ModeOverrideEnum.Default;
                            }
                            else if (i % 2 == 0 && contains)
                            {
                                Pages.Remove(item);
                            }
                            i++;
                        }
                    }
                    break;
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
                        CommandCanExecuteUpdate();
                    }
                    break;
                case SpreadPagePanel.ModeEnum.Single:
                    {
                        if (!(this.PageSelectedViewModel is PageViewModel pageView)) return;
                        int lastIndex = -1;
                        foreach (var item in PagesOriginal)
                        {
                            var index = Pages.IndexOf(item);
                            var nextItem = index >= 0 && index + 1 < Pages.Count ? Pages[index + 1] : null;
                            if (index == -1) Pages.Insert(lastIndex = index = lastIndex + 1, item);
                            else lastIndex = index;
                            bool nextHalfSecond = nextItem?.Content == item.Content && nextItem?.SpreadModeOverride == SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond;

                            switch (item.SpreadDisplayedStatus)
                            {
                                case SpreadPagePanel.DisplayedStatusEnum.Single:
                                    if (nextHalfSecond)
                                    {
                                        Pages.RemoveAt(index + 1);
                                    }
                                    break;
                                case SpreadPagePanel.DisplayedStatusEnum.HalfFirst:
                                    if (!nextHalfSecond)
                                    {
                                        var cloned = item.CloneBasic();
                                        cloned.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.ForceHalfSecond;
                                        Pages.Insert(index + 1, cloned);
                                    }
                                    lastIndex++;
                                    break;
                                case SpreadPagePanel.DisplayedStatusEnum.Spread:
                                case SpreadPagePanel.DisplayedStatusEnum.HalfSecond:
                                default:
#if DEBUG
                                    System.Diagnostics.Debug.Fail("This should not happend");
#endif
                                    break;
                            }
                        }
                        {
                            int index = Pages.IndexOf(pageView);
                            if (index != -1) this._PageSelected = index;
                        }
                        break;
                    }
                case SpreadPagePanel.ModeEnum.Default:
                    {
                        RestorePages();
                        Pages
                            .Where(a => a.SpreadModeOverride != SpreadPagePanel.ModeOverrideEnum.Default)
                            .ToList().ForEach(a => a.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.Default);
                        CommandCanExecuteUpdate();
                    }
                    return;
                default:
                    return;
            }
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
            return this.Pages.Count > page && page >= 0;
        }

        public void UpdateSettings()
        {
            if (Content != null) this.Reversed = ShouldBeReversed(Content);
            if (SettingStorage.GetValue("DefaultSpreadType") is SpreadPagePanel.ModeEnum modeSpread) this.SpreadMode = modeSpread;
            foreach (var item in PagesOriginal)
            {
                item.SpreadModeOverride = SpreadPagePanel.ModeOverrideEnum.Default;
                item.SpreadDisplayedStatus = SpreadPagePanel.DisplayedStatusEnum.Single;
            }
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

        public async void SetImageNoWait(BitmapImage im, System.Threading.CancellationToken token, System.Threading.SemaphoreSlim Semaphore, double width, double height)
        {
            if (im is null) return;
            await Semaphore.WaitAsync();
            try
            {
                token.ThrowIfCancellationRequested();
                await Content.SetBitmapAsync(im, width, height);
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

        public class ShiftSelectedBook : ICommandEventRaiseable
        {
            public event EventHandler? CanExecuteChanged;
            public void OnCanExecuteChanged() { if (CanExecuteChanged != null) CanExecuteChanged(this, new EventArgs()); }

            public bool CanExecute(object parameter)
            {
                if (model?.FileItem == null || parameter is null) return false;
                if (!int.TryParse(parameter.ToString(), out int shift)) return false;
                var index = model.ContainerItems.IndexOf(model.FileItem) + shift;
                return 0 <= index && index < model.ContainerItems.Count;
            }

            public void Execute(object parameter)
            {
                if (model?.FileItem == null || parameter is null) return;
                if (!int.TryParse(parameter.ToString(), out int shift)) return;
                var index = model.ContainerItems.IndexOf(model.FileItem) + shift;
                if (!(0 <= index && index < model.ContainerItems.Count)) return;

                if (!(model.ContainerItems[index] is kurema.FileExplorerControl.Models.FileItems.StorageFileItem result)) return;
                if (!(result.Content is Windows.Storage.IStorageFile content)) return;
                model.FileItem = result;
                model.Initialize(content);
            }

            private BookViewModel model;

            public ShiftSelectedBook(BookViewModel model)
            {
                this.model = model ?? throw new ArgumentNullException(nameof(model));
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
