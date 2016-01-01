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
    public sealed partial class BookFixedViewerControl : UserControl
    {

        public BookFixedViewerControl()
        {
            this.InitializeComponent();

            InitializeCommands(this.BodyControl);

            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();

            var buttons = new List<AppBarButton>();

            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageTop, Icon = new SymbolIcon(Symbol.Previous), Label = rl.GetString("PageTop") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPagePrevious, Icon = new SymbolIcon(Symbol.Previous),Label=rl.GetString("PagePrevious") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageNext, Icon = new SymbolIcon(Symbol.Next), Label = rl.GetString("PageNext") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandPageLast, Icon = new SymbolIcon(Symbol.Next), Label = rl.GetString("PageEnd") });
            CommandBarMain.PrimaryCommands.Add(new AppBarButton() { Command = CommandSelectFile, Icon = new SymbolIcon(Symbol.OpenLocal), Label = rl.GetString("OpenLocalBook") });

            CommandBarMain.SecondaryCommands.Add(new AppBarButton() {Command=CommandPageReverse,Icon=new SymbolIcon(Symbol.Switch) ,Label= rl.GetString("PageReverse") });

            BodyControl.LoadLastReadPageAsDefault = true;

            //I know I should use Binding.
            BodyControl.SelectedPageChanged += (s, e) => { this.TextBoxSelectedPage.Text = BodyControl.SelectedPage.ToString(); UpdateProgressBar(); };
            BodyControl.PageCountChanged += (s, e) => { this.TextBlockPageCount.Text = BodyControl.PageCount.ToString(); UpdateProgressBar(); };
        }

        public void UpdateProgressBar()
        {
            ProgressBarMain.Value = BodyControl.PageCount <= 0 ? 0 : (double)BodyControl.SelectedPage / (double)BodyControl.PageCount * 100.0;
            if (BodyControl.Reversed)
            {
                ProgressBarMain.RenderTransform = new CompositeTransform() { ScaleX = -1 };
            }
            else
            {
                ProgressBarMain.RenderTransform = null;
            }
        }

        public System.Windows.Input.ICommand CommandPageNext;
        public System.Windows.Input.ICommand CommandPagePrevious;
        public System.Windows.Input.ICommand CommandSelectFile;
        public System.Windows.Input.ICommand CommandPageTop;
        public System.Windows.Input.ICommand CommandPageLast;
        public System.Windows.Input.ICommand CommandPageReverse;

        public void Open(Windows.Storage.IStorageFile file)
        {
            this.BodyControl.Open(file);
        }

        public void InitializeCommands(BookFixedViewerBodyControl Target)
        {
            CommandPageNext = new BookFixedViewerBodyControl.CommandAddPage(Target, 1);
            CommandPagePrevious = new BookFixedViewerBodyControl.CommandAddPage(Target, -1);
            CommandSelectFile = new BookFixedViewerBodyControl.CommandOpenPicker(Target);
            CommandPageTop = new BookFixedViewerBodyControl.CommandSetPage(Target, 1);
            CommandPageLast = new BookFixedViewerBodyControl.CommandLastPage(Target);
            CommandPageReverse = new BookFixedViewerBodyControl.CommandSwapReverse(Target);
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
