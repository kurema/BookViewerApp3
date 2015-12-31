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


            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();

            var buttons = new List<AppBarButton>();

            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageTop, Icon = new SymbolIcon(Symbol.Previous), Label = rl.GetString("PageTop") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPagePrevious, Icon = new SymbolIcon(Symbol.Previous),Label=rl.GetString("PagePrevious") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageNext, Icon = new SymbolIcon(Symbol.Next), Label = rl.GetString("PageNext") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandSelectFile, Icon = new SymbolIcon(Symbol.OpenLocal), Label = rl.GetString("OpenLocalBook") });

            BodyControl.LoadLastReadPageAsDefault = true;

            //I know I should use Binding.
            BodyControl.SelectedPageChanged += (s, e) => { this.TextBoxSelectedPage.Text = BodyControl.SelectedPage.ToString(); };
            BodyControl.PageCountChanged += (s, e) => { this.TextBlockPageCount.Text = BodyControl.PageCount.ToString(); };

        }

        public System.Windows.Input.ICommand CommandPageNext;
        public System.Windows.Input.ICommand CommandPagePrevious;
        public System.Windows.Input.ICommand CommandSelectFile;
        public System.Windows.Input.ICommand CommandPageTop;

        public void Open(Windows.Storage.IStorageFile file)
        {
            this.BodyControl.Open(file);
        }

        public void InitializeCommands(ControlBookFixedViewerBody Target)
        {
            CommandPageNext = new ControlBookFixedViewerBody.CommandAddPage(Target, 1);
            CommandPagePrevious = new ControlBookFixedViewerBody.CommandAddPage(Target, -1);
            CommandSelectFile = new ControlBookFixedViewerBody.CommandOpenPicker(Target);
            CommandPageTop = new ControlBookFixedViewerBody.CommandSetPage(Target, 0);
        }

        private void TextBoxPageCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            try {
                var pageCount = int.Parse(((TextBox)sender).Text);
                if (BodyControl.CanSelect(pageCount)) BodyControl.SelectedPage = pageCount;
            }
            catch { }
        }
    }
}
