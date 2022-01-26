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

namespace kurema.FileExplorerControl.Views;
public sealed partial class RenameRegexControl : UserControl
{
    Dictionary<ViewModels.RenameRegexViewModel.NameTargetType, string> NameTargets { get; } = new();

    ShortcutEntry[] ShortcutsRegEx { get; }
    ShortcutEntry[] ShortcutsWords { get; }

    public RenameRegexControl()
    {
        NameTargets = new()
        {
            { ViewModels.RenameRegexViewModel.NameTargetType.Full, "Filename + extension" },
            { ViewModels.RenameRegexViewModel.NameTargetType.FilenameOnly, "Filename only" },
            { ViewModels.RenameRegexViewModel.NameTargetType.ExtensionOnly, "Extension only" },
        };

        ShortcutsRegEx = new ShortcutEntry[]
        {
            new(".","Mathes any letter."),
            new(@"\d","Metches any number."),
        };

        ShortcutsWords = new ShortcutEntry[]
        {
            new("$d","day")
        };

        this.InitializeComponent();
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    public class ShortcutEntry
    {
        public ShortcutEntry(string code, string description)
        {
            Code = code;
            Description = description;
        }

        public string Code { get; }
        public string Description { get; }
    }
}
