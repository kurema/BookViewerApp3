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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class ControlPageViewer : UserControl
    {
        //protected override Size ArrangeOverride(Size finalSize)
        //{
        //    var aow = base.ArrangeOverride(finalSize).Width;
        //    var aoh = base.ArrangeOverride(finalSize).Height;
        //    var vpw = ((ScrollViewer)Parent).ActualWidth;
        //    var vph = ((ScrollViewer)Parent).ActualHeight;
        //    //var iaw = TargetImage.ActualWidth;
        //    //var iah = TargetImage.ActualHeight;
        //    //var w = Math.Min(aow, aoh / iah * iaw);
        //    //w = double.IsNaN(w) ? 0 : w;
        //    //var h = Math.Min(aoh, aow / iaw * iah);
        //    //h = double.IsNaN(h) ? 0 : h;

        //    var w = Math.Min(aow, vpw);
        //    w = double.IsNaN(w) ? aow : w;
        //    var h = Math.Min(aoh, vph);
        //    h = double.IsNaN(h) ? aoh : h;

        //    return new Size(vpw, vph);
        //}


        public BookViewerApp.Books.IPage Page { get; private set; }

        public ControlPageViewer()
        {
            this.InitializeComponent();
        }

        public async System.Threading.Tasks.Task SetPageAsync(Books.IPage page) {
            this.Page = page;
            await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync() {
            //Do not call me when Page is null;
            this.TargetImage.Source = await Page.GetImageSourceAsync();
        }
    }
}
