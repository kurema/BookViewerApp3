
using System;
using System.ComponentModel;

using System.Windows.Input;


namespace kurema.BrowserControl.ViewModels
{
    public class DownloadItemViewModel : INotifyPropertyChanged
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


        private double _DownloadedRate = 0;
        public double DownloadedRate { get => _DownloadedRate; set => SetProperty(ref _DownloadedRate, value); }


        private string _FileName;

        public DownloadItemViewModel(string fileName)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public string FileName { get => _FileName; set => SetProperty(ref _FileName, value); }


        private ICommand _OpenCommand = new InvalidCommand();
        public ICommand OpenCommand { get => _OpenCommand; set => SetProperty(ref _OpenCommand, value); }

        public class InvalidCommand : ICommand
        {
#pragma warning disable 0067
            //https://anopara.net/2013/12/03/c%E3%82%B3%E3%83%B3%E3%83%91%E3%82%A4%E3%83%AB%E6%99%82%E3%81%AE%E8%AD%A6%E5%91%8A%E3%82%92%E6%8A%91%E5%88%B6%E3%81%99%E3%82%8B/
            public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

            public bool CanExecute(object parameter)
            {
                return false;
            }

            public void Execute(object parameter)
            {
            }
        }

    }
}
