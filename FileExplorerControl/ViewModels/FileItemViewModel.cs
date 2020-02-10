using kurema.FileExplorerControl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;


namespace kurema.FileExplorerControl.ViewModels
{
    public class FileItemViewModel:INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
            System.Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion


        private Models.IFileItem _Content;
        public Models.IFileItem Content { get => _Content; set
            {
                SetProperty(ref _Content, value);
                _Children = null;
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Folders));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(LastModified));
                OnPropertyChanged(nameof(IsFolder));
            }
        }

        public FileItemViewModel(IFileItem content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public FileItemViewModel[] Folders
             => Children?.Where(a => a.IsFolder).ToArray()?? new FileItemViewModel[0];
        public FileItemViewModel[] Files => Children?.Where(a => !a.IsFolder).ToArray() ?? new FileItemViewModel[0];


        private ObservableCollection<FileItemViewModel> _Children;

        public ObservableCollection<FileItemViewModel> Children
        {
            get {
                return _Children;
                //if (_Children == null)
                //{
                //    Task.Run(async () => {
                //        await UpdateChildren();
                //    });
                //    return null;
                //}
                //else
                //{
                //    return _Children;
                //}
            }
            private set
            {
                _Children = value;
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Folders));
            }
        }

        public async Task UpdateChildren()
        {
            Children = new ObservableCollection<FileItemViewModel>((await Content?.GetChildren())?.Select(f => new FileItemViewModel(f)));
        }

        public string Title => _Content?.FileName ?? "";

        public DateTimeOffset LastModified => _Content?.DateCreated??new DateTimeOffset();

        public bool IsFolder => _Content?.IsFolder ?? false;
    }
}
