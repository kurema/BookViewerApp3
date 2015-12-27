using System;
using System.Collections.Generic;
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

using System.ComponentModel;
using System.Threading.Tasks;
// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class ControlPageViewer : UserControl
    {
        private BookViewerApp.Books.IPage Page { get { return ((PageViewModel)this.DataContext).Page; } set { ((PageViewModel)this.DataContext).Page = value; } }

        public ControlPageViewer()
        {
            this.InitializeComponent();

            this.DataContextChanged += ControlPageViewer_DataContextChanged;

            //double zoomf = 1.0 / (double)Windows.Graphics.Display.DisplayInformation.GetForCurrentView().ResolutionScale * 100.0;
            //TargetImage.RenderTransform = new ScaleTransform() { CenterX=0.5,CenterY=0.5,ScaleX=zoomf,ScaleY=zoomf};//fix me! I need LayerTransform!
        }

        private async void ControlPageViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var p = this.DataContext;
            if(this.DataContext!=null && ((PageViewModel)this.DataContext).SelfCalculating==false)await LoadAsync();
        }

        public async System.Threading.Tasks.Task SetPageAsync(Books.IPage page) {
            this.Page = page;
            await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync() {
            if (this.Page != null)
            {
                this.TargetImage.Source = await Page.GetImageSourceAsync();
            }
        }

        public async void UpdateSize()
        {
            //ToDo: 一定時間待機してサイズ調節が落ち着くのを待つ。
            if (this.Page != null && (await Page.UpdateRequired()))
            {
                var src = await Page.GetImageSourceAsync();
                if (src != null)
                {
                    this.TargetImage.Source = src;
                }
            }
        }

        public class PageViewModel : INotifyPropertyChanged
        {
            public PageViewModel(Books.IPage page) { this.Page = page; }
            public PageViewModel() {  }

            public event PropertyChangedEventHandler PropertyChanged;

            public bool SelfCalculating = false;

            public Books.IPage Page { get { Prepare();return _PageCache;  } set { _PageCache = value; PageCacheLatest = true; RaisePropertyChanged(nameof(Page)); } }
            private Books.IPage _PageCache;
            private bool PageCacheLatest = false;

            private Func<Books.IPage> PageAccessor;

            public void SetPageAccessor(Func<Books.IPage> accessor)
            {
                this.PageAccessor = accessor;
                PageCacheLatest = false;
                RaisePropertyChanged(nameof(Page));
            }

            public void Prepare()
            {
                if (!PageCacheLatest)
                {
                    SelfCalculating = true;
                    this._PageCache = PageAccessor();
                    PageCacheLatest = true;
                    PageAccessor = null;
                    SelfCalculating = false;
                }
            }

            protected void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
