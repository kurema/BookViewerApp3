using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views
{
    public class FileExplorerContentDataSelector : DataTemplateSelector
    {

        public DataTemplate TemplateIcon { get; set; }
        public DataTemplate TemplateList { get; set; }
        public DataTemplate TemplateIconWide { get; set; }

        public ViewModels.ContentViewModel.ContentStyles ContentStyle { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            switch (ContentStyle)
            {
                case ViewModels.ContentViewModel.ContentStyles.Detail:
                    return null;
                case ViewModels.ContentViewModel.ContentStyles.List:
                    return TemplateList;
                case ViewModels.ContentViewModel.ContentStyles.Icon:
                    return TemplateIcon;
                case ViewModels.ContentViewModel.ContentStyles.IconWide:
                    return TemplateIconWide;
                default:
                    break;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
