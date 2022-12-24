using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable
namespace kurema.BrowserControl.Helper.SearchComplitions;
public interface ISearchComplition
{
    /// <summary>
    /// Url of the API. This should be unique.
    /// </summary>
    string APIUrl { get; }
    //IAsyncEnumerable is not available for UWP, yet.
    //https://learn.microsoft.com/dotnet/standard/net-standard?tabs=net-standard-2-1
    Task<IEnumerable<string>> GetComplitions(string word, System.Globalization.CultureInfo? culture = null);
}

//Easy choice for config. If another service is added on the app, this doesn't make sense.
public enum SearchComplitionOptions
{
    Dummy, Google, Yahoo, Bing,
}

