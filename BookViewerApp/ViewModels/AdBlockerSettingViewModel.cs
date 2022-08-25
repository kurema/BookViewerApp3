using BookViewerApp.Storages.ExtensionAdBlockerItems;
using kurema.FileExplorerControl.ViewModels;
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
public class AdBlockerSettingViewModel : Helper.ViewModelBase
{
    public AdBlockerSettingViewModel()
    {
    }

    public ObservableCollection<AdBlockerSettingFilterGroupViewModel> FilterList { get; private set; } = new ObservableCollection<AdBlockerSettingFilterGroupViewModel>();

    public async Task LoadFilterList()
    {
        var filter = await Managers.ExtensionAdBlockerManager.LocalLists.GetContentAsync();
        //ToDo: Update IsEnabled
        FilterList = new ObservableCollection<AdBlockerSettingFilterGroupViewModel>(filter?.group?.Select(a => new AdBlockerSettingFilterGroupViewModel(a)) ?? new AdBlockerSettingFilterGroupViewModel[0]);
        OnPropertyChanged(nameof(FilterList));
     }
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
}

public class AdBlockerSettingFilterViewModel : BaseViewModel
{
    private itemsGroupItem content;

    public AdBlockerSettingFilterViewModel(itemsGroupItem content)
    {
        this.content = content ?? throw new ArgumentNullException(nameof(content));
        ReloadCommang = new Helper.DelegateCommand(async _ =>
        {
            await Managers.ExtensionAdBlockerManager.TryDownloadList(content);
            await Managers.ExtensionAdBlockerManager.LoadRulesFromText();
        });
    }

    public itemsGroupItem GetContent() => content;

    public ICommand ReloadCommang { get; }

    private bool _IsEnabled;
    public bool IsEnabled { get => _IsEnabled; set => SetProperty(ref _IsEnabled, value); }


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
