﻿using System;
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
            return GetHash(Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(s, Windows.Security.Cryptography.BinaryStringEncoding.Utf8));
        }

        public static string GetHash(Windows.Storage.Streams.IBuffer buffer)
        {
            var algorithm = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm("SHA1");
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
            using var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            var buffer = new byte[stream.Size];
            var ibuffer = buffer.AsBuffer();
            stream.Seek(0);
            await stream.ReadAsync(ibuffer, (uint)stream.Size, Windows.Storage.Streams.InputStreamOptions.None);
            await fileStream.WriteAsync(ibuffer);
        }

        public static async Task SerializeAsync<T>(T content, Windows.Storage.StorageFolder folder, string fileName, System.Threading.SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                var f = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)).AsStream();
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                serializer.Serialize(s, content);
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

            var propset = new BitmapPropertySet
            {
                { "ImageQuality", new BitmapTypedValue(0.5, Windows.Foundation.PropertyType.Single) }
            };
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
                    using var s = (await f.OpenAsync(Windows.Storage.FileAccessMode.Read)).AsStream();
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    return (serializer.Deserialize(s) as T);
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

        public static void ArrayAdd<T>(ref T[] ts, T t)
        {
            ArrayOperate<T>(ref ts, (list) => list.Add(t));
        }

        public static void ArrayRemove<T>(ref T[] ts, T t)
        {
            ArrayOperate<T>(ref ts, (list) => { if (list.Contains(t)) list.Remove(t); });
        }

        public static void ArrayOperate<T>(ref T[] ts, Action<List<T>> action)
        {
            var list = ts.ToList();
            action?.Invoke(list);
            ts = list.ToArray();
        }

        public static List<T> GetArrayAdded<T>(IEnumerable<T> ts, T t)
        {
            return GetArrayOperated(ts, list => list.Add(t));
        }

        public static List<T> GetArrayRemoved<T>(IEnumerable<T> ts, T t)
        {
            if (ts is null) return null;
            return GetArrayOperated(ts, list => { if (list.Contains(t)) { list.Remove(t); } });
        }


        public static List<T> GetArrayOperated<T>(IEnumerable<T> ts, Action<List<T>> action)
        {
            var list = ts.ToList();
            action?.Invoke(list);
            return list;
        }

        public static IEnumerable<T> SortByArchiveEntry<T>(IEnumerable<T> entries, Func<T,string> titleProvider)
        {
            bool SortCover(T a) => !titleProvider(a).ToUpperInvariant().Contains("COVER");
            NaturalSort.NaturalList SortNatural(T a) => new NaturalSort.NaturalList(titleProvider(a));

            if ((bool)Storages.SettingStorage.GetValue("SortNaturalOrder"))
            {
                if ((bool)Storages.SettingStorage.GetValue("SortCoverComesFirst"))
                {
                    return entries.OrderBy(SortCover).ThenBy(SortNatural);
                }
                else
                {
                    return entries.OrderBy(SortNatural);
                }
            }
            else
            {
                if ((bool)Storages.SettingStorage.GetValue("SortCoverComesFirst"))
                {
                    return entries.OrderBy(SortCover).ThenBy(titleProvider);
                }
                else
                {
                    return entries.OrderBy(titleProvider);
                }
            }
        }

        public static string EscapeFileName(string str)
        {
            foreach (char t in Path.GetInvalidFileNameChars())
            {
                str = str.Replace(t, '_');
            }
            return str;
        }

        public static async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync(byte[] data)
        {
            if (data == null) return null;

            var bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();

            using (var stream = new Windows.Storage.Streams. InMemoryRandomAccessStream())
            {
                using (var writer = new Windows.Storage.Streams.DataWriter(stream))
                {
                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }

                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
            }
            return bitmapImage;
        }

        //public static MemoryStream GetPngStreamFromImageMagick(ImageMagick.IMagickImage image)
        //{
        //    if (image == null) return null;
        //    var stream = new MemoryStream();

        //    image.Write(stream, ImageMagick.MagickFormat.Png);
        //    stream.Seek(0, SeekOrigin.Begin);
        //    return stream;
        //}

    }
}
