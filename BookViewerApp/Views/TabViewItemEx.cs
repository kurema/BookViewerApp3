using Microsoft.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;

#nullable enable

public class TabViewItemEx : TabViewItem
{
	public object? SessionInfo { get; set; }
}
