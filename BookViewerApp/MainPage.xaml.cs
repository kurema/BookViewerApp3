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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            //var book = new Books.Image.ImageBookUriCollection("https://www.google.co.jp/images/branding/googlelogo/2x/googlelogo_color_272x92dp.png", "http://k.yimg.jp/images/top/sp2/cmn/logo-ns-131205.png");
            //this.cbfv.DataContext = new ControlBookFixedViewer.BookViewModel(book);

        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            //await cpv.SetPageAsync(new Books.Image.ImagePageUrl(new Uri("https://www.google.co.jp/images/branding/googlelogo/2x/googlelogo_color_272x92dp.png")));
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            var file = await picker.PickSingleFileAsync();
            var book = new Books.Pdf.PdfBook();
            await book.Load(file);
            this.cbfv.DataContext = new ControlBookFixedViewer.BookViewModel(book);
        }
    }
}
