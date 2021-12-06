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
        //Size of ContentDialog has a problem. Not my fault.
        var dialog = new ContentDialog()
        {
            Content = new Views.RenamePage(),
            FullSizeDesired = true,
        };
        {
            var loader = Application.ResourceLoader.Loader;
            dialog.CloseButtonText = loader.GetString("Command/OK");
        }
        await dialog.ShowAsync();
    }
}
