using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace kurema.FileExplorerControl.ViewModels
{
    public class FileExplorerViewModel : INotifyPropertyChanged
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

        private ObservableCollection<FileItemViewModel> _ItemsTree;
        public ObservableCollection<FileItemViewModel> ItemsTree { get => _ItemsTree; set => SetProperty(ref _ItemsTree, value); }


        private ObservableCollection<FileItemViewModel> _ItemsMain;

        public ObservableCollection<FileItemViewModel> ItemsMain { get => _ItemsMain; set => SetProperty(ref _ItemsMain, value); }

    }
}
