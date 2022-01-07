using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace kurema.FileExplorerControl.Helper;

public static class UIHelper
{
    //public static Style MenuFlyoutPresenterStyleBasic
    //{
    //    get
    //    {
    //        var result= new Style()
    //        {
    //            TargetType = typeof(MenuFlyoutPresenter),
    //        };
    //        result.Setters.Add(new Setter(Control.BackgroundProperty,));

    //    }
    //}

    public static async Task OpenRename(Models.FileItems.IFileItem file)
    {
        {
            //Size of ContentDialog has a problem. Not my fault.
            //var dialog = new ContentDialog()
            //{
            //    Content = new Views.RenamePage(),
            //    FullSizeDesired = true,
            //};
            //{
            //    var loader = Application.ResourceLoader.Loader;
            //    dialog.CloseButtonText = loader.GetString("Command/OK");
            //}
            //await dialog.ShowAsync();
        }
        {
            //var newWindow = await Windows.UI.WindowManagement.AppWindow.TryCreateAsync();
            //var newPage = new Views.RenamePage();
            //Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetAppWindowContent(newWindow, newPage);

            //await newWindow.TryShowAsync();
        }
        {
            //https://docs.microsoft.com/ja-jp/windows/apps/design/layout/application-view
            var coreview = Windows.ApplicationModel.Core.CoreApplication.CreateNewView();
            Windows.UI.ViewManagement.ApplicationView newAppView = null;
            await coreview.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                newAppView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                var newFrame = new Frame();
                newFrame.Navigate(typeof(Views.RenamePage), file);
                Window.Current.Content = newFrame;
                Window.Current.Activate();
            });
            await Windows.UI.ViewManagement.ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id);
        }
    }
}
