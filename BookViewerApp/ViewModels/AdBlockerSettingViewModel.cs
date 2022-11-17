﻿using BookViewerApp.Helper;
using BookViewerApp.Storages.ExtensionAdBlockerItems;
using kurema.FileExplorerControl.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable
namespace BookViewerApp.ViewModels;
public class AdBlockerSettingViewModel : ViewModelBase
{
    public AdBlockerSettingViewModel()
    {
        RefreshCommand = new DelegateCommand(async _ => await RefreshSelectedAsync(), _ =>
        {
            var info = Managers.ExtensionAdBlockerManager.LocalInfo.Content;
            if (RefreshAll) return true;
            foreach (var g in this.FilterList)
            {
                foreach (var i in g)
                {
                    if (i.IsRefreshRequested && i.IsEnabled) return true;
                    if (i.IsEnabled && !i.IsLoaded) return true;
                    if (info is not null && i.IsEnabled != info.selected.Any(a => a.filename == i.FileName)) return true;
                }
            }
            return false;
        });
    }

    public ObservableCollection<AdBlockerSettingFilterGroupViewModel> FilterList { get; private set; } = new ObservableCollection<AdBlockerSettingFilterGroupViewModel>();

    public async Task<bool> RefreshSelectedAsync()
    {
        bool success = true;
        foreach (var g in FilterList)
        {
            foreach (var i in g)
            {
                if (i.IsEnabled && (i.IsRefreshRequested || RefreshAll || !(i.IsLoaded = await Managers.ExtensionAdBlockerManager.IsItemLoaded(i.GetContent()))))
                {
                    bool successLocal = await Managers.ExtensionAdBlockerManager.TryDownloadList(i.GetContent());
                    if (successLocal)
                    {
                        i.IsRefreshRequested = false;
                        i.IsLoaded = true;
                        i.DownloadStatus = AdBlockerSettingFilterViewModel.DownloadSuccessStatus.Success;
                    }
                    else
                    {
                        i.DownloadStatus = AdBlockerSettingFilterViewModel.DownloadSuccessStatus.Fail;
                    }
                    success &= successLocal;
                }
                if (!i.IsEnabled)
                {
                    i.DownloadStatus = AdBlockerSettingFilterViewModel.DownloadSuccessStatus.NoMessage;
                    await Managers.ExtensionAdBlockerManager.TryRemoveListFromInfo(i.FileName);
                }
            }
        }
        if (success)
        {
            RefreshAll = false;
            SetupRequired = false;
        }
        await Managers.ExtensionAdBlockerManager.LoadRulesFromText();
        await Managers.ExtensionAdBlockerManager.LocalInfo.SaveAsync();
        return success;
    }

    public async Task LoadFilterList()
    {
        var filter = await Managers.ExtensionAdBlockerManager.LocalLists.GetContentAsync();
        var info = await Managers.ExtensionAdBlockerManager.LocalInfo.GetContentAsync();

        FilterList = new ObservableCollection<AdBlockerSettingFilterGroupViewModel>(filter?.group?.Select(a => new AdBlockerSettingFilterGroupViewModel(a) { Parent = this }) ?? new AdBlockerSettingFilterGroupViewModel[0]);
        if (info.filters?.Length > 0)
        {
            itemsGroup gr = new()
            {
                title = new[] { new title() { Value = Managers.ResourceManager.Loader.GetString("Extension/AdBlocker/Filters/CustomGroup/Header"), @default = true } },
                item = info.filters,
            };
            FilterList.Add(new AdBlockerSettingFilterGroupViewModel(gr) { Parent = this });
        }
        foreach (var g in FilterList)
        {
            foreach (var i in g)
            {
                var c = i.GetContent();
                var b = await Managers.ExtensionAdBlockerManager.IsItemLoaded(c);
                SetupRequired &= !b;
                i.IsLoaded = b;
                i.IsEnabled = info.selected.Any(a => a.filename == c.filename);
                if (i.IsEnabled && !i.IsLoaded) i.IsRefreshRequested = true;
            }
        }
        OnPropertyChanged(nameof(FilterList));
    }


    private bool _RefreshAll = false;
    public bool RefreshAll
    {
        get => _RefreshAll; set
        {
            SetProperty(ref _RefreshAll, value);
            CheckRefreshCommandCanExecuteChange();
        }
    }

    public DelegateCommand RefreshCommand { get; }
    public void CheckRefreshCommandCanExecuteChange()
    {
        RefreshCommand?.OnCanExecuteChanged();
    }


    private bool _SetupRequired;
    public bool SetupRequired { get => _SetupRequired; set => SetProperty(ref _SetupRequired, value); }


    private AdBlockerSettingFilterViewModel _ItemToAdd;
    public AdBlockerSettingFilterViewModel ItemToAdd { get => _ItemToAdd; set => SetProperty(ref _ItemToAdd, value); }


}

public class AdBlockerSettingFilterGroupViewModel : ObservableCollection<AdBlockerSettingFilterViewModel>
{
    private itemsGroup content { get; }

