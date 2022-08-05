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
using System.Threading.Tasks;

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

            if (SettingStorage.SettingInstances is null) return;

            var src = new List<SettingViewModel>(SettingStorage.SettingInstances.Length);
            foreach (var item in SettingStorage.SettingInstances.Where(a => a.IsVisible))
            {
                src.Add(new SettingViewModel(item));
            }
            //this.SettingPanel.ItemsSource = src;
            SettingPanel.SettingSource.Source = src.GroupBy(a => a.Group);

            this.Loaded += SettingPage_Loaded;
        }

        private async void SettingPage_Loaded(object sender, RoutedEventArgs e)
        {
            var collection = new System.Collections.ObjectModel.ObservableCollection<IGrouping<string, ViewModels.ListItemViewModel>>(GetOptionalItems().GroupBy(a => Managers.ResourceManager.Loader.GetString(a.GroupTag)));
            listView.Source = collection;
            var purchased = (await GetPurchaseItems()).GroupBy(a => Managers.ResourceManager.Loader.GetString(a.GroupTag));
            if (purchased.Count() >= 1) collection.Insert(Math.Max(collection.Count - 1, 0), purchased.First());

            {
                StackToc.Children.Clear();
                {
                    StackTocAdd(Managers.ResourceManager.Loader.GetString("Setting/Label"), () => TextBlockSettingHeader, Windows.UI.Text.FontWeights.Bold);
                }
                foreach (var item in SettingPanel.GetGroupsContainer())
                {
                    if (item.Content is not IGrouping<string, SettingPage.SettingViewModel> ig) continue;
                    StackTocAdd(Managers.ResourceManager.Loader.GetString($"Setting/Group/{ig.Key}"), () => item,marginLeft:15);
                }
                foreach (var item in listView.GetGroupsContainer())
                {
                    if (item.Content is not IGrouping<string, ViewModels.ListItemViewModel> ig) continue;
                    StackTocAdd(ig.Key, () => item, Windows.UI.Text.FontWeights.Bold);
                }
                Main_SizeChanged_General(this.ActualWidth);
            }
        }

        private void StackTocAdd(string title, Func<DependencyObject> elementProvider, Windows.UI.Text.FontWeight? fontWeight = null, double marginLeft = 0)
        {
            var lvi = new ListViewItem();
            lvi.Content = new TextBlock() { Text = title, FontWeight = fontWeight ?? Windows.UI.Text.FontWeights.Normal, Margin = new Thickness(marginLeft, 0, 0, 0) };
            lvi.Tapped += (_, _) =>
            {
                if (elementProvider?.Invoke() is not UIElement elementUI) return;
                var transform = TextBlockSettingHeader.TransformToVisual(elementUI);
                var point = transform.TransformPoint(new Point(0, 0));
                ScrollViewerMain.ChangeView(null, -point.Y, null);
            };
            StackToc.Children.Add(lvi);

        }

        public async Task<IEnumerable<ViewModels.ListItemViewModel>> GetPurchaseItems()
        {
            var loader = Managers.ResourceManager.Loader;
            try
            {
                await Managers.LicenseManager.Initialize();
                var listingInformation = await Managers.LicenseManager.GetListingAsync();


                return listingInformation.ProductListings
                    //I know it's not a good way. But there seems to be no way to delete addon on Partner Center.
                    //https://docs.microsoft.com/en-us/windows/uwp/monetize/delete-an-add-on
                    //Azire AD... no thank you.
                    //https://twitter.com/juju4ka_/status/890359560237666304
                    .Where(a => a.Value.ProductId != "free_addon")
                    .Select(a => new ViewModels.ListItemViewModel(
                    a.Value.Name ?? "", $" {a.Value.FormattedPrice ?? ""} {(Managers.LicenseManager.IsActive(a.Value) ? loader.GetString("Info/Purchase/Word/Purchased") : "")}"
                    , new DelegateCommand(async _ => await UIHelper.RequestPurchaseAsync(a.Value)
                          )
                    )
                    { GroupTag = "Info/Purchase/Title" });
            }
            catch
            {
                return new ViewModels.ListItemViewModel[0];
            }
        }

        public ViewModels.ListItemViewModel[] GetOptionalItems()
        {

            var tabpage = UIHelper.GetCurrentTabPage(this);
            var loader = Managers.ResourceManager.Loader;
            //Info.Info.Translation

            var result = new ViewModels.ListItemViewModel[] {
                new ViewModels.ListItemViewModel(loader.GetString("AppName"), loader.GetString("Info/Info/AboutThisApp/Description"),new DelegateCommand(async _=>await OpenLicenseContentDialogAboutThisApp())){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/Privacy/Title"), loader.GetString("Info/Info/Privacy/Description"),new OpenWebCommand(tabpage,"https://github.com/kurema/BookViewerApp3/blob/master/PrivacyPolicy.md")){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/ThirdParty/Title"), loader.GetString("Info/Info/ThirdParty/Description"),new DelegateCommand(async _=>await OpenLicenseContentDialogThirdParty())){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/Contributors"), loader.GetString("Info/Info/ShowContributors/Description"),new DelegateCommand(async _=>await OpenLicenseContentDialogContributors())){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/BeSponsor/Title"), loader.GetString("Info/Info/BeSponsor/Description"),new OpenWebCommand(tabpage,"https://github.com/sponsors/kurema/")){ GroupTag="Info/Info/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Info/ReleaseNotes/Title"), string.Format(loader.GetString("Info/Info/ReleaseNotes/Description"),VersionText),new OpenWebCommand(tabpage,"https://github.com/kurema/BookViewerApp3/releases")){ GroupTag="Info/Info/Title"},
            }.ToList();

            result.AddRange(new[] {
                new ViewModels.ListItemViewModel(loader.GetString("Info/Debug/OpenAppData/Title"), loader.GetString("Info/Debug/OpenAppData/Description"),new DelegateCommand(async _=>await Windows.System.Launcher.LaunchFolderAsync(Windows.Storage.ApplicationData.Current.LocalFolder)) ){ GroupTag="Info/Debug/Title"},
                new ViewModels.ListItemViewModel(loader.GetString("Info/Debug/DeleteThumbnails/Title"), loader.GetString("Info/Debug/DeleteThumbnails/Description"),
                    new DelegateCommand(async _=>await DeleteThumbnail())){ GroupTag="Info/Debug/Title"},
            });
#if DEBUG
            result.Add(new ViewModels.ListItemViewModel(loader.GetString("Info/Debug/CopyFAL/Title"), loader.GetString("Info/Debug/CopyFAL/Description"), new DelegateCommand(async _ => await CopyFutureAccessListToClipboard())) { GroupTag = "Info/Debug/Title" });
#endif
            return result.ToArray();
        }

        private async Task DeleteThumbnail()
        {
            var loader = Managers.ResourceManager.Loader;
            var dlg = new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Debug/DeleteThumbnails/Dialog/Message"), loader.GetString("Info/Debug/DeleteThumbnails/Title"));
            dlg.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Word/OK")) { Id = "ok" });
            dlg.Commands.Add(new Windows.UI.Popups.UICommand(loader.GetString("Word/Cancel")) { Id = "cancel" });
            Windows.UI.Popups.IUICommand result;
            try
            {
                result = await dlg.ShowAsync();
            }
            catch
            {
                return;
            }
            if (result.Id?.ToString() == "ok")
            {
                try
                {
                    await Managers.ThumbnailManager.DeleteAllAsync();
                }
                catch
                {
                    await new Windows.UI.Popups.MessageDialog(loader.GetString("Info/Debug/DeleteThumbnails/Dialog/MessageFail"), loader.GetString("Info/Debug/DeleteThumbnails/Title")).ShowAsync();
                }
            }
        }

        public string VersionText
        {
            get
            {
                if (Windows.ApplicationModel.Package.Current?.Id?.Version is { } ver)
                {
                    return string.Format("v{0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build, ver.Revision);
                }
                else
                {
                    return "";
                }
            }
        }

        public async Task CopyFutureAccessListToClipboard()
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

        public async Task OpenLicenseContentDialogThirdParty()
        {
            var license = await LicenseStorage.LocalLicense.GetContentAsync();
            await OpenLicenseContentDialog(license.thirdparty.GroupBy(a => Managers.ResourceManager.Loader.GetString("Info/Info/ThirdParty/ThirdParty")));
        }

        public async Task OpenLicenseContentDialogContributors()
        {
            var license = await LicenseStorage.LocalLicense.GetContentAsync();

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

        public async Task OpenLicenseContentDialogAboutThisApp()
        {
            var license = await LicenseStorage.LocalLicense.GetContentAsync();

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

        public async Task OpenLicenseContentDialog(object source)
        {
            if (source is null) return;

            var dialog = new ContentDialog
            {
                CloseButtonText = Managers.ResourceManager.Loader.GetString("Word/OK"),
                XamlRoot = this.XamlRoot,
            };
            var control = new LicenseControl
            {
                Source = source,
                OpenWebCommand = new DelegateCommand(async address =>
                {
                    await UIHelper.GetCurrentTabPage(this)?.OpenTabWebPreferedBrowser(address?.ToString());
                })
            };
            dialog.Content = control;
            try
            {
                await dialog.ShowAsync();
            }
            catch { }
        }

        public class SettingEnumItemViewModel
        {
            public SettingEnumItemViewModel(Enum content, string stringResourceKey)
            {
                Content = content ?? throw new ArgumentNullException(nameof(content));
                StringResourceKey = stringResourceKey ?? throw new ArgumentNullException(nameof(stringResourceKey));
            }

            public Enum Content { get; private set; }

            public string StringResourceKey { get; private set; }

            public string Title
            {
                get
                {
                    var result = Managers.ResourceManager.Loader.GetString($"{StringResourceKey}/{Name}/Title");
                    return string.IsNullOrEmpty(result) ? Name : result;
                }
            }

            public string Name
            {
                get
                {
                    try
                    {
                        return Content?.GetType()?.GetEnumName(Content) ?? "";
                    }
                    catch
                    {
                        return "";
                    }
                }
            }
        }

        public class SettingViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            private readonly SettingStorage.SettingInstance target;

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

            public string ToolTip
            {
                get
                {
                    var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    //https://docs.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest#load-a-string-for-a-specific-language-or-other-context
                    var resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
                    resourceContext.QualifierValues["Language"] = "en-US";
                    var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
                    return string.Format(rl.GetString("Setting/ToolTip/Message"),
                        resourceMap.GetValue(target.StringResourceKey + "/Title", resourceContext).ValueAsString,
                        resourceMap.GetValue(target.StringResourceKey + "/Description", resourceContext).ValueAsString,
                        target.DefaultValue?.ToString() ?? "null");
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

            public string EnumPlaceholderText
            {
                get
                {
                    var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
                    return rl.GetString(target.StringResourceKey + "/Placeholder");
                }
            }

            SettingEnumItemViewModel[] _EnumItems = null;

            public SettingEnumItemViewModel[] EnumItems
            {
                get
                {
                    if (_EnumItems != null) return _EnumItems;
                    var type = Type;
                    if (!type.IsEnum) return new SettingEnumItemViewModel[0];

                    var result = new List<SettingEnumItemViewModel>();
                    foreach (Enum item in type.GetEnumValues())
                    {
                        result.Add(GetEnumViewModel(item, type.Name));
                    }

                    return _EnumItems = result.ToArray();
                }
            }

            SettingEnumItemViewModel GetEnumViewModel(Enum item, string typeName) => new(item, target.StringResourceKey + "/Enums/" + typeName);

            public SettingEnumItemViewModel SelectedEnum
            {
                get
                {
                    try
                    {
                        if (target.GetValue() is Enum @enum) return EnumItems.First(a => a.Content.Equals(@enum));
                    }
                    catch
                    {
                    }
                    return null;
                }
                set
                {
                    target.SetValue(value.Content);
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
            //var currentView = Windows.UI.Core.SystemNavigationManager.GetForCurrentView();
            //currentView.AppViewBackButtonVisibility = Frame?.CanGoBack == true ? Windows.UI.Core.AppViewBackButtonVisibility.Visible : Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            //currentView.BackRequested += CurrentView_BackRequested;

            UIHelper.SetTitleByResource(this, "Setting");
        }

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Main_SizeChanged_General(e.NewSize.Width);
        }

        private void Main_SizeChanged_General(double width)
        {
            if (width >= 800 + 150 * 2)
            {
                StackToc.Visibility = Visibility.Visible;
                StackToc.Width = (width - SettingPanel.ActualWidth) / 2.0 - StackToc.Margin.Left - StackToc.Margin.Right;
            }
            else
            {
                StackToc.Visibility = Visibility.Collapsed;
            }

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
            public DataTemplate TemplateEnum { get; set; }

            protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
            {
                if (item is not SettingPage.SettingViewModel itemVM) return base.SelectTemplateCore(item, container);
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
                else if (itemVM.Type.IsEnum)
                {
                    return TemplateEnum;
                }
                else
                {
                    return TemplateGeneralString;
                }
            }
        }
    }
}
