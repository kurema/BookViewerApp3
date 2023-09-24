using BookViewerApp.Storages.WindowStates;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;

#nullable enable

public class TabViewItemTagInfo
{
	public IWindowStateWindowTab? SessionInfo { get; set; }

	public static TabViewItemTagInfo? SetOrGetTag(FrameworkElement? element)
	{
		if (element is null) return null;
		if (element.Tag is TabViewItemTagInfo result) return result;
		result = new TabViewItemTagInfo();
		element.Tag = result;
		return result;
	}
}