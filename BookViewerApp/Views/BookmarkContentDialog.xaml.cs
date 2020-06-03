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
        public string AddressBookmark
        {
            get { return (string)GetValue(AddressBookmarkProperty); }
            set
            {
                SetValue(AddressBookmarkProperty, value);
            }
        }

        public static readonly DependencyProperty AddressBookmarkProperty = DependencyProperty.Register("Address", typeof(string), typeof(BookmarkContentDialog), new PropertyMetadata("", new PropertyChangedCallback((a, b) =>
        {
            string GetHost(string address)
            {
                Uri uri;
                if (Uri.TryCreate(address, UriKind.Absolute, out uri))
                {
                    return uri.Host;
                }
                return "";
            }

            if (a is BookmarkContentDialog dialog)
            {
                Uri uri1;
                if (string.IsNullOrWhiteSpace(dialog.TitleBookmark) || GetHost(b.OldValue.ToString()) == dialog.TitleBookmark) dialog.TitleBookmark = GetHost(b.NewValue.ToString());
                dialog.IsPrimaryButtonEnabled = Uri.TryCreate(b.NewValue.ToString(), UriKind.Absolute, out uri1) && (uri1.Scheme == Uri.UriSchemeHttp || uri1.Scheme == Uri.UriSchemeHttps);
            }
        })));

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
            this.IsPrimaryButtonEnabled = false;
        }

        public Storages.Library.libraryBookmarksContainerBookmark GetLibraryBookmark()
        {
            return new Storages.Library.libraryBookmarksContainerBookmark()
            {
                created=DateTime.Now,
                createdSpecified=true,
                title=this.TitleBookmark,
                url=this.AddressBookmark,
            };
        }
    }
}
