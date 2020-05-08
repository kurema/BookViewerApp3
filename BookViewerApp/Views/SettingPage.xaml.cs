﻿using System;
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

using BookViewerApp.Helper;
using BookViewerApp.Storages;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            this.InitializeComponent();

            if (SettingStorage.SettingInstances == null) return;

            var src = new List<SettingViewModel>(SettingStorage.SettingInstances.Length);
            foreach(var item in SettingStorage.SettingInstances)
            {
                src.Add(new SettingViewModel(item));
            }
            //this.SettingPanel.ItemsSource = src;
            settingSource.Source = src.GroupBy(a => a.Group);
        }

        public class SettingViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

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

            public object Minimum => target.Minimum;

            public object Maximum => target.Maximum;

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

            public string Group => target.GroupName;
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
            if (Frame?.CanGoBack==true)
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            //currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            //currentView.BackRequested += CurrentView_BackRequested;

            UIHelper.SetTitleByResource(this, "Setting");
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
            public DataTemplate TemplateRegex { get; set; }
            public DataTemplate TemplateGeneralString { get; set; }

            protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            {
                if (!(item is SettingPage.SettingViewModel))
                {
                    return base.SelectTemplateCore(item, container);
                }
                var itemVM = (SettingPage.SettingViewModel) item;
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
                else if (itemVM.Type == typeof(System.Text.RegularExpressions.Regex))
                {
                    return TemplateRegex;
                }
                else
                {
                    return TemplateGeneralString;
                }
            }
        }
    }
}
