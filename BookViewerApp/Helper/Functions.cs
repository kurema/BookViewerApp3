using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using Windows.Graphics.Imaging;

namespace BookViewerApp.Helper
{
    public static class Functions
    {
        public static string GetHash(string s)
        {
            var algorithm = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm("SHA1");
            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(s, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            var hash = algorithm.HashData(buffer);
            return Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(hash);
        }

        public static string CombineStringAndDouble(string str, double value)
        {
            return "\"" + str + "\"" + "<" + value.ToString() + "> ";
        }

        public static Windows.Storage.StorageFolder GetSaveFolderRoaming()
        {
            return Windows.Storage.ApplicationData.Current.RoamingFolder;
        }

        public static Windows.Storage.StorageFolder GetSaveFolderLocal()
        {
            return Windows.Storage.ApplicationData.Current.LocalFolder;
        }

        public static Windows.Storage.StorageFolder GetSaveFolderLocalCache()
        {
            return Windows.Storage.ApplicationData.Current.LocalCacheFolder;
        }

        public static async Task SaveStreamToFile(Windows.Storage.Streams.IRandomAccessStream stream, Windows.Storage.IStorageFile file)
        {
            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                var buffer = new byte[stream.Size];
                var ibuffer = buffer.AsBuffer();
                stream.Seek(0);
                await stream.ReadAsync(ibuffer, (uint)stream.Size, Windows.Storage.Streams.InputStreamOptions.None);
                await fileStream.WriteAsync(ibuffer);
            }
        }

        public static async Task SerializeAsync<T>(T content, Windows.Storage.StorageFolder folder, string fileName, System.Threading.SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                var f = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    serializer.Serialize(s, content);
                }
            }
            catch { }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task<T> DeserializeAsync<T>(Windows.Storage.StorageFolder folder, string fileName, System.Threading.SemaphoreSlim semaphore) where T : class
        {
            try
            {
                return await DeserializeAsync<T>((await folder.GetItemAsync(fileName)) as Windows.Storage.StorageFile, semaphore);
            }
            catch
            {
                return null;
            }
        }

        public static async Task ResizeImage(Windows.Storage.Streams.IRandomAccessStream origin, Windows.Storage.Streams.IRandomAccessStream result, uint maxSize, Action extractAction = null)
        {
            var decoder = await BitmapDecoder.CreateAsync(origin);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            double scale = (double)maxSize / Math.Max(decoder.PixelWidth, decoder.PixelHeight);

            var propset = new BitmapPropertySet();
            propset.Add("ImageQuality", new BitmapTypedValue(0.5, Windows.Foundation.PropertyType.Single));
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, result);
            encoder.SetSoftwareBitmap(softwareBitmap);

            if (scale >= 1)
            {
                extractAction?.Invoke();
                return;
            }
            scale = Math.Min(scale, 0.5);
            encoder.BitmapTransform.ScaledWidth = (uint)(decoder.PixelWidth * scale);
            encoder.BitmapTransform.ScaledHeight = (uint)(decoder.PixelHeight * scale);
            encoder.IsThumbnailGenerated = false;
            try
            {
                await encoder.FlushAsync();
            }
            catch
            {
            }
        }

        public static async Task<T> DeserializeAsync<T>(Windows.Storage.StorageFile f, System.Threading.SemaphoreSlim semaphore) where T : class
        {
            await semaphore.WaitAsync();
            try
            {
                if (f != null)
                {
                    using (var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream())
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                        return (serializer.Deserialize(s) as T);
                    }
                }
                else { return null; }
            }
            catch
            {
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static bool IsAncestorOf(string ancestor, string progeny)
        {
            var currentDir = progeny;

            while (true)
            {
                if (Path.GetRelativePath(ancestor, currentDir.ToString()) == ".") return true;
                currentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(currentDir)) return false;
            }
        }
    }
}
