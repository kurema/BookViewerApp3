using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;

namespace BookViewerApp
{
    public static class ThumbnailManager
    {
        private static Windows.Storage.StorageFolder DataFolder { get { return Functions.GetSaveFolderLocalCache(); } }

        public static async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetImageSourceAsync(string ID)
        {
            var result= new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            var file = (await GetImageFileAsync(ID));
            if (file == null) return null;
            var src = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            if (src == null) return null;
            try {
                await result.SetSourceAsync(src);
                return result;
            }
            catch
            {
                return null;
            }
        }

        //Not smart at all...
        public async static void SetToImageSourceNoWait(string ID,Windows.UI.Xaml.Media.Imaging.BitmapImage image) {
            var file= (await GetImageFileAsync(ID));
            if (file == null) return;
            var src = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            if (src == null) return;
            try {
                await image.SetSourceAsync(src);
            }
            catch { }
        }

        public static async Task SaveImageAsync(Books.IBook book)
        {
            if(book is Books.IBookFixed && (book as Books.IBookFixed).PageCount>0)
            {
                if (await GetImageFileAsync(book.ID) == null)
                {
                    await (book as Books.IBookFixed).GetPage(0).SaveImageAsync(await CreateImageFileAsync(book.ID), 300);
                }
            }
        }

        public static async Task<Windows.Storage.StorageFile> CreateImageFileAsync(string ID)
        {
            return await DataFolder.CreateFileAsync(GetFileNameFromID(ID));
        }

        public static async Task<Windows.Storage.StorageFile> GetImageFileAsync(string ID)
        {
            var item= await DataFolder.TryGetItemAsync(GetFileNameFromID(ID));
            if(item!=null && item is Windows.Storage.StorageFile)
            {
                return item as Windows.Storage.StorageFile;
            }
            return null;
        }

        public static string GetFileNameFromID(string ID)
        {
            return "Thumbnail_" + EscapeString(ID) + Extension;
        }

        public static string Extension { get { return ".image"; } }

        public static String EscapeString(string str)
        {
            foreach(char t in System.IO.Path.GetInvalidFileNameChars())
            {
                str=str.Replace(t, '_');
            }
            return str;
        }
    }
}
