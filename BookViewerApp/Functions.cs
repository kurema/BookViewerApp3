using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
