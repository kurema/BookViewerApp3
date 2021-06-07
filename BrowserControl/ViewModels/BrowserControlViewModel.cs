using System;
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

using System.Windows.Input;
using System.Collections.Generic;

namespace kurema.BrowserControl.ViewModels
{
    public class BrowserControlViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private WebView _Content;
        public WebView Content
        {
            get => _Content; set
            {
                if (_Content != null)
                {
                    _Content.NavigationStarting -= Content_NavigationStarting;
                    _Content.NavigationCompleted -= Content_NavigationCompleted;
                    _Content.NavigationFailed -= Content_NavigationFailed;
                    _Content.UnviewableContentIdentified -= _Content_UnviewableContentIdentified;
                }
                SetProperty(ref _Content, value);
                if (_Content != null)
                {
                    _Content.NavigationStarting += Content_NavigationStarting;
                    _Content.NavigationCompleted += Content_NavigationCompleted;
                    _Content.NavigationFailed += Content_NavigationFailed;
                    _Content.UnviewableContentIdentified += _Content_UnviewableContentIdentified;
                }
            }
        }

        private void _Content_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = true;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            LastErrorStatus = null;
            LastErrorUri = null;
            _Uri = _UriLastSuccess = _UriLastSuccessPrevious;
            OnPropertyChanged(nameof(this.Uri));
        }

        private ObservableCollection<DownloadItemViewModel> _Downloads = new ObservableCollection<DownloadItemViewModel>();
        public ObservableCollection<DownloadItemViewModel> Downloads { get => _Downloads; set => SetProperty(ref _Downloads, value); }

        public ObservableCollection<IBookmarkItem> BookmarkCurrent { get; } = new ObservableCollection<IBookmarkItem>();
        public ObservableCollection<IBookmarkItem> BookmarkAddFolders { get; } = new ObservableCollection<IBookmarkItem>();

        private bool _ControllerCollapsed = false;
        public bool ControllerCollapsed { get => _ControllerCollapsed; set => SetProperty(ref _ControllerCollapsed, value); }

        private IBookmarkItem _BookmarkProvider;
        public IBookmarkItem BookmarkRoot { get => _BookmarkProvider; set
            {
                SetProperty(ref _BookmarkProvider, value);
                if (value is null) return;
                Task.Run(async () =>
                {
                    BookmarkCurrent.Clear();
                    BookmarkAddFolders.Clear();
                    var bookmarkResult = await value.GetChilderenAsync();
                    if (bookmarkResult is null) return;
                    foreach (var item in bookmarkResult) BookmarkCurrent.Add(item);
                    foreach (var item in bookmarkResult?.Where(a => !a.IsReadOnly && a.IsFolder)) BookmarkAddFolders.Add(item);
                });
            }
        }


        private ICommand _OpenDownloadDirectoryCommand;
        public ICommand OpenDownloadDirectoryCommand
        {
            get
            {
                return _OpenDownloadDirectoryCommand = _OpenDownloadDirectoryCommand ?? new Helper.DelegateCommand(async (_) =>
                {
                    var folder = await FolderProvider?.Invoke();
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                });
            }
            set { _OpenDownloadDirectoryCommand = value; }
        }


        private Func<Task<Windows.Storage.StorageFolder>> _FolderProvider = async () => await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Download", Windows.Storage.CreationCollisionOption.OpenIfExists);
        public Func<Task<Windows.Storage.StorageFolder>> FolderProvider { get => _FolderProvider; set => SetProperty(ref _FolderProvider, value); }


        private string _HomePage;
        public string HomePage { get => _HomePage; set => SetProperty(ref _HomePage, value); }


        private string _SearchEngine = "https://www.google.com/search?q=%s";
        public string SearchEngine { get => _SearchEngine; set => SetProperty(ref _SearchEngine, value); }



        private string _Title;
        public string Title { get => _Title; set => SetProperty(ref _Title, value); }


        private Windows.Web.WebErrorStatus? _LastErrorStatus;
        public Windows.Web.WebErrorStatus? LastErrorStatus { get => _LastErrorStatus; set => SetProperty(ref _LastErrorStatus, value); }


        private Uri _LastErrorUri;
        public Uri LastErrorUri { get => _LastErrorUri; set => SetProperty(ref _LastErrorUri, value); }


        private void Content_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = true;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            LastErrorStatus = e.WebErrorStatus;
            LastErrorUri = e.Uri;
            _Uri = UriLastSuccess;
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = true;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            if (args.IsSuccess) UriLastSuccess = _Uri = args.Uri;
            OnPropertyChanged(nameof(Uri));
        }

        private void Content_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            GoBackForwardCommand.OnCanExecuteChanged();
            ReloadCommand.IsLoaded = false;
            ReloadCommand.OnCanExecuteChanged();
            Title = Content.DocumentTitle;
            _Uri = args.Uri;
            OnPropertyChanged(nameof(Uri));
        }


        private Uri _UriLastSuccessPrevious;
        private Uri _UriLastSuccess;
        public Uri UriLastSuccess
        {
            get => _UriLastSuccess; set
            {
                if (UriLastSuccess != value)
                {
                    _UriLastSuccessPrevious = _UriLastSuccess;
                    SetProperty(ref _UriLastSuccess, value);
                }
            }
        }

        private Uri _Uri;
        public string Uri
        {
            get => _Uri?.ToString() ?? Content?.Source?.ToString();
            set
            {
                try
                {
                    if (!value.Contains("://"))
                    {
                        if (value.Contains(".") && !value.Contains(" ") && !value.Contains("　"))
                        {
                            try
                            {
                                Content?.Navigate(new Uri("https://" + value));
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                var word = (this.SearchEngine ?? "https://www.google.com/search?q=%s").Replace("%s", value);
                                Content?.Navigate(new Uri(word));
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        Content?.Navigate(new Uri(value));
                    }
                }
                catch { }
            }
        }


        private Commands.GoBackForwardCommand _GoBackForwardCommand;
        public Commands.GoBackForwardCommand GoBackForwardCommand => _GoBackForwardCommand = _GoBackForwardCommand ?? new Commands.GoBackForwardCommand(this);

        private Commands.ReloadCommand _ReloadCommand;
        public Commands.ReloadCommand ReloadCommand => _ReloadCommand = _ReloadCommand ?? new Commands.ReloadCommand(this);

        private Commands.NavigateCommand _NavigateCommand;
        public Commands.NavigateCommand NavigateCommand => _NavigateCommand = _NavigateCommand ?? new Commands.NavigateCommand(this);

        public class Commands
        {
            public abstract class CommandBase : ICommand
            {
                public event EventHandler CanExecuteChanged;

                public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());


                protected BrowserControlViewModel _Content;
                public BrowserControlViewModel Content
                {
                    get => _Content;
                    set
                    {
                        if (_Content != null) _Content.PropertyChanged -= Value_PropertyChanged;
                        _Content = value;
                        if (_Content != null) _Content.PropertyChanged += Value_PropertyChanged;
                    }
                }

                private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(BrowserControlViewModel.Content))
                    {
                        if (_ContentView != null) _ContentView.ContentLoading -= UpdateContent;
                        _ContentView = Content.Content;
                        if (_ContentView != null) _ContentView.ContentLoading += UpdateContent;

                    }
                }

                private WebView _ContentView;

                public WebView ContentView
                {
                    get => Content.Content;
                }

                protected void UpdateContent(WebView sender, WebViewContentLoadingEventArgs args)
                {
                    CanExecuteChanged?.Invoke(this, new EventArgs());
                }

                public abstract bool CanExecute(object parameter);

                public abstract void Execute(object parameter);
            }

            public class GoBackForwardCommand : CommandBase
            {
                public GoBackForwardCommand()
                {
                }

                public GoBackForwardCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }


                private bool GetDirection(object s) => s?.ToString().ToLower() == "forward";

                public override bool CanExecute(object parameter)
                {
                    if (GetDirection(parameter)) return ContentView?.CanGoForward ?? false;
                    else return ContentView?.CanGoBack ?? false;
                }

                public override void Execute(object parameter)
                {
                    if (GetDirection(parameter)) ContentView?.GoForward();
                    else ContentView?.GoBack();
                }
            }

            public class ReloadCommand : CommandBase
            {
                public ReloadCommand()
                {
                }

                public ReloadCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }

                public bool IsLoaded { get; set; } = false;

                public override bool CanExecute(object parameter)
                {
                    return IsLoaded;
                }

                public override void Execute(object parameter)
                {
                    ContentView?.Refresh();
                }
            }

            public class NavigateCommand : CommandBase
            {
                public NavigateCommand()
                {
                }

                public NavigateCommand(BrowserControlViewModel model)
                {
                    this.Content = model;
                }


                public override bool CanExecute(object parameter)
                {
                    return Content?.Content != null && parameter != null;
                }

                public override void Execute(object parameter)
                {
                    if (Content != null) Content.Uri = parameter.ToString();
                }
            }
        }
    }
}
