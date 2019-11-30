using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Data;

namespace BookViewerApp.ViewModels
{
    public class BookShelf2ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string selectedPage;
        public string SelectedPage
        {
            get => selectedPage; set
            {
                selectedPage = value;
                if (selectedPage != value)
                    OnPropertyChanged(nameof(SelectedPage));
            }
        }

        private System.Windows.Input.ICommand _SelectCommand;
        public System.Windows.Input.ICommand SelectCommand =>
            _SelectCommand = _SelectCommand ?? new DelegateCommand((p) => this.SelectedPage = p.ToString());
    }    
}
