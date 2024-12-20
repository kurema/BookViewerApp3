﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace kurema.BrowserControl.ViewModels;
public interface IBrowserControlViewModel
{
    ObservableCollection<IBookmarkItem> BookmarkAddFolders { get; }
    ObservableCollection<IBookmarkItem> BookmarkCurrent { get; }
    IBookmarkItem BookmarkRoot { get; set; }
    bool ControllerCollapsed { get; set; }
    Func<Task<StorageFolder>> FolderProvider { get; set; }
    string HomePage { get; set; }
    ICommand OpenDownloadDirectoryCommand { get; set; }
    string SearchEngine { get; set; }
    void Search(string term);

    ISearchEngineEntry[] SearchEngines { get; set; }
    //ISearchEngineEntry SearchEngineDefault { get; set; }
}
