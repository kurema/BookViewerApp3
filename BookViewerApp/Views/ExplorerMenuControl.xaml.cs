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

using BookViewerApp.Helper;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace BookViewerApp.Views;

public sealed partial class ExplorerMenuControl : UserControl
{
    public ExplorerMenuControl()
    {
        this.InitializeComponent();

        listView.ItemsSource = new MenuItem[]
        {
                //new MenuItem(Symbol.Folder,"Add Folder",()=>{ }),
                new MenuItem(Symbol.World,"Browser",()=>{
                    var tab=GetTabPage();
                    if(tab==null) return;
                    tab.OpenTabWeb();
                }),
                new MenuItem(Symbol.OpenFile,"PickBook",()=>{
                    var tab=GetTabPage();
                    if(tab==null) return;
                    System.Threading.Tasks.Task.Run(async()=>{await UIHelper.FrameOperation.OpenBookPicked(()=>tab.OpenTab("BookViewer"));});
                }),
                new MenuItem(Symbol.Setting,"Setting",()=>{
                    var tab=GetTabPage();
                    if(tab==null) return;
                    tab.OpenTabSetting();
                }),
        };
    }

    public Page OriginPage { get; set; }

    public TabPage GetTabPage()
    {
        if (OriginPage is null) return null;
        return UIHelper.GetCurrentTabPage(OriginPage);
    }


    public class MenuItem
    {
        public MenuItem(Symbol icon, string title, Action action)
        {
            Icon = icon;
            _ = title ?? throw new ArgumentNullException(nameof(title));
            Title = Managers.ResourceManager.Loader.GetString("Explorer/" + title);
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public Symbol Icon { get; set; }
        public string Title { get; set; }
        public Action Action { get; set; }
    }

    private void listView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is MenuItem item)
        {
            item?.Action?.Invoke();
        }
    }
}
