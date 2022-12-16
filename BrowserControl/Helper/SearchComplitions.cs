using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
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
    private static SemaphoreSlim semaphoreHttp = new(1, 1);
    private static Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

    private static async Task<string> GetHttp(string url)
    {
        await semaphoreHttp.WaitAsync();
        try
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return string.Empty;
            return await httpClient.GetStringAsync(uri);
        }
        catch { return string.Empty; }
        finally { semaphoreHttp.Release(); }
    }

    public class SearchComplitionGoogle : ISearchComplition
    {
        public string APIUrl => "http://www.google.com/complete/search?hl={1}&output=toolbar&q={0}";

        public async Task<IEnumerable<string>> GetComplitions(string word)
        {
            var lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var response = await GetHttp(string.Format(APIUrl, System.Web.HttpUtility.UrlEncode(word), lang));
            if (string.IsNullOrWhiteSpace(response)) return Array.Empty<string>();
            var result = new List<string>();
            try
            {
                var xdoc = new System.Xml.XmlDocument();
                xdoc.LoadXml(response);
                foreach (var item in xdoc.ChildNodes)
                {
                    if (item is not System.Xml.XmlElement itemXe) continue;
                    if (!itemXe.Name.Equals("CompleteSuggestion", StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (!itemXe.FirstChild.Name.Equals("suggestion", StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (itemXe.FirstChild.Attributes.Count != 1) continue;
                    result.Add(itemXe.FirstChild.Attributes[0].Value);
                }

                var nodelist = xdoc.SelectNodes("/CompleteSuggestion/suggestion");
                foreach(var item in nodelist)
                {
                    if (item is not System.Xml.XmlElement itemXe) continue;
                }
            }
            catch
            {
                return Array.Empty<string>();
            }
            return result.ToArray();
        }
    }
}

