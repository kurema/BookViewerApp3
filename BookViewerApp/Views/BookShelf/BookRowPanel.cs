using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml;


namespace BookViewerApp.Views.Bookshelf
{
    public class BookRowPanel : Panel
    {
        public UIElement[] ShadowTargets => Children.OfType<BookInfo>().Select(a => a.ShadowTarget).ToArray();


    }
}
