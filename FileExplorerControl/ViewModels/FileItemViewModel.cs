using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.UI.Xaml.Media;
using kurema.FileExplorerControl.Models.IconProviders;
using kurema.FileExplorerControl.Models.FileItems;

namespace kurema.FileExplorerControl.ViewModels
{
    public partial class FileItemViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
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

        private CancellationTokenSource _IconCancellationTokenSource = new CancellationTokenSource();
        public CancellationTokenSource IconCancellationTokenSource { get => _IconCancellationTokenSource; set { SetProperty(ref _IconCancellationTokenSource, value); } }

        private IFileItem _Content;
        public IFileItem Content
        {
            get => _Content; set
            {
                if (_Content != null)
                {
                    _Content.Updated -= _Content_Updated;
                }

                SetProperty(ref _Content, value);
                _Children = null;
                Content_Updated();
                if (_Content != null) _Content.Updated += _Content_Updated;

                DeleteCommand?.OnCanExecuteChanged();
            }
        }

        private void _Content_Updated(object sender, EventArgs e)
        {
            Content_Updated();

            if (!(IconLarge is null && IconSmall is null)) UpdateIcon();
        }

        private void Content_Updated()
        {
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(Folders));
            OnPropertyChanged(nameof(Files));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(FileTypeDescription));
            OnPropertyChanged(nameof(LastModified));
            OnPropertyChanged(nameof(IsFolder));
            OnPropertyChanged(nameof(Size));
            OnPropertyChanged(nameof(MenuCommands));
        }

        public FileItemViewModel(IFileItem content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public FileItemViewModel[] Folders
             => Children?.Where(a => a.IsFolder).ToArray() ?? new FileItemViewModel[0];
        public FileItemViewModel[] Files => Children?.Where(a => !a.IsFolder).ToArray() ?? new FileItemViewModel[0];

        private OrderStatus _Order = new OrderStatus();
        public OrderStatus Order
        {
            get => _Order; set
            {
                SetProperty(ref _Order, value);
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Folders));
            }
        }

        public Models.MenuCommand[] MenuCommands => Content?.MenuCommandsProvider?.Invoke(Content) ?? new Models.MenuCommand[0];

        private Helper.DelegateCommand _DeleteCommand;
        public Helper.DelegateCommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(async (parameter) =>
        {
            async Task<(bool, bool)> checkDelete(bool canDeleteComplete)
            {
                if (ParentContent?.DialogDelete != null)
                {
                    var result = await ParentContent?.DialogDelete(this, canDeleteComplete);
                    if (result.delete == false) return (false, false);
                    return (result.delete, result.completeDelete);
                }
                return (true, false);
            }

            if (Content?.DeleteCommand is Helper.DelegateAsyncCommand dc)
            {
                if (dc.CanExecute(false))
                {
                    var checkResult = await checkDelete(dc.CanExecute(true));
                    if (checkResult.Item1)
                    {
                        await dc.ExecuteAsync(checkResult.Item2);
                        await Parent?.UpdateChildren();
                    }
                }
            }
            else
            {
                if (Content?.DeleteCommand?.CanExecute(false) == true)
                {
                    var checkResult = await checkDelete(Content.DeleteCommand.CanExecute(true));
                    if (checkResult.Item1)
                    {
                        Content.DeleteCommand.Execute(checkResult.Item2);
                    }
                }
            }
        }, a => Content?.DeleteCommand?.CanExecute(a) ?? false);


        private ContentViewModel _ParentContent;
        public ContentViewModel ParentContent
        {
            get => _ParentContent; set
            {
                SetProperty(ref _ParentContent, value);

                if (Children != null)
                    foreach (var item in this.Children)
                    {
                        item.ParentContent = this.ParentContent;
                    }
            }
        }

        private IEnumerable<FileItemViewModel> _Children = null;

        public IEnumerable<FileItemViewModel> Children
        {
            get
            {
                return Order?.OrderDelegate is null || _Children is null ? _Children : Order?.OrderDelegate(_Children);
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

        public ObservableCollection<IIconProvider> IconProviders
        {
            get => _IconProviders; set
            {
                void IconUpdate(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                {
                    OnPropertyChanged(nameof(IconSmall));
                    OnPropertyChanged(nameof(IconLarge));
                }

                if (_IconProviders != null) _IconProviders.CollectionChanged -= IconUpdate;
                SetProperty(ref _IconProviders, value);
                IconUpdate(this, null);
                if (_IconProviders != null) _IconProviders.CollectionChanged += IconUpdate;
            }
        }

        public void IconFetchingCancel()
        {
            foreach (var item in Children)
            {
                if (!item.IsFolder) item.IconCancellationTokenSource.Cancel();
            }
        }

        public void IconFetchingUnCancel()
        {
            foreach (var item in Children)
            {
                if (!item.IsFolder) item.IconCancellationTokenSource = new CancellationTokenSource();
            }
        }

        public async Task UpdateChildren()
        {
            void SetChildren(ObservableCollection<IFileItem> children_param)
            {
                var children = new List<FileItemViewModel>(children_param.Select(f => new FileItemViewModel(f)));
                foreach (var item in children)
                {
                    item.IconProviders = this.IconProviders;

                    item.Parent = this;
                    item.ParentContent = this.ParentContent;
                }
                Children = children;

            }

            var children_observable = await Content?.GetChildren();

            if (children_observable is null)
            {
                Children = new List<FileItemViewModel>();
                return;
            }


            void Children_observable_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                SetChildren(children_observable);

                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Folders));
            }

            SetChildren(children_observable);

            //古い方のイベントから外しときたいけど、忘れるので無理。
            children_observable.CollectionChanged += Children_observable_CollectionChanged;
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

        public string FileTypeDescription => _Content?.FileTypeDescription ?? "";

        public DateTimeOffset LastModified => _Content?.DateCreated ?? new DateTimeOffset();

        public bool IsFolder => _Content?.IsFolder ?? false;


        private ulong? _Size;
        public ulong? Size
        {
            get
            {
                if (_Size != null) return _Size;
                if (_Content is null) return null;
                if (this.IsFolder)
                {
                    if (Children is null) return null;
                    ulong result = 0;
                    foreach (var item in Children)
                    {
                        if (item.Size is null) return null;
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
            if (_Content is null) return;
            try
            {
                _Size = await _Content.GetSizeAsync();
                OnPropertyChanged(nameof(Size));
            }
            catch { }
        }

        private ImageSource _IconSmall;
        public ImageSource IconSmall
        {
            get
            {
                if (_IconSmall is null) UpdateIcon();
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
                if (_IconLarge is null) UpdateIcon();
                return _IconLarge;
            }
            set
            {
                SetProperty(ref _IconLarge, value);
            }
        }

        public async void UpdateIcon()
        {
            try
            {
                (IconSmall, IconLarge) = await IconProviderDefault.GetIcon(this.Content, IconProviders, IconCancellationTokenSource.Token);
            }
            catch { }
        }
    }
}
