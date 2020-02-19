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
                OnPropertyChanged(nameof(Path));
                OnPropertyChanged(nameof(LastModified));
                OnPropertyChanged(nameof(IsFolder));
                OnPropertyChanged(nameof(Size));
            }
        }

        public FileItemViewModel(IFileItem content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public FileItemViewModel[] Folders
             => Children?.Where(a => a.IsFolder).ToArray()?? new FileItemViewModel[0];
        public FileItemViewModel[] Files => Children?.Where(a => !a.IsFolder).ToArray() ?? new FileItemViewModel[0];

        private OrderStatus _Order = new OrderStatus();
        public OrderStatus Order { get => _Order; set
            {
                SetProperty(ref _Order, value);
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Folders));
            }
        }

        public class OrderStatus:INotifyPropertyChanged
        {
            #region INotifyPropertyChanged
            protected bool SetProperty<T>(ref T backingStore, T value,
                [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
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
            protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
            #endregion


            private string _Key;
            public string Key { get => _Key; set => SetProperty(ref _Key, value); }


            private bool _KeyIsAscending;
            public bool KeyIsAscending { get => _KeyIsAscending; set => SetProperty(ref _KeyIsAscending, value); }


            private Func<IEnumerable<FileItemViewModel>, IEnumerable<FileItemViewModel>> _OrderDelegate;
            public Func<IEnumerable<FileItemViewModel>, IEnumerable<FileItemViewModel>> OrderDelegate { get => _OrderDelegate; set
                {
                    SetProperty(ref _OrderDelegate, value);
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }

            public bool IsEmpty => _OrderDelegate == null;

            public OrderStatus GetBasicOrder(string key, bool isAscending)
            {
                var resultOrder = new OrderStatus();

                void SetSortState<T>(Func<FileItemViewModel, T> func, bool isAscendingArg)
                {
                    if (isAscendingArg) resultOrder.OrderDelegate = a => a.OrderBy(func);
                    else resultOrder.OrderDelegate = a => a.OrderByDescending(func);
                }

                switch (key)
                {
                    case "Title":
                        SetSortState(b => b.Title, isAscending);
                        break;
                    case "Size":
                        SetSortState(b => b.Size ?? 0, isAscending);
                        break;
                    case "Date":
                        SetSortState(b => b.LastModified.Ticks, isAscending);
                        break;
                    default:
                        return new OrderStatus();
                }
                resultOrder.Key = key;
                resultOrder.KeyIsAscending = isAscending;
                return resultOrder;
            }

            public OrderStatus GetShiftedBasicOrder(string key)
            {
                if (Key == key)
                {
                    if (KeyIsAscending)
                    {
                        return GetBasicOrder(key, false);
                    }
                    else
                    {
                        return new OrderStatus();
                    }
                }
                else
                {
                    return GetBasicOrder(key, true);
                }
            }
        }



        private IEnumerable<FileItemViewModel> _Children;

        public IEnumerable<FileItemViewModel> Children
        {
            get {
                return Order?.OrderDelegate == null ? _Children : Order?.OrderDelegate(_Children);
            }
            private set
            {
                _Children = value;
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Folders));
            }
        }

        private ObservableCollection<Models.IIconProvider> _IconProviders = new ObservableCollection<IIconProvider>(new[] { new Models.IconProviderDefault() });

        public ObservableCollection<Models.IIconProvider> IconProviders { get => _IconProviders; set
            {
                void IconUpdate(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                {
                    OnPropertyChanged(nameof(IconSmall));
                    OnPropertyChanged(nameof(IconLarge));
                }

                if (_IconProviders!=null) _IconProviders.CollectionChanged -= IconUpdate;
                SetProperty(ref _IconProviders, value);
                IconUpdate(this, null);
                if (_IconProviders != null) _IconProviders.CollectionChanged += IconUpdate;
            }
        }


        public async Task UpdateChildren()
        {
            var children = new List<FileItemViewModel>((await Content?.GetChildren())?.Select(f => new FileItemViewModel(f)));
            foreach (var item in children)
            {
                //(item.IconSmall, item.IconLarge) = IconProviderDefault.GetIcon(item.Content, IconProviders);
                item.IconProviders = this.IconProviders;

                item.Parent = this;
            }
            Children = children;
        }

        private FileItemViewModel _Parent;
        public FileItemViewModel Parent { get => _Parent; set => SetProperty(ref _Parent, value); }


        public string Title => _Content?.FileName ?? "";

        public string Path => _Content?.Path ?? "";

        public DateTimeOffset LastModified => _Content?.DateCreated??new DateTimeOffset();

        public bool IsFolder => _Content?.IsFolder ?? false;


        private ulong? _Size;
        public ulong? Size
        {
            get
            {
                if (_Size != null) return _Size;
                if (_Content == null) return null;
                if (this.IsFolder)
                {
                    if (Children == null) return null;
                    ulong result = 0;
                    foreach(var item in Children)
                    {
                        if (item.Size == null) return null;
                        else result += item.Size ?? 0;
                    }
                    return result;
                }
                else
                {
                    //Task.Run(async () =>
                    //{
                    //    _Size = await _Content.GetSizeAsync();
                    //    OnPropertyChanged(nameof(Size));
                    //});
                    UpdateSize();
                    return null;
                }
            }
        }
        private async void UpdateSize()
        {
            _Size = await _Content.GetSizeAsync();
            OnPropertyChanged(nameof(Size));
        }

        private ImageSource _IconSmall;
        public ImageSource IconSmall { get {
                if (_IconSmall == null) (_IconSmall, _IconLarge) = IconProviderDefault.GetIcon(this.Content, IconProviders);
                return _IconSmall;
            } 
            set
            {
                SetProperty(ref _IconSmall, value);
            }
        }


        private ImageSource _IconLarge;
        public ImageSource IconLarge
        {
            get
            {
                if (_IconLarge == null) (_IconSmall, _IconLarge) = IconProviderDefault.GetIcon(this.Content, IconProviders);
                return _IconLarge;
            }
            set
            {
                SetProperty(ref _IconLarge, value);
            }
        }
    }
}
