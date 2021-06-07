using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Windows.Input;

namespace BookViewerApp.ViewModels
{
    public class ListItemViewModel : INotifyPropertyChanged
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


        private string _Title;
        public string Title { get => _Title; set => SetProperty(ref _Title, value); }

        private string _Description;
        public string Description { get => _Description; set => SetProperty(ref _Description, value); }

        private ICommand _OpenCommand;
        public ICommand OpenCommand { get => _OpenCommand; set => SetProperty(ref _OpenCommand, value); }


        public ListItemViewModel(string title, string description = "", ICommand openCommand = null, object tag = null)
        {
            _Title = title ?? throw new ArgumentNullException(nameof(title));
            _Description = description ?? "";
            _OpenCommand = openCommand ?? new Helper.InvalidCommand();
            _Tag = tag;
        }

        public ListItemViewModel()
        {
        }

        private object _Tag;

        public object Tag { get => _Tag; set => SetProperty(ref _Tag, value); }


        private string _GroupTag;
        public string GroupTag { get => _GroupTag; set => SetProperty(ref _GroupTag, value); }

    }
}
