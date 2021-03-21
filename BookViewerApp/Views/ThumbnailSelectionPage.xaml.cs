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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class ThumbnailSelectionPage : Page
    {
        public Microsoft.Toolkit.Uwp.UI.Controls.ImageCropper ImageCropper => this.imageCropper;

        private Books.IBookFixed book;
        public Books.IBookFixed Book
        {
            get => book; set
            {
                book = value;
                currentPage = 0;
                UpdatePage();
                UpdateImage();
                _CommandAddPage?.OnCanExecuteChanged();
            }
        }

        private uint currentPage = 0;

        public uint CurrentPage
        {
            get => currentPage; set
            {
                if (currentPage == value) return;
                currentPage = value;
                UpdatePage();
                UpdateImage();
                _CommandAddPage?.OnCanExecuteChanged();
            }
        }

        private void UpdatePage() => textBoxPage.Text = $"{CurrentPage} / {Book?.PageCount ?? 0}";
        private async void UpdateImage()
        {
            if (Book is null) return;
            int size = Managers.ThumbnailManager.ThumbnailSize;
            if (!GetIfPageIsInRange(CurrentPage)) return;
            if (!(ImageCropper.Source is Windows.UI.Xaml.Media.Imaging.WriteableBitmap wbmp)) return;
            await Book.GetPage(CurrentPage).SetBitmapAsync(wbmp, size * 2, size * 2);
            ImageCropper.Reset();
            
        }

        private bool GetIfPageIsInRange(long target) => target >= 0 && target < Book.PageCount;

        public Helper.DelegateCommand _CommandAddPage;

        public Helper.DelegateCommand CommandAddPage => _CommandAddPage ??= new Helper.DelegateCommand((param) =>
        {
            if (!int.TryParse(param.ToString(), out int delta)) delta = 0;
            CurrentPage = (uint)Math.Clamp(CurrentPage + delta, 0, Book.PageCount);
        }, (param) =>
        {
            if (!int.TryParse(param.ToString(), out int delta)) delta = 0;
            return GetIfPageIsInRange(CurrentPage + delta);
        });

        public ThumbnailSelectionPage()
        {
            this.InitializeComponent();

            int size = Managers.ThumbnailManager.ThumbnailSize;
            ImageCropper.Source = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap(size, size);
        }


    }
}
