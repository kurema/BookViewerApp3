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

namespace BookViewerApp.InfoPageViewModel
{
    public class Info : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;//never be called.

        private Windows.ApplicationModel.Package CurrentPackage { get { return Windows.ApplicationModel.Package.Current; } }

        private Windows.ApplicationModel.PackageId ID { get { return Windows.ApplicationModel.Package.Current.Id; } }

        public string VersionText { get { return string.Format("{0}.{1}.{2}.{3}",VersionInfo.Major,VersionInfo.Minor,VersionInfo.Revision,VersionInfo.Revision); } }

        public Windows.ApplicationModel.PackageVersion VersionInfo { get { return CurrentPackage.Id.Version; } }

        public string Architecture { get { return ID.Architecture.ToString(); } }

        public string Name { get { return ID.Name; } }

        public string FamilyName { get { return ID.FamilyName; } }

        public string FullName { get { return ID.FullName; } }

        public string Publisher { get { return ID.Publisher; } }

        public string PublisherID { get { return ID.PublisherId; } }

        public string InstalledLocation { get { return CurrentPackage.InstalledLocation.Path; } }

        public string DisplayName { get { return CurrentPackage.DisplayName; } }

        public string PublisherDisplayName { get { return CurrentPackage.PublisherDisplayName; } }

        public Uri LogoUri { get { return CurrentPackage.Logo; } }

        public string Description { get { return CurrentPackage.Description; } }

        public string InstalledDate { get { return CurrentPackage.InstalledDate.ToString(); } }

        public License License { get
            {
                return License.MIT;
            }
        }
    }

    public class License : INotifyPropertyChanged
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
            set { _Content = value;OnPropertyChanged(nameof(Content)); }
        }
        private string _Content;

        public static License MIT { get
            {
                return new License() {
                    Title = "The MIT License(MIT)",
                                        Content = @"The MIT License (MIT)
Copyright (c) <year> <copyright holders>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
                    Uri = new Uri("http://opensource.org/licenses/mit-license.php")
                };
            } }
    }

    public class SimpleTile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }

        public string Title { get { return _Title; } set { _Title = value;OnPropertyChanged(nameof(Title)); } }
        private string _Title = "";

        public string Content { get { return _Content; } set { _Content = value; OnPropertyChanged(nameof(Content)); } }
        private string _Content = "";
    }
}
