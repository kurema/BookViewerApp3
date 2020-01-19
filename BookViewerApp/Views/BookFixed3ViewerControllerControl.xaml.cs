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

namespace BookViewerApp
{
    public sealed partial class BookFixed3ViewerControllerControl : UserControl
    {
        public BookFixed3ViewerControllerControl()
        {
            this.InitializeComponent();

            this.LosingFocus += BookFixed3ViewerControllerControl_LosingFocus;
            try
            {
                this.Focus(FocusState.Programmatic);
            }
            catch { }
        }

        private void BookFixed3ViewerControllerControl_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            var ui = args.NewFocusedElement as FrameworkElement;
            if ((this.DataContext as ViewModels.BookViewModel)?.IsControlPinned == true)
            {
                return;
            }
            if (this.BaseUri != ui?.BaseUri && !(ui is Popup))
                SetControlPanelVisibility(false);
        }

        public void SetControlPanelVisibility(bool visibility)
        {
            if (visibility)
            {
                VisualStateManager.GoToState(this, "ControlPanelFadeIn", true);
                try
                {
                    this.Focus(FocusState.Programmatic);
                }
                catch { }
            }
            else
            {
                VisualStateManager.GoToState(this, "ControlPanelFadeOut", true);
            }
        }

        private void ListView_SelectBookmark(object sender, ItemClickEventArgs e)
        {
            if (e.OriginalSource is ListView view)
            {
                if (e.ClickedItem is ViewModels.BookmarkViewModel model)
                {
                    if (this.DataContext is ViewModels.BookViewModel v)
                    {
                        v.PageSelectedDisplay = model.Page;
                    }
                }
            }
        }
    }
}
