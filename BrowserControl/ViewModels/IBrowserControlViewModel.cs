using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace kurema.BrowserControl.ViewModels;
public interface IBrowserControl2ViewModel
{
    ObservableCollection<IBookmarkItem> BookmarkAddFolders { get; }
    ObservableCollection<IBookmarkItem> BookmarkCurrent { get; }
    IBookmarkItem BookmarkRoot { get; set; }
    bool ControllerCollapsed { get; set; }
    Func<Task<StorageFolder>> FolderProvider { get; set; }
    string HomePage { get; set; }
    ICommand OpenDownloadDirectoryCommand { get; set; }
    string SearchEngine { get; set; }
}