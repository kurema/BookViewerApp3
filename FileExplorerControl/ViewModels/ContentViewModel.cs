using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace kurema.FileExplorerControl.ViewModels
{
    public class ContentViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
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

        #region dialog

        private Func<FileItemViewModel, bool, Task<(bool delete, bool completeDelete)>> _DialogDelete;
        public Func<FileItemViewModel, bool, Task<(bool delete, bool completeDelete)>> DialogDelete { get => _DialogDelete; set => SetProperty(ref _DialogDelete, value); }


        private Func<FileItemViewModel, Task<string>> _DialogRename;
        public Func<FileItemViewModel, Task<string>> DialogRename { get => _DialogRename; set => SetProperty(ref _DialogRename, value); }


        private Func<FileItemViewModel, Task> _DialogProperty;
        public Func<FileItemViewModel, Task> DialogProperty { get => _DialogProperty; set => SetProperty(ref _DialogProperty, value); }


        #endregion

        private ContentStyles _ContentStyle = ContentStyles.Icon;
        public ContentStyles ContentStyle
        {
            get => _ContentStyle; set
            {
                SetProperty(ref _ContentStyle, value);
                (SetContentStyleCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                OnPropertyChanged(nameof(IsDataGrid));
                OnPropertyChanged(nameof(IconSizeVariable));
            }
        }

        public enum ContentStyles
        {
            Detail, List, Icon, IconWide
        }

        public bool IsDataGrid
        {
            get
            {
                switch (ContentStyle)
                {
                    case ContentStyles.Detail:
                        return true;
                    case ContentStyles.List:
                    case ContentStyles.Icon:
                    case ContentStyles.IconWide:
                        return false;
                    default:
                        return false;
                }
            }
        }

        public bool IconSizeVariable
        {
            get
            {
                switch (ContentStyle)
                {
                    case ContentStyles.Detail:
                    case ContentStyles.List:
                        return false;
                    case ContentStyles.Icon:
                    case ContentStyles.IconWide:
                        return true;
                    default:
                        return false;
                }
            }
        }


        private double _IconSize = 75.0;
        public double IconSize { get => _IconSize; set => SetProperty(ref _IconSize, value); }

        public ObservableCollection<FileItemViewModel> History { get; } = new ObservableCollection<FileItemViewModel>();

        private int _SelectedHistory;
        public int SelectedHistory
        {
            get => _SelectedHistory; set
            {
                if (!SelectedHistoryWithinRange(value))
                {
                    throw new IndexOutOfRangeException();
                }
                SetProperty(ref _SelectedHistory, value);
                OnPropertyChanged(nameof(Item));

                (HistoryShiftCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                (GoUpCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                (LaunchCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                (RefreshCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
            }
        }

        private bool SelectedHistoryWithinRange(int target)
        {
            return History != null && 0 <= target && target < History.Count;
        }

        private ICommand _RefreshCommand;
        public ICommand RefreshCommand => _RefreshCommand = _RefreshCommand ?? new Helper.DelegateCommand(
            async (a) => await Item?.UpdateChildren(),
            (a) => Item != null
            );


        private ICommand _LaunchCommand;
        public ICommand LaunchCommand { get => _LaunchCommand = _LaunchCommand ?? new Helper.DelegateCommand(async (parameter) =>
        {
            if (Item?.Content is Models.FileItems.StorageFileItem storage && storage.Content is Windows.Storage.IStorageFolder folder)
            {
                await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
        }, (_) => Item?.Content is Models.FileItems.StorageFileItem); 
            set => _LaunchCommand = value;
        }

        private ICommand _HistoryShiftCommand;
        public ICommand HistoryShiftCommand => _HistoryShiftCommand = _HistoryShiftCommand ?? new Helper.DelegateCommand(
            (a) =>
            {
                this.SelectedHistory += int.Parse(a.ToString());
            },
            (a) =>
            {
                return SelectedHistoryWithinRange(this.SelectedHistory + int.Parse(a.ToString()));
            }
            );

        private ICommand _SetContentStyleCommand;

        public ICommand SetContentStyleCommand => _SetContentStyleCommand = _SetContentStyleCommand ?? new Helper.DelegateCommand(
            a =>
            {
                var atxt = a.ToString();
                foreach (ContentStyles item in Enum.GetValues(typeof(ContentStyles)))
                {
                    if (atxt == Enum.GetName(typeof(ContentStyles), item))
                    {
                        this.ContentStyle = item;
                        return;
                    }
                }
            },
            a =>
            {
                if (a?.ToString() == this.ContentStyle.ToString()) return false;
                var atxt = a?.ToString();
                foreach (ContentStyles item in Enum.GetValues(typeof(ContentStyles)))
                {
                    if (atxt == Enum.GetName(typeof(ContentStyles), item))
                    {
                        return true;
                    }
                }
                return false;
            }
            );


        private ICommand _GoUpCommand;
        public ICommand GoUpCommand => _GoUpCommand = _GoUpCommand ?? new Helper.DelegateCommand(
            async (a) =>
            {
                if (Item.Parent.Children is null) await Item.Parent.UpdateChildren();
                Item = Item.Parent;
            },
            (a) =>
            {
                return Item?.Parent != null;
            }
            );

        private ICommand _GoToCommand;
        public ICommand GoToCommand => _GoToCommand = _GoToCommand ?? new Helper.DelegateCommand(
            async a =>
            {
                if (a is FileItemViewModel vm)
                {
                    if (vm.Children is null) await vm.UpdateChildren();
                    Item = vm;
                }
            }
            );

        public FileItemViewModel Item
        {
            get => SelectedHistoryWithinRange(SelectedHistory) ? History[SelectedHistory] : null;
            set
            {
                while (History.Count > SelectedHistory + 1)
                {
                    History.Remove(History.Last());
                }
                History.Add(value);
                SelectedHistory = History.Count - 1;

                if (value.Children is null)
                {
                    Task.Run(
                        async () =>
                        {
                            await value.UpdateChildren();
                        }
                        );
                }

            }
        }

        //private FileItemViewModel _RootItem;
        //public FileItemViewModel RootItem { get => _RootItem; set => SetProperty(ref _RootItem, value); }

        public void SetDefaultOrderSelectors()
        {
            OrderSelectors = new ObservableCollection<OrderSelector>(new[] {
            new OrderSelector()
            {
                Key="Title",Title= Application.ResourceLoader.Loader.GetString("Header/Name"),Parent=this
            },
            new OrderSelector()
            {
                Key="Date",Title=Application.ResourceLoader.Loader.GetString("Header/Date"),Parent=this
            },
            new OrderSelector()
            {
                Key="Size",Title=Application.ResourceLoader.Loader.GetString("Header/Size"),Parent=this
            },
        });
        }

        private ObservableCollection<OrderSelector> _OrderSelectors;

        public ContentViewModel()
        {
            History.CollectionChanged += (s, e) =>
              {
                  if (e.NewItems != null)
                      foreach (FileItemViewModel item in e.NewItems)
                      {
                          item.ParentContent = this;
                      }
              };
        }

        public ObservableCollection<OrderSelector> OrderSelectors { get => _OrderSelectors; set => SetProperty(ref _OrderSelectors, value); }

        public class OrderSelector : INotifyPropertyChanged
        {

            #region INotifyPropertyChanged
            protected bool SetProperty<T>(ref T backingStore, T value,
                [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
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

            private string _Key;
            public string Key { get => _Key; set => SetProperty(ref _Key, value); }


            private string _Title;
            public string Title { get => _Title; set => SetProperty(ref _Title, value); }

            public ContentViewModel Parent
            {
                get => parent; set
                {
                    SetProperty(ref parent, value);

                    if (parent.Item != null)
                        parent.Item.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(parent.Item.Order))
                            {
                                OnPropertyChanged(nameof(DirectionGlyph));
                            }
                        };

                    if (parent != null)
                        parent.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(parent.Item))
                            {
                                OnPropertyChanged(nameof(DirectionGlyph));

                                if (parent.Item != null)
                                    parent.Item.PropertyChanged += (s2, e2) =>
                                    {
                                        if (e2.PropertyName == nameof(parent.Item.Order))
                                        {
                                            OnPropertyChanged(nameof(DirectionGlyph));
                                        }
                                    };
                            }
                        };
                }
            }

            private ICommand _ShiftCommand;
            private ContentViewModel parent;

            public ICommand ShiftCommand => _ShiftCommand = _ShiftCommand ?? new Helper.DelegateCommand(
                a =>
                {
                    if (Parent?.Item?.Order is null) return;
                    Parent.Item.Order = Parent.Item.Order.GetShiftedBasicOrder(this.Key);
                }
                );

            public string DirectionGlyph
            {
                get =>
new Helper.ValueConverters.OrderToDirectionFontIconGlyphConverter().Convert(Parent.Item.Order, typeof(string), this.Key, null) as string;
            }


            //private Windows.UI.Xaml.Data.Binding _GlyphBindig;
            //public Windows.UI.Xaml.Data.Binding GlyphBindig
            //{
            //    get
            //    {
            //        if (_GlyphBindig is null) return _GlyphBindig;
            //        var result = new Windows.UI.Xaml.Data.Binding();
            //        result.Path = new Windows.UI.Xaml.PropertyPath("Parent.Item.Order");
            //        result.Converter = new Helper.ValueConverters.OrderToDirectionFontIconGlyphConverter();
            //        result.ConverterParameter = this.Key;
            //        return _GlyphBindig = result;
            //    }
            //}



        }
    }
}
