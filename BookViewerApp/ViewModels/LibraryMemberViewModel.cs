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
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion


        private libraryLibrary _Content;

        public LibraryMemberViewModel(libraryLibrary content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public LibraryMemberViewModel()
        {
        }

        public libraryLibrary Content
        {
            get => _Content;
            set
            {
                SetProperty(ref _Content, value);
                if (value != null)
                    value.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(Content.Items))
                        {
                            OnPropertyChanged(nameof(Items));
                        }
                        Storages.LibraryStorage.OnLibraryUpdateRequest(Storages.LibraryStorage.LibraryKind.Library);
                    };
            }
        }

        public ObservableCollection<LibraryMemberItemViewModel> Items
        {
            get
            {
                var result = new ObservableCollection<LibraryMemberItemViewModel>(Content?.Items.Select(a => new LibraryMemberItemViewModel(a as IlibraryLibraryItem, this)));
                result.CollectionChanged += Result_CollectionChanged;
                return result;
            }
        }

        private void Result_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Content == null) return;
            Content.Items = (sender as ObservableCollection<LibraryMemberItemViewModel>)?.Select(a => a.Content)?.ToArray() ?? Content.Items;
        }
    }

    public class LibraryMemberItemViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
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


        private LibraryMemberViewModel _Parent;
        public LibraryMemberViewModel Parent { get => _Parent; private set => SetProperty(ref _Parent, value); }

        private IlibraryLibraryItem _Member;

        public LibraryMemberItemViewModel(IlibraryLibraryItem member, LibraryMemberViewModel parent)
        {
            Content = member ?? throw new ArgumentNullException(nameof(member));
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public IlibraryLibraryItem Content
        {
            get => _Member; set
            {
                SetProperty(ref _Member, value);
                OnPropertyChanged(nameof(Path));
            }
        }

        public string Path => Content?.path ?? "";

        public string Title => Path == null ? "" : System.IO.Path.GetFileNameWithoutExtension(Path);

        public string KindTitle
        {
            get
            {
                if (Content == null) return "";
                switch (Content)
                {
                    case Storages.Library.libraryLibraryFolder _: return Managers.ResourceManager.Loader.GetString("LibraryManager/Kind/Folder/Title");
                    case Storages.Library.libraryLibraryArchive _: return Managers.ResourceManager.Loader.GetString("LibraryManager/Kind/Archive/Title");
                    case Storages.Library.libraryLibraryNetwork _: return Managers.ResourceManager.Loader.GetString("LibraryManager/Kind/Network/Title");
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
                var result=new Helper.DelegateCommand(async (parameter) =>
                {
                    var list = Parent?.Content?.Items?.ToList();
                    if (list == null) return;
                    list.Remove(Content);
                    Parent.Content.Items = list.ToArray();
                }, parameter =>
                {
                    return Parent?.Items?.Count > 1;
                });
                if (Parent?.Content != null) Parent.Content.PropertyChanged += (s, e) => result.OnCanExecuteChanged();
                return _RemoveCommand = result;
            }
        }

    }
}
