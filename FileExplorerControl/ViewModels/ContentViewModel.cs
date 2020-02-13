using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;

namespace kurema.FileExplorerControl.ViewModels
{
    public class ContentViewModel : INotifyPropertyChanged
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


        private ContentStyles _ContentStyle=ContentStyles.Icon;
        public ContentStyles ContentStyle { get => _ContentStyle; set => SetProperty(ref _ContentStyle, value); }

        public enum ContentStyles
        {
            Detail, List, Icon ,IconWide
        }

        public bool IsGridView
        {
            get
            {
                switch (ContentStyle)
                {
                    case ContentStyles.Detail:
                        return false;
                    case ContentStyles.List:
                    case ContentStyles.Icon:
                    case ContentStyles.IconWide:
                        return true;
                    default:
                        return false;
                }
            }
        }


        private ObservableCollection<FileItemViewModel> _Items;
        public ObservableCollection<FileItemViewModel> Items { get => _Items; set => SetProperty(ref _Items, value); }

    }
}
