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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class BookFixedViewer2 : Page
    {
        public BookFixedViewer2()
        {
            this.InitializeComponent();

            Application.Current.Suspending += CurrentApplication_Suspending;
        }

        private void CurrentApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SaveInfo();
        }

    private async void AppBarButton_OpenFile(object sender, RoutedEventArgs e)
        {
            var book = await Books.BookManager.PickBook();
            if (book != null && book is Books.IBookFixed)
                (this.DataContext as BookFixed2ViewModels.BookViewModel).Initialize(book as Books.IBookFixed, this.flipView);
        }

        public void SaveInfo()
        {
            (this.DataContext as BookFixed2ViewModels.BookViewModel).SaveInfo();
        }

    }
}
