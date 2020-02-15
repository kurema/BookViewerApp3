using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading.Tasks;


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

        public async Task SetFolder(ViewModels.FileItemViewModel folder)
        {
            if(this.DataContext is ViewModels.ContentViewModel vm)
            {
                if (folder.Children == null) await folder.UpdateChildren();
                vm.Item = folder;
            }
        }

        public TypedEventHandler<FileExplorerContentControl, ViewModels.FileItemViewModel> FileOpenedEventHandler;

        private async void Button_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if((sender as Button)?.DataContext is ViewModels.FileItemViewModel vm1)
            {
                if (vm1.IsFolder)
                {
                    await SetFolder(vm1);
                }
                else
                {
                    FileOpenedEventHandler?.Invoke(this, vm1);
                }

            }
        }
    }
}
