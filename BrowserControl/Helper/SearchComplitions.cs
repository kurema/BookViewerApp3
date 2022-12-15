using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kurema.BrowserControl.Helper.SearchComplitions;
public interface ISearchComplition
{
    string APIUrl { get; }
    //IAsyncEnumerable is not available for UWP, yet.
    //https://learn.microsoft.com/dotnet/standard/net-standard?tabs=net-standard-2-1
    Task<IEnumerable<string>> GetComplitions(string word);
}

public static class SearchComplitionManager
{
    private static SemaphoreSlim SemaphoreHttp = new(1, 1);

    public class SearchComplitionGoogle : ISearchComplition
    {
        public string APIUrl => "http://www.google.com/complete/search?hl={1}&output=toolbar&q={0}";

        public async Task<IEnumerable<string>> GetComplitions(string word)
        {
            await SemaphoreHttp.WaitAsync();
            var result = new List<string>();
            try
            {
            }
            catch
            {

            }
            finally
            {
                SemaphoreHttp.Release();
            }
            return result.ToArray();
        }
    }

}

