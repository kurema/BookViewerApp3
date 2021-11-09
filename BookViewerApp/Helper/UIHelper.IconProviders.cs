using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using kurema.FileExplorerControl.Models.FileItems;
using System.Threading;
using Windows.UI.Xaml.Media;


namespace BookViewerApp.Helper;

public static partial class UIHelper
{
    public static class IconProviderHelper
    {
        public static (Func<ImageSource> Small, Func<ImageSource> Large) BookmarkIconsExplorer()
        {
            var bmps = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_bookmark_s.png"));
            var bmpl = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///res/Icon/icon_bookmark_l.png"));

            //async void LoadFavicon()
            //{
            //    //try
            //    {
            //        var image = await Managers.FaviconManager.GetMaximumIcon(bookmark.TargetUrl);
            //        if (image is null) return;
            //        var png = Functions.GetPngStreamFromImageMagick(image);
            //        if (png is null) return;
            //        var png2 = new MemoryStream();
            //        await png.CopyToAsync(png2);
            //        png.Seek(0, SeekOrigin.Begin);
            //        png2.Seek(0, SeekOrigin.Begin);
            //        await frame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //        {
            //            await bmps.SetSourceAsync(png.AsRandomAccessStream());
            //            await bmpl.SetSourceAsync(png2.AsRandomAccessStream());
            //        });
            //        image.Dispose();
            //    }
            //    //catch
            //    //{
            //    //}
            //}
            //if ((bool)Storages.SettingStorage.GetValue("ShowBookmarkFavicon")) LoadFavicon();

            return (() => bmps, () => bmpl);
        }

        public static Task<(Func<ImageSource> Small, Func<ImageSource> Large)> BookIconsExplorer(IFileItem file, CancellationToken cancel, Windows.UI.Core.CoreDispatcher dispatcher)
        {
            return BookIcons(file, cancel, dispatcher, new Uri("ms-appx:///res/Icon/icon_book_s.png"), new Uri("ms-appx:///res/Icon/icon_book_l.png"));
        }

        public static Task<(Func<ImageSource> Small, Func<ImageSource> Large)> BookIconsBookshelf(IFileItem file, CancellationToken cancel, Windows.UI.Core.CoreDispatcher dispatcher)
        {
            //ToDo: Add book cover and fix this.
            return BookIcons(file, cancel, dispatcher, new Uri("ms-appx:///res/cover/shincho.png"), new Uri("ms-appx:///res/cover/shincho.png"));
        }


        public async static Task<(Func<ImageSource> Small, Func<ImageSource> Large)> BookIcons(IFileItem file, CancellationToken cancel, Windows.UI.Core.CoreDispatcher dispatcher, Uri smallIcon, Uri largeIcon)
        {
            string id =
            (file as HistoryMRUItem)?.Id ??
            //(a as kurema.FileExplorerControl.Models.FileItems.HistoryItem)?.Content?.Id ??
            Storages.PathStorage.GetIdFromPath(file.Path);
            if (!string.IsNullOrEmpty(id))
            {
                var image = await Managers.ThumbnailManager.GetImageSourceAsync(id);
                if (image != null)
                {
                    return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(smallIcon),
                    () => image
                    );
                }
            }
            if (file is StorageFileItem storage)
            {
                return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(smallIcon),
                () =>
                {
                    var bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage(largeIcon);
                    if ((bool)Storages.SettingStorage.GetValue("FetchThumbnailsBackground"))
                    {
                        Task.Run(() => Managers.ThumbnailManager.SaveImageAndLoadAsync(storage.Content, dispatcher, bitmap, cancel));
                    }
                    return bitmap;
                }
                );
            }

            return (() => new Windows.UI.Xaml.Media.Imaging.BitmapImage(smallIcon),
            () => new Windows.UI.Xaml.Media.Imaging.BitmapImage(largeIcon));
        }
    }
}
