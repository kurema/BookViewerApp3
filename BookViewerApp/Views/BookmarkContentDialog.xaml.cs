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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views
{
    public sealed partial class BookmarkContentDialog : ContentDialog
    {
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty AddressProperty = DependencyProperty.Register("Address", typeof(string), typeof(BookmarkContentDialog), new PropertyMetadata(""));

        public string TitleBookmark
        {
            get { return (string)GetValue(TitleBookmarkProperty); }
            set { SetValue(TitleBookmarkProperty, value); }
        }

        public static readonly DependencyProperty TitleBookmarkProperty = DependencyProperty.Register("TitleBookmark", typeof(string), typeof(BookmarkContentDialog), new PropertyMetadata(""));

        public BookmarkContentDialog()
        {
            this.InitializeComponent();

            this.PrimaryButtonText = Managers.ResourceManager.Loader.GetString("Word/OK");
            this.SecondaryButtonText = Managers.ResourceManager.Loader.GetString("Word/Cancel");
        }
    }
}
