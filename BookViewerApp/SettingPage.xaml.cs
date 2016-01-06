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

using System.ComponentModel;
using System.Collections;


// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            this.InitializeComponent();

            var src = new List<SettingViewModel>(SettingStorage.SettingInstances.Count());
            foreach(var item in SettingStorage.SettingInstances)
            {
                src.Add(new SettingViewModel(item));
            }
            this.SettingPanel.ItemsSource = src;
        }

        public class SettingViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }

            private SettingStorage.SettingInstance target;

            public SettingViewModel(SettingStorage.SettingInstance instance)
            {
                target = instance;
            }

            public Type Type
            {
                get
                {
                    return target.GetGenericType();
                }
            }

            public string Title
            {
                get
                {
                    var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    return rl.GetString(target.StringResourceKey + "/Title");
                }
            }

            public string Description
            {
                get
                {
                    var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    return rl.GetString(target.StringResourceKey + "/Description");
                }
            }

            public string ValidRangeDescription
            {
                get
                {
                    var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    return rl.GetString(target.StringResourceKey + "/ValidRangeDescription");
                }
            }

            public object Value
            {
                get
                {
                    return target.GetValueAsString();
                }
                set
                {
                    target.SetValue(value);
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            base.OnNavigatedFrom(e);
        }

        private void CurrentView_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = Frame.CanGoBack ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed; currentView.BackRequested += CurrentView_BackRequested;
            currentView.BackRequested += CurrentView_BackRequested;
        }

    }

    namespace TemplateSelectors
    {
        public sealed class SettingTemplateSelector : DataTemplateSelector
        {
            public DataTemplate TemplateBool { get; set; }
            public DataTemplate TemplateInt { get; set; }
            public DataTemplate TemplateDouble { get; set; }
            public DataTemplate TemplateString { get; set; }
            public DataTemplate TemplateGeneralString { get; set; }

            protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            {
                if (!(item is SettingPage.SettingViewModel))
                {
                    return base.SelectTemplateCore(item, container);
                }
                var itemVM = item as SettingPage.SettingViewModel;
                if(itemVM.Type == typeof(bool))
                {
                    return TemplateBool;
                }else if (itemVM.Type == typeof(int))
                {
                    return TemplateInt;
                }
                else if (itemVM.Type == typeof(double))
                {
                    return TemplateDouble;
                }
                else if (itemVM.Type == typeof(String))
                {
                    return TemplateString;
                }
                else
                {
                    return TemplateGeneralString;
                }
            }
        }
    }
}
