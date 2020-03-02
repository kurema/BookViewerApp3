using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.UI.Xaml.Media;
using kurema.FileExplorerControl.Models.IconProviders;
using kurema.FileExplorerControl.Models.FileItems;

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

        private IFileItem _Content;
        public IFileItem Content { get => _Content; set
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
                DeleteCommand?.OnCanExecuteChanged();
                //RenameCommand?.OnCanExecuteChanged();
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

        //public Helper.DelegateCommand RenameCommand { get; set; }

        private Helper.DelegateCommand _DeleteCommand;
        public Helper.DelegateCommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(async (parameter) =>
        {
            async Task<(bool,bool)> checkDelete()
            {
                if (ParentContent?.DialogDelete != null)
                {
                    var result = await ParentContent?.DialogDelete(this);
                    if (result.delete == false) return (false, false);
                    return (result.delete, result.completeDelete);
                }
                return (true, false);
            }

            if (Content?.DeleteCommand is Helper.DelegateAsyncCommand dc)
            {
                if (dc.CanExecute(parameter))
                {
                    var checkResult = await checkDelete();
                    if (checkResult.Item1)
                    {
                        await dc.ExecuteAsync(checkResult.Item2);
                        await Parent?.UpdateChildren();
                    }
                }
            }
            else
            {
                if (Content?.DeleteCommand?.CanExecute(parameter) == true)
                {
                    var checkResult = await checkDelete();
                    if (checkResult.Item1)
                    {
                        Content.DeleteCommand.Execute(checkResult.Item2);
                    }
                }
            }
        }, a => Content?.DeleteCommand?.CanExecute(a) ?? false);


        private ContentViewModel _ParentContent;
        public ContentViewModel ParentContent { get => _ParentContent; set
            {
                SetProperty(ref _ParentContent, value);

                if (Children != null)
                    foreach (var item in this.Children)
                    {
                        item.ParentContent = this.ParentContent;
                    }
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

        private ObservableCollection<IIconProvider> _IconProviders = new ObservableCollection<IIconProvider>(new[] { new IconProviderDefault() });

        public ObservableCollection<IIconProvider> IconProviders { get => _IconProviders; set
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
                item.ParentContent = this.ParentContent;
            }
            Children = children;
        }

        private FileItemViewModel _Parent;
        public FileItemViewModel Parent { get => _Parent; set => SetProperty(ref _Parent, value); }


        public string Title
        {
            get => _Content?.Name ?? "";
            set
            {
                Rename(value?.ToString());
            }
        }

        private async void Rename(string title)
        {
            if (title == Title) return;
            if (String.IsNullOrEmpty(title)) return;
            if (Content?.RenameCommand?.CanExecute(title) == true)
            {
                if (Content.RenameCommand is Helper.DelegateAsyncCommand renac)
                {
                    try
                    {
                        await renac.ExecuteAsync(title);
                    }
                    catch { return; }
                }
                else
                {
                    try
                    {
                        Content.RenameCommand.Execute(title);
                    }
                    catch { return; }
                }
            }
            OnPropertyChanged(nameof(Title));
        }

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
                    UpdateSize();
                    return null;
                }
            }
        }
        private async void UpdateSize()
        {
            if (_Content == null) return;
            try
            {
                _Size = await _Content.GetSizeAsync();
                OnPropertyChanged(nameof(Size));
            }
            catch { }
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
