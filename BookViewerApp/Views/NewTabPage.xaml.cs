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

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using BookViewerApp.Views;
using kurema.FileExplorerControl.Models.IconProviders;
using kurema.FileExplorerControl.Models.FileItems;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NewTabPage : Page
    {
        public NewTabPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Helper.UIHelper.SetTitleByResource(this, "NewTab");

            base.OnNavigatedTo(e);
        }


        private void Button_Click_GoToSetting(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }

        private void Button_Click_GoToBrowser(object sender, RoutedEventArgs e)
        {
            UIHelper.SetTitleByResource(this, "Browser");

            string homepage = "https://www.google.com/";
            if (SettingStorage.GetValue("WebHomePage") is string h)
            {
                homepage = h;
            }

            void OpenBrowser(TabPage tabPage)
            {
                var item = (this.Parent as Frame)?.Parent as Microsoft.UI.Xaml.Controls.TabViewItem;
                UIHelper.FrameOperation.OpenBrowser(this.Frame, homepage, (a) => { tabPage.OpenTabWeb(a); }, (b) => { tabPage.OpenTabBook(b); }, (c) =>
                {
                    {
                        if (item != null) item.Header = c;
                    }
                });
            }

            var tab = UIHelper.GetCurrentTabPage(this);
            if (tab != null)
            {
                OpenBrowser(tab);
            }
            else
            {
                //ToDo: This will not be used. But set correct Action.
                UIHelper.FrameOperation.OpenBrowser(this.Frame, homepage, (a) => { }, (b) => { }, (c) => { });
            }
        }

        private async void Button_Click_PickBook(object sender, RoutedEventArgs e)
        {
            var file = await BookManager.PickFile();
            UIHelper.FrameOperation.OpenBook(file, this.Frame, this);
        }

        private async void Button_Click_GoToExplorer(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                UIHelper.SetTitleByResource(this, "Explorer");
                this.Frame.Navigate(typeof(kurema.FileExplorerControl.Views.FileExplorerPage), null);
                if(this.Frame.Content is kurema.FileExplorerControl.Views.FileExplorerPage content)
                {
                    if (content.Content is kurema.FileExplorerControl.Views.FileExplorerControl control)
                    {
                        //{
                        //    control.MenuChildrens.Add(new AcrylicButtonControl()
                        //    {
                        //        Icon = new SymbolIcon(Symbol.Folder)
                        //    });
                        //}
                        control.MenuChildrens.Add(new ExplorerMenuControl());

                        var fv = new kurema.FileExplorerControl.ViewModels.FileItemViewModel(new StorageFileItem(folder));
                        fv.IconProviders.Add(new IconProviderDelegate((a) => {
                            if (BookManager.AvailableExtensionsArchive.Contains(System.IO.Path.GetExtension(a.Name).ToLower()))
                            {
                                return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_book_s.png")),
                                () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_book_l.png"))
                                );
                            }
                            else { return (null, null); }
                        }));
                        control.SetTreeViewItem(fv);
                        await control.ContentControl.SetFolder(fv);
                        control.ContentControl.FileOpenedEventHandler += (s2,e2) =>
                        {
                                
                            var fileitem = (e2 as kurema.FileExplorerControl.ViewModels.FileItemViewModel)?.Content;
                            if (!BookManager.AvailableExtensionsArchive.Contains(System.IO.Path.GetExtension(fileitem?.Name ?? "").ToLower()))
                            {
                                return;
                            }

                            var tab = UIHelper.GetCurrentTabPage(this);
                            if (tab != null)
                            {
                                if (fileitem is StorageFileItem sfi)
                                {
                                    tab.OpenTabBook(sfi.Content);
                                }
                                else
                                {
                                    var stream = fileitem?.OpenStreamForReadAsync();
                                    if (stream != null)
                                        tab.OpenTabBook(stream);
                                }
                            }
                        };
                    }
                }
            }
        }
    }
}
