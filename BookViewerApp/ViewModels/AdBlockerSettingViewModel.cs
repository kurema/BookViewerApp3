using BookViewerApp.Helper;
using BookViewerApp.Storages.ExtensionAdBlockerItems;
using kurema.FileExplorerControl.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

        {
            AddItemCommand = new DelegateCommand(async _ =>
            {
                await SemaphoreAddItem.WaitAsync();
                try
                {
                    if (CustomFilters is null) return;
                    ItemToAdd.PropertyChanged -= ItemToAdd_PropertyChanged;
                    var item = ItemToAdd;
                    item.UpdateFileNameFromNewFileNameBody();
                    var content = item.GetContent();
                    if (!content.IsValidEntry) return;
                    if (CustomFilters.Select(a => a.GetContent())
                    .Any(a => (a.filename ?? string.Empty).Equals(content.filename, StringComparison.InvariantCultureIgnoreCase) || (a.source ?? string.Empty).Equals(content.source, StringComparison.InvariantCultureIgnoreCase))) return;
                    if (!await Managers.ExtensionAdBlockerManager.TryDownloadList(content)) return;
                    // We should check if file is valid.
                    item.Parent = CustomFilters;
                    CustomFilters.Add(item);
                    ItemToAdd = new AdBlockerSettingFilterViewModel(new item());
                    ItemToAdd.PropertyChanged += ItemToAdd_PropertyChanged;
                    await SaveCustomFilters();
                    AddItemCommand?.OnCanExecuteChanged();
                }
                finally
                {
                    SemaphoreAddItem.Release();
                }
            }, _ => !string.IsNullOrEmpty(ItemToAdd.NewFileNameBody) && !string.IsNullOrEmpty(ItemToAdd.Source) && !string.IsNullOrWhiteSpace(ItemToAdd.Title));
            ItemToAdd.PropertyChanged += ItemToAdd_PropertyChanged;

            void ItemToAdd_PropertyChanged(object sender, PropertyChangedEventArgs e) => AddItemCommand?.OnCanExecuteChanged();
        }
    }

    private static SemaphoreSlim SemaphoreAddItem = new(1, 1);

    public async Task SaveCustomFilters()
    {
        if (CustomFilters is null) return;
        var info = await Managers.ExtensionAdBlockerManager.LocalInfo.GetContentAsync();
        if (info is null) return;
        info.filters = CustomFilters.Content.item;
        await Managers.ExtensionAdBlockerManager.LocalInfo.SaveAsync();
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


    private AdBlockerSettingFilterGroupViewModel? _CustomFilters;
    public AdBlockerSettingFilterGroupViewModel? CustomFilters { get => _CustomFilters; set => SetProperty(ref _CustomFilters, value); }


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
            FilterList.Add(CustomFilters = new AdBlockerSettingFilterGroupViewModel(gr) { Parent = this, CanDelete = true });
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

    public DelegateCommand AddItemCommand { get; }


    private bool _SetupRequired;
    public bool SetupRequired { get => _SetupRequired; set => SetProperty(ref _SetupRequired, value); }


    private AdBlockerSettingFilterViewModel _ItemToAdd = new AdBlockerSettingFilterViewModel(new item());
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

    private bool _CanDelete;
    public bool CanDelete
    {
        get => _CanDelete; set
        {
            _CanDelete = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanDelete)));
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


    private ICommand? _DeleteCommand;
    public ICommand DeleteCommand => _DeleteCommand ??= new DelegateCommand(_ => { Parent?.Remove(this); }, _ => Parent is not null);


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


    private string _NewFileNameBody = string.Empty;
    public string NewFileNameBody { get => _NewFileNameBody; set => SetProperty(ref _NewFileNameBody, value); }

    public void UpdateFileNameFromNewFileNameBody()
    {
        FileName = $"{Managers.ExtensionAdBlockerManager.CustomFilterFileNameHeader}{NewFileNameBody}.txt";
    }


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
            if (content.source is null || string.IsNullOrWhiteSpace(NewFileNameBody) || GetNewFileNameBodyFromSource(content.source) == NewFileNameBody)
                NewFileNameBody = GetNewFileNameBodyFromSource(value);
            content.source = value;
            OnPropertyChanged();
        }
    }

    public static string GetNewFileNameBodyFromSource(string source)
    {
        string result = System.IO.Path.GetFileNameWithoutExtension(source);
        foreach (var item in System.IO.Path.GetInvalidFileNameChars()) result = result.Replace(item, '_');
        int len = 32 - Managers.ExtensionAdBlockerManager.CustomFilterFileNameHeader.Length - 4;
        if (result.Length > len) result = result.Substring(0, len);
        return result;
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
}
