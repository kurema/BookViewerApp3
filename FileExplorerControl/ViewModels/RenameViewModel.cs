using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;
using System.Collections;

namespace kurema.FileExplorerControl.ViewModels;

public class RenameViewModel : BaseViewModel
{
    public ObservableCollection<RenameItemViewModel> Files { get; } = new();

    public async Task LoadFolder(Models.FileItems.IFileItem fileItem)
    {
        Files.Clear();

        if (fileItem.IsFolder is false) return;
        var files = await fileItem.GetChildren();
        foreach (var item in files)
        {
            if (item.RenameCommand?.CanExecute("sample.txt") is true) Files.Add(new RenameItemViewModel(item));
        }
        {
            _SelectAllToggle = true;
            OnPropertyChanged(nameof(SelectAllToggle));
        }
    }


    private System.Windows.Input.ICommand _SelectAllCommand;
    public System.Windows.Input.ICommand SelectAllCommand => _SelectAllCommand ??= new Helper.DelegateCommand((parameter) =>
    {
        bool p = true;
        if (parameter is bool param1) p = param1;
        else if (bool.TryParse(parameter?.ToString() ?? "", out bool param2)) p = param2;
        SelectAll(p);
    });

    public void SelectAll(bool p)
    {
        foreach (var item in Files) item.IsEnabled = p;
    }


    private bool _SelectAllToggle = true;
    public bool SelectAllToggle
    {
        get => _SelectAllToggle;
        set
        {
            SetProperty(ref _SelectAllToggle, value, onChanged: () =>
            {
                SelectAll(value);
            });
        }
    }



    private RenameRegexViewModel _ContentRegex = new();
    public RenameRegexViewModel ContentRegex { get => _ContentRegex; set => SetProperty(ref _ContentRegex, value); }


    public RenameViewModel()
    {
        Files.Add(new RenameItemViewModel(new Models.FileItems.FileItemPlaceHolder()));
    }
}

public class RenameRegexViewModel : BaseViewModel
{
    private bool _IsRegex;
    public bool IsRegex { get => _IsRegex; set => SetProperty(ref _IsRegex, value); }


    private bool _IsCaseSensitive;
    public bool IsCaseSensitive { get => _IsCaseSensitive; set => SetProperty(ref _IsCaseSensitive, value); }


    private bool _MatchAllOccurence;
    public bool MatchAllOccurence { get => _MatchAllOccurence; set => SetProperty(ref _MatchAllOccurence, value); }


    private string _NameOriginal = "";
    public string NameOriginal { get => _NameOriginal; set => SetProperty(ref _NameOriginal, value); }

    private string _NameRenamed = "";
    public string NameRenamed { get => _NameRenamed; set => SetProperty(ref _NameRenamed, value); }

    private CaseFormatType _CaseFormat;
    public CaseFormatType CaseFormat { get => _CaseFormat; set => SetProperty(ref _CaseFormat, value); }


    public enum CaseFormatType
    {
        Normal, Lowercase, Uppercase, TitleCase, CapitalizeEachWord
    }
}

public class RenameItemViewModel : BaseViewModel
{
    private FileItemViewModel _File;
    public FileItemViewModel File { get => _File; set => SetProperty(ref _File, value); }

    private string _NameRenamed = "";
    public string NameRenamed { get => _NameRenamed; set => SetProperty(ref _NameRenamed, value); }

    private bool _HasErrors = false;
    public bool HasErrors { get => _HasErrors; set => SetProperty(ref _HasErrors, value); }

    private string _ErrorMessage = "";

    public RenameItemViewModel(Models.FileItems.IFileItem file)
    {
        File = new FileItemViewModel(file);
    }


    private bool _IsEnabled = true;
    public bool IsEnabled { get => _IsEnabled; set => SetProperty(ref _IsEnabled, value); }


    public string ErrorMessage { get => _ErrorMessage; set => SetProperty(ref _ErrorMessage, value); }
}
