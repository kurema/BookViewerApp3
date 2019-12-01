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

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();

            var vm = new ViewModels.HomeViewModel() {
                SettingItem = new ViewModels.BookShelf2ViewModelMenuItem()
                {
                    Action = (a) => { FrameNavigate(typeof(SettingPage), null); }
                }
            };
            vm.MenuItems.Add(new ViewModels.BookShelf2ViewModelMenuItem()
            {
                Title = "History",
                Icon = new FontIcon()
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Glyph = "\xE81C",
                },
                Action = (a) =>
                  { }
            });
            vm.MenuItems.Add(new ViewModels.BookShelf2ViewModelMenuItem()
            {
                Title = "Library",
                Icon = new SymbolIcon(Symbol.Library),
                Action = (a) =>
                { }
            });
            this.DataContext = vm;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                var vm = this.DataContext as ViewModels.HomeViewModel;
                //if (vm.SelectedItem != null) vm?.OpenSetting();
                vm?.OpenSetting();
            }
        }

        public void FrameNavigate(Type type,object parameter)
        {
            frame?.Navigate(type, parameter);
        }
    }
}
