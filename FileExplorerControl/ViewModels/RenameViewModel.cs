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

public class RenameViewModel : INotifyPropertyChanged
{
    #region INotifyPropertyChanged
    protected bool SetProperty<T>(ref T backingStore, T value,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
        System.Action onChanged = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
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

    public ObservableCollection<FileItemViewModel> Files { get; } = new();

    public async Task LoadFolder(Models.FileItems.IFileItem fileItem)
    {
        Files.Clear();

        if (fileItem.IsFolder is false) return;
        var files = await fileItem.GetChildren();
        foreach (var item in files)
        {
            if (item.RenameCommand?.CanExecute("sample.txt") is true) Files.Add(new FileItemViewModel(item));
        }
    }
}

public class RenameItemViewModel : INotifyPropertyChanged,INotifyDataErrorInfo
{
    public bool HasErrors => throw new NotImplementedException();

    public IEnumerable GetErrors(string propertyName)
    {
        throw new NotImplementedException();
    }


    private FileItemViewModel _File;
    public FileItemViewModel File { get => _File; set => SetProperty(ref _File, value); }



    #region INotifyPropertyChanged
    protected bool SetProperty<T>(ref T backingStore, T value,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
        System.Action onChanged = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
    #endregion

}
