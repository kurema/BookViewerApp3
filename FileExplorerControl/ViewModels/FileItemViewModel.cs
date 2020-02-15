using kurema.FileExplorerControl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.UI.Xaml.Media;


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
            var children = new ObservableCollection<FileItemViewModel>((await Content?.GetChildren())?.Select(f => new FileItemViewModel(f)));
            foreach (var item in children)
            {
                if (item.IsFolder) {
                    item.IconSmall = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_folder_s.png"));
                    item.IconLarge = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_folder_l.png"));
                }
                else
                {
                    item.IconSmall = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_book_s.png"));
                    item.IconLarge = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///FileExplorerControl/res/icon_book_l.png"));
                }
                item.Parent = this;
            }
            Children = children;
        }



        private FileItemViewModel _Parent;
        public FileItemViewModel Parent { get => _Parent; set => SetProperty(ref _Parent, value); }


        public string Title => _Content?.FileName ?? "";

        public DateTimeOffset LastModified => _Content?.DateCreated??new DateTimeOffset();

        public bool IsFolder => _Content?.IsFolder ?? false;


        private ImageSource _IconSmall;
        public ImageSource IconSmall { get => _IconSmall; set => SetProperty(ref _IconSmall, value); }


        private ImageSource _IconLarge;
        public ImageSource IconLarge { get => _IconLarge; set => SetProperty(ref _IconLarge, value); }
    }
}
