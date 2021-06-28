using BookViewerApp.Storages.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BookViewerApp.ViewModels
{
    public class LibraryMemberViewModel : INotifyPropertyChanged
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
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private libraryLibrary _Content;

        public LibraryMemberViewModel()
        {
            Items = new ObservableCollection<LibraryMemberItemViewModel>();
        }

        /// <summary>
        /// Load libraryLibrary and update Title asynchronously. Call this from UI thread.
        /// </summary>
        /// <param name="content">Content to load</param>
        /// <returns>Task</returns>
        public async Task LoadContent(libraryLibrary content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));

            Items = new ObservableCollection<LibraryMemberItemViewModel>((Content?.Items ?? new object[0]).Where(a => a is IlibraryLibraryItem).Select(a => new LibraryMemberItemViewModel(a as IlibraryLibraryItem, this)));
            foreach(var item in Items)
            {
                await item.UpdateStorageItem();
            }
            Items.CollectionChanged += Result_CollectionChanged;
        }

        public libraryLibrary Content
        {
            get => _Content;
            set
            {
                if (!(_Content is null)) value.PropertyChanged -= Value_PropertyChanged;

                SetProperty(ref _Content, value);
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Items));

                if (!(value is null)) value.PropertyChanged += Value_PropertyChanged;
            }
        }

        private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Content.Items))
            {
                OnPropertyChanged(nameof(Items));
            }
            Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
        }

        public string Title
        {
            get => Content?.title ?? "";
            set
            {
                if (Content is null) return;
                Content.title = value;
                OnPropertyChanged(nameof(Title));
            }
        }


        public ObservableCollection<LibraryMemberItemViewModel> Items { get; set; }

        private void Result_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Content is null) return;
            Content.Items = (sender as ObservableCollection<LibraryMemberItemViewModel>)?.Select(a => a.Content)?.ToArray() ?? Content.Items;
        }

        public async Task UpdateStorages()
        {
            foreach (var item in this.Items)
            {
                await item.UpdateStorageItem();
            }
        }
    }

    public class LibraryMemberItemViewModel : INotifyPropertyChanged
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

        private LibraryMemberViewModel _Parent;
        public LibraryMemberViewModel Parent { get => _Parent; private set => SetProperty(ref _Parent, value); }

        private IlibraryLibraryItem _Content;

        public LibraryMemberItemViewModel(IlibraryLibraryItem member, LibraryMemberViewModel parent)
        {
            _Content = member ?? throw new ArgumentNullException(nameof(member));
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            var _ = UpdateStorageItem();
        }

        public IlibraryLibraryItem Content
        {
            get => _Content;
        }

        public async Task UpdateStorageItem()
        {
            if (Content is libraryLibraryFolder f)
            {
                StorageItem = await f.GetStorageFolderAsync().ConfigureAwait(false);
            }
            else StorageItem = null;
        }

        public string Path => StorageItem?.Path ?? Content?.path;

        public string KindTitle => Managers.ResourceManager.Loader.GetString($"LibraryManager/KindTitle/Archive/{Kind}");

        public string Kind
        {
            get
            {
                if (Content is null) return "";
                switch (Content)
                {
                    case libraryLibraryFolder _: return "Folder";
                    case libraryLibraryArchive _: return "Archive";
                    case libraryLibraryNetwork _: return "Network";
                    default: return "";
                }
            }
        }

        private Windows.Storage.IStorageItem StorageItem;

        public string Title
        {
            get
            {
                if (Content is null) return "";
                switch (Content)
                {
                    case libraryLibraryFolder _: return StorageItem?.Name;
                    case libraryLibraryArchive a: return System.IO.Path.GetFileNameWithoutExtension(a.path);
                    case libraryLibraryNetwork n: return System.IO.Path.GetFileNameWithoutExtension(n.path);
                    default: return "";
                }
            }
        }


        private System.Windows.Input.ICommand _RemoveCommand;
        public System.Windows.Input.ICommand RemoveCommand
        {
            get
            {
                if (_RemoveCommand != null) return _RemoveCommand;
                var result = new Helper.DelegateCommand(async (parameter) =>
                  {
                      var list = Parent?.Content?.Items?.ToList();
                      if (list is null) return;
                      list.Remove(Content);
                      Parent.Content.Items = list.ToArray();
                      Storages.LibraryStorage.GarbageCollectToken();
                      //Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                      await Storages.LibraryStorage.Content.SaveAsync();
                  }, parameter =>
                  {
                      return Parent?.Content?.Items?.Contains(Content) == true;
                  });
                if (Parent?.Content != null) Parent.Content.PropertyChanged += (s, e) => result.OnCanExecuteChanged();
                return _RemoveCommand = result;
            }
        }
    }
}
