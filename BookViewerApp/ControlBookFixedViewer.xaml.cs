using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp
{
    public sealed partial class ControlBookFixedViewer : UserControl
    {

        public ControlBookFixedViewer()
        {
            this.InitializeComponent();

            InitializeCommands(this.BodyControl);

            var buttons = new List<AppBarButton>();

            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPagePrevious, Icon = new SymbolIcon(Symbol.Previous) });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageNext, Icon = new SymbolIcon(Symbol.Next) });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandSelectFile, Icon = new SymbolIcon(Symbol.OpenLocal) });
        }

        public System.Windows.Input.ICommand CommandPageNext;
        public System.Windows.Input.ICommand CommandPagePrevious;
        public System.Windows.Input.ICommand CommandSelectFile;

        public void InitializeCommands(ControlBookFixedViewerBody Target)
        {
            CommandPageNext = new ControlBookFixedViewerBody.CommandAddPage(Target, 1);
            CommandPagePrevious = new ControlBookFixedViewerBody.CommandAddPage(Target, -1);
            CommandSelectFile = new ControlBookFixedViewerBody.CommandOpen(Target);
        }
    }
}
