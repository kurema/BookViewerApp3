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

namespace kurema.BrowserControl.ViewModels;
public class BrowserControl2ViewModel : INotifyPropertyChanged, IBrowserControlViewModel
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

    public ObservableCollection<IBookmarkItem> BookmarkCurrent { get; } = new ObservableCollection<IBookmarkItem>();
    public ObservableCollection<IBookmarkItem> BookmarkAddFolders { get; } = new ObservableCollection<IBookmarkItem>();

    private bool _ControllerCollapsed = false;
    public bool ControllerCollapsed { get => _ControllerCollapsed; set => SetProperty(ref _ControllerCollapsed, value); }

    private IBookmarkItem _BookmarkProvider;
    public IBookmarkItem BookmarkRoot
    {
        get => _BookmarkProvider; set
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

    private Uri _Source;
    public Uri Source
    {
        get => _Source; set
        {
            SetProperty(ref _Source, value);
            OnPropertyChanged(nameof(SourceString));
        }
    }


    private ICommand _OpenDownloadDirectoryCommand;
    public ICommand OpenDownloadDirectoryCommand
    {
        get
        {
            return _OpenDownloadDirectoryCommand ??= new Helper.DelegateCommand(async (_) =>
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

    private bool _CanGoBack;
    public bool CanGoBack { get => _CanGoBack; set => SetProperty(ref _CanGoBack, value); }


    private bool _CanGoForward;
    public bool CanGoForward { get => _CanGoForward; set => SetProperty(ref _CanGoForward, value); }

    public string SourceString
    {
        get => Source?.ToString();
        set
        {
            void SetUriString(string value)
            {
                void ParseAndSetSource(string uri)
                {
                    if (Uri.TryCreate(uri, UriKind.Absolute, out var result))
                    {
                        this.Source = result;
                    }
                }

                if (value.Contains(".") && !value.Contains(" ") && !value.Contains("　"))
                {
                    try
                    {
                        ParseAndSetSource($"https://{value}");
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        var word = (this.SearchEngine ?? "https://www.google.com/search?q=%s").Replace("%s", value);
                        ParseAndSetSource(word);
                    }
                    catch { }
                }
            }

            try
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var result))
                {
                    this.Source = result;
                    //Disallow edge:// or other random schemes.
                    //if (result.Scheme.ToUpperInvariant() is "HTTP" or "HTTPS")
                    //{
                    //    this.Source = result;
                    //}
                    //else
                    //{
                    //    SetUriString(value);
                    //}
                }
                else
                {
                    SetUriString(value);
                }
            }
            catch { }
        }
    }

}

