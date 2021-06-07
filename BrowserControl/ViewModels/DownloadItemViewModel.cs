using System;
using System.ComponentModel;

using System.Windows.Input;
using Windows.Storage;

namespace kurema.BrowserControl.ViewModels
{
    public class DownloadItemViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
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


        private double _DownloadedRate = 0;

        public DownloadItemViewModel(StorageFile file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public double DownloadedRate { get => _DownloadedRate; set => SetProperty(ref _DownloadedRate, value); }

        public StorageFile File { get; private set; }

    }
}
