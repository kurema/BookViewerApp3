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

namespace BookViewerApp.Views.Bookshelf
{
    public sealed partial class BookRow : UserControl
    {
        public UIElementCollection Children => this.BookRowMain.Children;

        /// <summary>
        /// Use this instead of Margin for proper shadow.
        /// </summary>
        public Thickness MarginPanel { get => BookRowMain.Margin; set => BookRowMain.Margin = value; }

        public Size Spacing { get => BookRowMain.Spacing; set => BookRowMain.Spacing = value; }

        public int MaxLine { get => BookRowMain.MaxLine; set => BookRowMain.MaxLine = value; }

        public bool AllowOverflow { get => BookRowMain.AllowOverflow; set => BookRowMain.AllowOverflow = value; }

        public Thickness MarginHeader { get => GridHeader.Margin; set => GridHeader.Margin = value; }

        public System.Windows.Input.ICommand CommandExpand { get; set; }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(BookRow), new PropertyMetadata(""));



        public BookRow()
        {
            this.InitializeComponent();

            SharedShadow.Receivers.Add(BackgroundGrid);
            BookRowMain.LayoutUpdated += (s, e) => { UpdateShadow(); };
        }

        public void UpdateShadow()
        {
            foreach(var target in BookRowMain.ShadowTargets)
            {
                target.Shadow = SharedShadow;
            }
        }
    }
}
