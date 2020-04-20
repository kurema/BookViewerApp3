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
    public sealed partial class MenuMenuCommand : UserControl
    {
        public MenuMenuCommand()
        {
            this.InitializeComponent();

            RegisterPropertyChangedCallback(MenuCommandsProperty, (a, b) =>
            {
                stack.Children.Clear();
                if (MenuCommands == null) return;
                foreach (var item in MenuCommands)
                {
                    stack.Children.Add(GetMenu(item));
                }
            });
        }

        public Models.MenuCommand[] MenuCommands
        {
            get { return (Models.MenuCommand[])GetValue(MenuCommandsProperty); }
            set
            {
                SetValue(MenuCommandsProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for MenuCommands.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuCommandsProperty =
            DependencyProperty.Register("MenuCommands", typeof(Models.MenuCommand[]), typeof(MenuMenuCommand), new PropertyMetadata(new Models.MenuCommand[0]));

        public static MenuFlyoutItemBase GetMenu(Models.MenuCommand menu)
        {
            if (menu.HasChild)
            {
                var result = new MenuFlyoutSubItem()
                {
                    Text = menu.Title,
                };
                foreach(var item in menu.Items)
                {
                    result.Items.Add(GetMenu(item));
                }
                return result;
            }
            else
            {
                return new MenuFlyoutItem()
                {
                    Text=menu.Title,
                    Command=menu.Command
                };
            }
        }
    }
}
