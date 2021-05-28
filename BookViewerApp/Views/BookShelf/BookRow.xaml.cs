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

#nullable enable
namespace BookViewerApp.Views.Bookshelf
{
    public sealed partial class BookRow : UserControl
    {
        public UIElementCollection Children => this.BookRowMain.Children;

        /// <summary>
        /// Use this instead of Margin for proper shadow.
        /// </summary>
        public Thickness MarginPanel
        {
            get { return (Thickness)GetValue(MarginPanelProperty); }
            set { SetValue(MarginPanelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MarginPanel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarginPanelProperty =
            DependencyProperty.Register("MarginPanel", typeof(Thickness), typeof(BookRowPanel), new PropertyMetadata(new Thickness()));



        public Size Spacing
        {
            get { return (Size)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Spacing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing", typeof(Size), typeof(BookRow), new PropertyMetadata(new Size()));

        public int MaxLine
        {
            get { return (int)GetValue(MaxLineProperty); }
            set { SetValue(MaxLineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxLine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxLineProperty =
            DependencyProperty.Register("MaxLine", typeof(int), typeof(BookRow), new PropertyMetadata(1));



        public bool AllowOverflow
        {
            get { return (bool)GetValue(AllowOverflowProperty); }
            set { SetValue(AllowOverflowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowOverflow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowOverflowProperty =
            DependencyProperty.Register("AllowOverflow", typeof(bool), typeof(BookRow), new PropertyMetadata(true));

        public Thickness MarginHeader { get => GridHeader.Margin; set => GridHeader.Margin = value; }

        public System.Windows.Input.ICommand? CommandExpand { get; set; }

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

            //following line cause crash:
            //https://github.com/microsoft/microsoft-ui-xaml/issues/2133
            SharedShadow.Receivers.Add(BackgroundGrid);
            BookRowMain.LayoutUpdated += (s, e) => { UpdateShadow(); };
        }
        public void UpdateShadow()
        {
            if (BookRowMain is null) return;
            foreach (var target in BookRowMain.ShadowTargets)
            {
                target.Shadow = SharedShadow;
            }
        }
    }
}