    public string Title
    {
        get => content.GetTitleForCulture()?.Value ?? string.Empty;
        set
        {
            static title getTitle(string value)
            {
                return new title()
                {
                    @default = true,
                    language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    Value = value,
                };
            }

            var lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var first = content.title?.FirstOrDefault(a => string.Equals(lang, a.language, StringComparison.InvariantCultureIgnoreCase));
            if (first is null)
            {
                if (content.title is null)
                {
                    content.title = new[] { getTitle(value) };
                }
                else
                {
                    foreach (var item in content.title) item.@default = false;
                    var temp = content.title;
                    Array.Resize(ref temp, temp.Length + 1);
                    temp[temp.Length - 1] = getTitle(value);
                    content.title = temp;
                }
            }
            else
            {
                first.Value = value;
            }
        }
    }

    public AdBlockerSettingFilterGroupViewModel(itemsGroup content) : base(content.item.Select(a => new AdBlockerSettingFilterViewModel(a)))
    {
        foreach (var item in this) item.Parent = this;
        this.content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public itemsGroup Content
    {
        get
        {
            content.item = this.Select(a => a.GetContent()).ToArray();
            return content;
        }
    }


    private AdBlockerSettingViewModel? _Parent;
    public AdBlockerSettingViewModel? Parent
    {
        get => _Parent; set
        {
            _Parent = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Parent)));
        }
    }

}

public class AdBlockerSettingFilterViewModel : BaseViewModel
{
    private item content;

    public AdBlockerSettingFilterViewModel(item content)
    {
        this.content = content ?? throw new ArgumentNullException(nameof(content));
        ReloadSingleCommand = new Helper.DelegateCommand(async _ =>
        {
            await Managers.ExtensionAdBlockerManager.TryDownloadList(content);
            await Managers.ExtensionAdBlockerManager.LoadRulesFromText();
        });
    }

    public item GetContent() => content;

    public ICommand ReloadSingleCommand { get; }

    private bool _IsEnabled;
    public bool IsEnabled
    {
        get => _IsEnabled; set
        {
            SetProperty(ref _IsEnabled, value);
            if (value && !IsLoaded) IsRefreshRequested = true;
            Parent?.Parent?.CheckRefreshCommandCanExecuteChange();
        }
    }

    private bool _IsRefreshRequested;
    public bool IsRefreshRequested
    {
        get => _IsRefreshRequested; set
        {
            SetProperty(ref _IsRefreshRequested, value);
            Parent?.Parent?.CheckRefreshCommandCanExecuteChange();
        }
    }


    private bool _IsLoaded;
    public bool IsLoaded { get => _IsLoaded; set => SetProperty(ref _IsLoaded, value); }


    private AdBlockerSettingFilterGroupViewModel? _Parent;
    public AdBlockerSettingFilterGroupViewModel? Parent { get => _Parent; set => SetProperty(ref _Parent, value); }


    public string Title
    {
        get => content.GetTitleForCulture();
        set
        {
            content.title = new title[0];
            content.title1 = value;
            OnPropertyChanged();
        }
    }

    public string FileName
    {
        get => content.filename;
        set
        {
            content.filename = value;
            OnPropertyChanged();
        }
    }


    private string _FileNameCandidateBody = string.Empty;
    public string NewFileNameBody { get => _FileNameCandidateBody; set => SetProperty(ref _FileNameCandidateBody, value); }


    public bool Recommended
    {
        get => content.recommended;
        set
        {
            Recommended = value;
            OnPropertyChanged();
        }
    }


    private string _Message = string.Empty;
    public string Message { get => _Message; set => SetProperty(ref _Message, value); }


    private DownloadSuccessStatus _DownloadStatus;
    public DownloadSuccessStatus DownloadStatus { get => _DownloadStatus; set => SetProperty(ref _DownloadStatus, value); }


    public enum DownloadSuccessStatus
    {
        NoMessage, Success, Fail
    }


    public string LicenseSource
    {
        get => content.license_source;
        set
        {
            content.license_source = value;
            OnPropertyChanged();
        }
    }

    public string LicenseSummary
    {
        get => content.license_summary;
        set
        {
            content.license_summary = value;
            OnPropertyChanged();
        }
    }

    public string Source
    {
        get => content.source;
        set
        {
            if (content.source is null || string.IsNullOrWhiteSpace(NewFileNameBody) || System.IO.Path.GetFileNameWithoutExtension(content.source) == NewFileNameBody)
                NewFileNameBody = System.IO.Path.GetFileNameWithoutExtension(value);
            content.source = value;
            OnPropertyChanged();
        }
    }

    public string ProjectSource
    {
        get => content.project_source;
        set
        {
            content.project_source = value;
            OnPropertyChanged();
        }
    }

    private bool _CanDelete;
    public bool CanDelete { get => _CanDelete; set => SetProperty(ref _CanDelete, value); }

}
