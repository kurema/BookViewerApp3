using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;

namespace BookViewerApp
{
    static class Functions
    {
        public static string GetHash(string s)
        {
            var algorithm = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm("SHA1");
            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(s, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            var hash = algorithm.HashData(buffer);
            return Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(hash);
        }

        public static string CombineStringAndDouble(string str,double value)
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

        public static async Task SaveStreamToFile(Windows.Storage.Streams.IRandomAccessStream stream,Windows.Storage.IStorageFile file)
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

    }
}
