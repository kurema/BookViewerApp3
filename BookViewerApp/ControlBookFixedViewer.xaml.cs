using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class ControlBookFixedViewer : UserControl
    {

        public ControlBookFixedViewer()
        {
            this.InitializeComponent();

            this.DataContext = new ControlBookFixedViewerViewModel(this.BodyControl);
        }

        public class ControlBookFixedViewerViewModel: INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            //protected void RaisePropertyChanged(string propertyName)
            //{
            //    if (PropertyChanged != null)
            //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //}

            //public ControlBookFixedViewerBody.BookFixedBodyViewModel BodyViewModel
            //{
            //    get { return _BodyViewMode; }
            //    set { _BodyViewMode = value; RaisePropertyChanged(nameof(BodyViewModel)); }
            //}
            //private ControlBookFixedViewerBody.BookFixedBodyViewModel _BodyViewMode;

            public System.Windows.Input.ICommand CommandPageNext;
            public System.Windows.Input.ICommand CommandPagePrevious;
            public System.Windows.Input.ICommand CommandSelectFile;

            public ControlBookFixedViewerViewModel(ControlBookFixedViewerBody Target)
            {
                CommandPageNext = new ControlBookFixedViewerBody.CommandAddPage(Target, 1);
                CommandPagePrevious = new ControlBookFixedViewerBody.CommandAddPage(Target, -1);
                CommandSelectFile = new ControlBookFixedViewerBody.CommandOpen(Target);

                //Target.DataContext = this._BodyViewMode = new ControlBookFixedViewerBody.BookFixedBodyViewModel();
                //Target.DataContextChanged += (s, e) => { RaisePropertyChanged(nameof(BodyViewModel)); };

            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            ((ControlBookFixedViewerViewModel)this.DataContext).CommandSelectFile.Execute(null);
        }
    }
}
