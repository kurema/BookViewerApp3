using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;

using BookViewerApp.Helper;

namespace BookViewerApp.Managers
{
    public static class ThumbnailManager
    {
        private async static Task<Windows.Storage.StorageFolder> GetDataFolder() => await Functions.GetSaveFolderLocalCache().CreateFolderAsync("thumbnail", Windows.Storage.CreationCollisionOption.OpenIfExists);

        private static System.Threading.SemaphoreSlim SemaphoreFetchThumbnail = new System.Threading.SemaphoreSlim(1, 1);

        public const int ThumbnailSize = 600;

        public static async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetImageSourceAsync(string ID)
        {
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            var file = await GetImageFileAsync(ID);
            if (file is null) return null;
            var src = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            if (src is null) return null;
            try
            {
                await result.SetSourceAsync(src);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public static async Task DeleteAllAsync()
        {
            await SemaphoreFetchThumbnail.WaitAsync();
            try
            {
                var f = await GetDataFolder();
                await f.DeleteAsync();
            }
            finally
            {
                SemaphoreFetchThumbnail.Release();
            }
        }

        public static async Task<Windows.Storage.FileProperties.BasicProperties> GetFolderBasicPropertiesAsync()
        {
            var f = await GetDataFolder();
            return await f.GetBasicPropertiesAsync();
        }

        //Not smart at all...
        public static async void SetToImageSourceNoWait(string ID, Windows.UI.Xaml.Media.Imaging.BitmapImage image)
        {
            var file = (await GetImageFileAsync(ID));
            if (file is null) return;
            var src = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            if (src is null) return;
            try
            {
                await image.SetSourceAsync(src);
            }
            catch
            {
                // ignored
            }
        }

        public static async Task SaveImageAndLoadAsync(Windows.Storage.IStorageItem storageItem, Windows.UI.Core.CoreDispatcher dispatcher, Windows.UI.Xaml.Media.Imaging.BitmapImage bitmap
            , System.Threading.CancellationToken cancellationToken = new System.Threading.CancellationToken()
            )
        {
            try
            {
                var book = await ThumbnailManager.SaveImageAsync(storageItem, cancellationToken);
                if (book is null || string.IsNullOrEmpty(book?.ID)) return;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    SetToImageSourceNoWait(book.ID, bitmap);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task<Books.IBook> SaveImageAsync(Windows.Storage.IStorageItem storageItem, System.Threading.CancellationToken cancellationToken = new System.Threading.CancellationToken())
        {
            await SemaphoreFetchThumbnail.WaitAsync();
            try
            {
                if (cancellationToken.IsCancellationRequested) return null;
                if (!(storageItem is Windows.Storage.IStorageFile f)) return null;
                var book = await BookManager.GetBookFromFile(f);
                if (string.IsNullOrEmpty(book.ID)) return book;
                Storages.PathStorage.AddOrReplace(f.Path, book.ID, (book as Books.IBookFixed)?.PageCount);
                await SaveImageAsync(book);
                await Storages.PathStorage.Content.SaveAsync();
                await Task.Delay(1000);
                return book;
            }
            finally
            {
                SemaphoreFetchThumbnail.Release();
            }
        }

        public static async Task SaveImageAsync(Books.IBook book)
        {
            if (book is Books.IBookFixed && (book as Books.IBookFixed).PageCount > 0)
            {
                if (await GetImageFileAsync(book.ID) is null)
                {
                    await (book as Books.IBookFixed).GetPageCover().SaveImageAsync(await CreateImageFileAsync(book.ID), ThumbnailSize);
                }
            }
        }

        public static async Task<Windows.Storage.StorageFile> CreateImageFileAsync(string ID)
        {
            return await (await GetDataFolder()).CreateFileAsync(GetFileNameFromID(ID), Windows.Storage.CreationCollisionOption.ReplaceExisting);
        }

        public static async Task<Windows.Storage.StorageFile> GetImageFileAsync(string ID)
        {
            var item = await (await GetDataFolder()).TryGetItemAsync(GetFileNameFromID(ID));
            if (item != null && item is Windows.Storage.StorageFile sf && (await sf.GetBasicPropertiesAsync()).Size != 0)
            {
                return sf;
            }
            return null;
        }

        public static string GetFileNameFromID(string ID) => "" + Helper.Functions.EscapeFileName(ID) + Extension;

        public static string Extension => ".jpeg";
    }
}
