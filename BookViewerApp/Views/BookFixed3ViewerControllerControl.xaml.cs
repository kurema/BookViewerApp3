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

using BookViewerApp.Storages;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views;

public sealed partial class BookFixed3ViewerControllerControl : UserControl
{
	private ViewModels.BookViewModel Binding => (ViewModels.BookViewModel)this.DataContext;

	public BookFixed3ViewerControllerControl()
	{
		this.InitializeComponent();

		this.LosingFocus += BookFixed3ViewerControllerControl_LosingFocus;

		if (!(bool)SettingStorage.GetValue("ShowRightmostAndLeftmost"))
		{
			this.ButtonLeftmost.Visibility = Visibility.Collapsed;
			this.ButtonRightmost.Visibility = Visibility.Collapsed;
		}

		this.Loaded += (s, e) =>
		{
			try
			{
				this.Focus(FocusState.Programmatic);
			}
			catch { }
		};
	}

	public void SetBackgroundSource(AcrylicBackgroundSource source)
	{
		if (ControlPanel_ControlPanelVisibilityStates_Border.Background is AcrylicBrush brush)
		{
			brush.BackgroundSource = source;
		}
	}

	private void BookFixed3ViewerControllerControl_LosingFocus(UIElement sender, LosingFocusEventArgs args)
	{
		var ui = args.NewFocusedElement as FrameworkElement;
		if ((this.DataContext as ViewModels.BookViewModel)?.IsControlPinned == true)
		{
			return;
		}
		if (this.BaseUri == ui?.BaseUri) return;
		if (ui is Popup) return;
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
		IsControlVisible = visibility;
	}

	private void ListView_SelectBookmark(object sender, ItemClickEventArgs e)
	{
		if (e.OriginalSource is not ListView || e.ClickedItem is not ViewModels.BookmarkViewModel model || this.DataContext is not ViewModels.BookViewModel v) return;
		//Should I add 1? Tested. No.
		v.PageSelectedDisplay = model.Page;
	}

	private void PageInfoElement_Tapped(object sender, TappedRoutedEventArgs e)
	{
		FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
	}

	private void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
	{
		this.SetControlPanelVisibility(true);
	}

	private void Scroller_Tapped(object sender, TappedRoutedEventArgs e)
	{
		var ui = (Canvas)sender;
		var rate = e.GetPosition(ui).X / ui.ActualWidth;
		if (Binding.Reversed) { rate = 1 - rate; }
		Binding.ReadRate = rate;
	}

	private void Scroller_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var ui = (Canvas)sender;
		var cp = e.GetCurrentPoint(ui);
		if (!cp.IsInContact) return;
		var rate = cp.Position.X / ui.ActualWidth;
		if (Binding.Reversed) { rate = 1 - rate; }
		Binding.ReadRate = rate;// Math.Round(rate * (Binding.PagesCount)) / (Binding.PagesCount);//何故だっけ？
		e.Handled = true;
	}

	private void InitializeZoomFactor(object sender, TappedRoutedEventArgs e)
	{
		if (Binding?.PageSelectedViewModel?.ZoomFactor != null)
		{
			Binding.PageSelectedViewModel.ZoomFactor = 1.0f;
		}
	}

	private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
	{
		if (args.InvokedItem is ViewModels.TocEntryViewModes toc)
		{
			Binding.PageSelectedDisplay = toc.Page + 1;
		}
	}

	private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is not ListView) return;
		if (e.AddedItems.Count != 1) return;
		if (e.AddedItems[0] is not kurema.FileExplorerControl.Models.FileItems.StorageFileItem file) return;
		if (Binding is null) return;
		FrameworkElement current = this;

		BookFixed3Viewer parent;
		while (current != null)
		{
			if (current.Parent is BookFixed3Viewer viewer)
			{
				parent = viewer;
				goto Init;
			}
			current = current.Parent as FrameworkElement;
		}
		return;

	Init:
		Binding.FileItem = file;
		await Binding.InitializeAsync(file.Content as Windows.Storage.IStorageFile, parent.FlipViewControl);
	}

	//private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	//{
	//    if (Binding?.PageSelectedViewModel != null)
	//    {
	//        Binding.PageSelectedViewModel.ZoomRequest((float)((sender as Slider).Value / 100.0));
	//    }
	//}

	//public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(
	//    nameof(HorizontalOffset), typeof(float), typeof(FlipViewEx), new PropertyMetadata(0.0f, (s, e) =>
	//    {
	//        if (s is not FlipViewEx ex) return;
	//        if (e.NewValue is not float f) return;
	//    }));

	public static readonly DependencyProperty IsControlVisibleProperty = DependencyProperty.Register(
		nameof(IsControlVisible), typeof(bool), typeof(BookFixed3ViewerControllerControl), new PropertyMetadata(true));

	public bool IsControlVisible { get => (bool)GetValue(IsControlVisibleProperty); set => SetValue(IsControlVisibleProperty, value); }

	private void ListView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var option = new FlyoutShowOptions();
		if (args.TryGetPosition(sender, out Point p)) option.Position = p;
		if ((args.OriginalSource as FrameworkElement)?.DataContext is not ViewModels.BookmarkViewModel vm) return;
		if (vm.AutoGenerated) return;
		var flyout = new MenuFlyout();
		{
			var item = new MenuFlyoutItem() { Text = Managers.ResourceManager.Loader.GetString("Bookmark/Delete/Title") };
			item.Click += (_, _) =>
			{
				if (vm.Page == Binding.PageSelectedDisplay)
				{
					Binding.CurrentBookmarkName = string.Empty;
				}
				else Binding.Bookmarks.Remove(vm);
			};
			flyout.Items.Add(item);
		}
		flyout.ShowAt(sender, option);
	}

	private void BookReaderControls_Command_Border_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (BookReaderControls_Command_Border.Child is not Grid g) return;
		var panel = BookReaderControlsCommandBar_Right;
		var buttonSize = panel.Children[0].ActualSize.X;
		var extra = panel.ActualWidth - g.ColumnDefinitions[2].ActualWidth - buttonSize * 0.5;
		if (BookReaderControlsCommandBar_Right_More_Button.Visibility == Visibility.Collapsed) extra += buttonSize;

		int count = (int)Math.Ceiling(extra / buttonSize);

		if (count + BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count == 1)
		{
			while (BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count != 0)
			{
				var element = BookReaderControlsCommandBar_Right_More_StackPanel.Children[BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count - 1];
				BookReaderControlsCommandBar_Right_More_StackPanel.Children.Remove(element);
				panel.Children.Insert(0, element);
			}
		}
		else if (count > 0)
		{
			for (int i = 0; i < count && panel.Children.Count > 1; i++)
			{
				var element = panel.Children[0];
				if (element == BookReaderControlsCommandBar_Right_More_Button) break;
				panel.Children.Remove(element);
				BookReaderControlsCommandBar_Right_More_StackPanel.Children.Add(element);
			}
		}
		else if (count < 0)
		{
			for (int i = 0; i > count && BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count > 0; i--)
			{
				var element = BookReaderControlsCommandBar_Right_More_StackPanel.Children[BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count - 1];
				BookReaderControlsCommandBar_Right_More_StackPanel.Children.Remove(element);
				panel.Children.Insert(0, element);
			}
		}

		BookReaderControlsCommandBar_Right_More_Button.Visibility = BookReaderControlsCommandBar_Right_More_StackPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}
}
