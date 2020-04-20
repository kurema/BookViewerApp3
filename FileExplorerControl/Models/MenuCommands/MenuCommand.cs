using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;

namespace kurema.FileExplorerControl.Models
{
    public class MenuCommand
    {
        public MenuCommand(string title, ICommand command)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public MenuCommand(string title, params MenuCommand[] items)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Items = new ObservableCollection<MenuCommand>(items);
        }

        public string Title { get; set; }
        public System.Windows.Input.ICommand Command { get; set; } = new Helper.DelegateCommand(a => { }, a => false);
        public ObservableCollection<MenuCommand> Items { get; } = new ObservableCollection<MenuCommand>();

        public bool HasChild => Items != null && Items.Count > 0;
    }

    //Git履歴に残すためにコメントアウトしています。
    //コミット後削除してください。
    //public class MenuCommand : INotifyPropertyChanged
    //{

    //    #region INotifyPropertyChanged
    //    protected bool SetProperty<T>(ref T backingStore, T value,
    //        [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
    //        System.Action onChanged = null)
    //    {
    //        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
    //            return false;

    //        backingStore = value;
    //        onChanged?.Invoke();
    //        OnPropertyChanged(propertyName);
    //        return true;
    //    }
    //    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    //    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    //    {
    //        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    //    }
    //    #endregion

    //    public void OnMenuCommandsUpdate() => OnPropertyChanged(nameof(MenuCommands));

    //    private ObservableCollection<MenuCommandItem> _MenuCommands;
    //    public ObservableCollection<MenuCommandItem> MenuCommands { get => _MenuCommands; set => SetProperty(ref _MenuCommands, value); }

    //}
}
