using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using BookViewerApp.Storages;

namespace BookViewerApp.ViewModels
{
    public class InfoPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;//never be called.

        private Windows.ApplicationModel.Package CurrentPackage => Windows.ApplicationModel.Package.Current;

        private Windows.ApplicationModel.PackageId ID => Windows.ApplicationModel.Package.Current.Id;

        public string VersionText => string.Format("{0}.{1}.{2}.{3}", VersionInfo.Major, VersionInfo.Minor, VersionInfo.Build, VersionInfo.Revision);

        public Windows.ApplicationModel.PackageVersion VersionInfo => CurrentPackage.Id.Version;

        public string Architecture => ID.Architecture.ToString();

        public string Name => ID.Name;

        public string FamilyName => ID.FamilyName;

        public string FullName => ID.FullName;

        public string Publisher => ID.Publisher;

        public string PublisherId => ID.PublisherId;

        public string InstalledLocation => CurrentPackage.InstalledLocation.Path;

        public string DisplayName => CurrentPackage.DisplayName;

        public string PublisherDisplayName => CurrentPackage.PublisherDisplayName;

        public Uri LogoUri => CurrentPackage.Logo;

        public string Description => CurrentPackage.Description;

        public string InstalledDate => CurrentPackage.InstalledDate.ToString();

        public LicenseViewModel License
        {
            get
            {
                var license = LicenseStorage.CurrentLicense;
                return new LicenseViewModel() { Content = license };
            }
        }

        public LicenseViewModel[] Licenses
        {
            get
            {
                var files = LicenseStorage.Licenses;
                var result = new List<LicenseViewModel>();
                foreach (var f in files)
                {
                    result.Add(new LicenseViewModel() { Content = f.Value, Title = f.Key });
                }
                return result.ToArray();
            }
        }
    }
}

namespace BookViewerApp.ViewModels
{
    public class LicenseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }

        public string Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged(nameof(Title)); }
        }
        private string _Title;

        public Uri Uri
        {
            get { return _Url; }
            set { _Url = value; OnPropertyChanged(nameof(Uri)); }
        }
        private Uri _Url;

        public string Content
        {
            get { return _Content; }
            set { _Content = value; OnPropertyChanged(nameof(Content)); }
        }
        private string _Content;
    }
}

namespace BookViewerApp.ViewModels
{
    public class SimpleTileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(nameof(Title)); } }
        private string _Title = "";

        public string Content { get { return _Content; } set { _Content = value; OnPropertyChanged(nameof(Content)); } }
        private string _Content = "";
    }
}
