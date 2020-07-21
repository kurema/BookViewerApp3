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
            foreach (var item in SettingStorage.SettingInstances.Where(a => a.IsVisible))
            {
                src.Add(new SettingViewModel(item));
            }
            //this.SettingPanel.ItemsSource = src;
            settingSource.Source = src.GroupBy(a => a.Group);


            this.Loaded += SettingPage_Loaded;
        }

        private void SettingPage_Loaded(object sender, RoutedEventArgs e)
        {
            listView.Source = GetOptionalItems().GroupBy(a => Managers.ResourceManager.Loader.GetString(a.GroupTag));
        }

        public ViewModels.ListItemViewModel[] GetOptionalItems()
        {
            var tabpage = UIHelper.GetCurrentTabPage(this);
            var loader = Managers.ResourceManager.Loader;
            //Info.Info.Translation
            return new[] {
                new ViewModels.ListItemViewModel(loader.GetString("AppName"), "",new DelegateCommand(async _=>await OpenLicenseContentDialogAboutThisApp())){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/Privacy/Title"), loader.GetString("Info/Info/Privacy/Description"),new OpenWebCommand(tabpage,"https://github.com/kurema/BookViewerApp3/blob/master/PrivacyPolicy.md")){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/ThirdParty/Title"), "",new DelegateCommand(async _=>await OpenLicenseContentDialogThirdParty())){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/Contributors"), "",new DelegateCommand(async _=>await OpenLicenseContentDialogContributors())){ GroupTag="Info/Info/Title"},

                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/BeSponsor"), "",new OpenWebCommand(tabpage,"https://github.com/sponsors/kurema/")){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Debug/OpenAppData"), "",new DelegateCommand(async _=>await Windows.System.Launcher.LaunchFolderAsync(Windows.Storage.ApplicationData.Current.LocalFolder)) ){ GroupTag="Info/Debug/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Debug/CopyFAL"), "",new DelegateCommand(async _=> await CopyFutureAccessListToClipboard()) ){ GroupTag="Info/Debug/Title"},
            };
        }

        public async System.Threading.Tasks.Task CopyFutureAccessListToClipboard()
        {
            var statistics = LibraryStorage.GetTokenUsedCount();

            var acl = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            var builder = new System.Text.StringBuilder();
            foreach (var item in acl.Entries)
            {
                builder.Append("Token:");
                builder.Append(item.Token);
                builder.AppendLine();

                builder.Append("Metadata:");
                builder.Append(item.Metadata);
                builder.AppendLine();

                if (statistics.ContainsKey(item.Token))
                {
                    builder.Append("Count:");
                    builder.Append(statistics[item.Token]);
                    builder.AppendLine();
                }

                try
                {
                    var storageItem = (await acl.GetItemAsync(item.Token));
                    builder.Append("Path:");
                    builder.Append(storageItem?.Path);
                }
                catch (Exception e)
                {
                    builder.Append("Error:");
                    builder.Append(e.Message);
                }
                builder.AppendLine();

                builder.AppendLine();

                try
                {
                    var data = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    data.SetText(builder.ToString());
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(data);
                }
                catch
                {
                }
            }
        }

        public async System.Threading.Tasks.Task OpenLicenseContentDialogThirdParty()
        {
            var license = await Storages.LicenseStorage.LocalLicense.GetContentAsync();
            await OpenLicenseContentDialog(license.thirdparty.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/ThirdParty/ThirdParty")));
        }

        public async System.Threading.Tasks.Task OpenLicenseContentDialogContributors()
        {
            var license = await Storages.LicenseStorage.LocalLicense.GetContentAsync();

            var groups = new List<IGrouping<string, object>>();
            void Add(IEnumerable<IGrouping<string, object>> item)
            {
                if (item == null || item.Count() == 0) return;
                groups.AddRange(item);
            }

            Add(license?.contributors?.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/Contributors")));
            Add(license?.translations?.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/Translation")));
            Add(license?.sponsors?.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/Sponsors")));
            await OpenLicenseContentDialog(groups);
        }

        public async System.Threading.Tasks.Task OpenLicenseContentDialogAboutThisApp()
        {
            var license = await Storages.LicenseStorage.LocalLicense.GetContentAsync();

            var groups = new List<IGrouping<string, object>>();
            void Add(IEnumerable<IGrouping<string, object>> item)
            {
                if (item == null || item.Count() == 0) return;
                groups.AddRange(item);
            }

            Add(license?.firstparty?.GroupBy(a => Managers.ResourceManager.Loader.GetString("AppName")));
            Add(license?.firstparty?.FirstOrDefault()?.developer?.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/Developers")));
            await OpenLicenseContentDialog(groups);
        }

        public async System.Threading.Tasks.Task OpenLicenseContentDialog(object source)
        {
            if (source == null) return;

            var dialog = new ContentDialog();
            dialog.CloseButtonText = Managers.ResourceManager.Loader.GetString("Word/OK");
            var control = new Views.LicenseControl();
            control.Source = source;
            control.OpenWebCommand = new DelegateCommand(address => UIHelper.GetCurrentTabPage(this)?.OpenTabWeb(address?.ToString()));
            dialog.Content = control;
            await dialog.ShowAsync();
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
            if (Frame?.CanGoBack == true)
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
                var itemVM = (SettingPage.SettingViewModel)item;
                if (itemVM.Type == typeof(bool))
                {
                    return TemplateBool;
                }
                else if (itemVM.Type == typeof(int))
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
