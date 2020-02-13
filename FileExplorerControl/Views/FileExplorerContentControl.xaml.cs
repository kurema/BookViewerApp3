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

namespace kurema.FileExplorerControl.Views
{
    public sealed partial class FileExplorerContentControl : UserControl
    {
        public FileExplorerContentControl()
        {
            this.InitializeComponent();

            if (this.DataContext is ViewModels.ContentViewModel vm)
            {
                vm.ContentStyle = ViewModels.ContentViewModel.ContentStyles.Icon;
            }

        }

        public void SetFolder(ViewModels.FileItemViewModel folder)
        {
            if(this.DataContext is ViewModels.ContentViewModel vm)
            {
                vm.Items = folder.Children;
            }
        }
    }

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
