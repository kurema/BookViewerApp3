﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace kurema.BrowserControl.Helper.SearchComplitions;

public static class SearchComplitionManager
{
    private static SemaphoreSlim semaphoreHttp = new(1, 1);
    private static Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
    public static Dictionary<string, IEnumerable<string>> Cache { get; } = new Dictionary<string, IEnumerable<string>>();
    public static void Clear() { Cache.Clear(); }
    public static object? CacheServiceId { get; set; }

    //new() each time is not good. Event though with no field.
    public static SearchComplitionGoogle SearchComplitionGoogleSingle { get; } = new();
    public static SearchComplitionDummy SearchComplitionDummySingle { get; } = new();
    public static SearchComplitionYahoo SearchComplitionYahooSingle { get; } = new();
    public static SearchComplitionBing SearchComplitionBingSingle { get; } = new();

    public static async Task<IEnumerable<string>> UseCacheOrGet(object id, string term, Func<string,Task<IEnumerable<string>>> func)
    {
        if (CacheServiceId != id) { Cache.Clear(); CacheServiceId = id; }
        else if (Cache.TryGetValue(term, out var r)) return r;
        if (func is null) return Array.Empty<string>();
        var result = await func.Invoke(term);
        if (result is null) return Array.Empty<string>();
        if (!result.Any()) return result;
        //Dictionary<>.First() is undefined. But somthing is returned.
        try { if (Cache.Count > 1000) Cache.Remove(Cache.First().Key); } catch { }
        Cache.Add(term, result);
        return result;
    }

    public static async Task<IEnumerable<string>> UseCacheOrGet(string term)
    {
        var complition = DefaultSearchComplition;
        if (complition is null) return Array.Empty<string>();
        return await UseCacheOrGet(complition.APIUrl, term, async t => await complition.GetComplitions(t));
    }

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

    //You can use DI. Or just inject using Func<>. This is easier.
    public static Func<ISearchComplition>? DefaultSearchComplitionProvider { get; set; }
    public static ISearchComplition DefaultSearchComplition => DefaultSearchComplitionProvider?.Invoke() ?? new SearchComplitionDummy();


    public class SearchComplitionDummy : ISearchComplition
    {
        public string APIUrl => string.Empty;

        public Task<IEnumerable<string>> GetComplitions(string word, CultureInfo? culture = null)
        {
            return Task.FromResult((IEnumerable<string>)Array.Empty<string>());
        }
    }

    public class SearchComplitionGoogle : ISearchComplition
    {
        public string APIUrl => "http://www.google.com/complete/search?hl={1}&output=toolbar&q={0}";

        public async Task<IEnumerable<string>> GetComplitions(string word, CultureInfo? culture = null)
        {
            try
            {
                culture ??= CultureInfo.CurrentCulture;
                var lang = culture?.TwoLetterISOLanguageName ?? "en";
                var response = await GetHttp(string.Format(APIUrl, System.Web.HttpUtility.UrlEncode(word), lang));
                if (string.IsNullOrWhiteSpace(response)) return Array.Empty<string>();
                var result = new List<string>();

                var xdoc = new System.Xml.XmlDocument();
                xdoc.LoadXml(response);
                //foreach (var item in xdoc.DocumentElement.ChildNodes)
                //{
                //    if (item is not System.Xml.XmlElement itemXe) continue;
                //    if (!itemXe.Name.Equals("CompleteSuggestion", StringComparison.InvariantCultureIgnoreCase)) continue;
                //    if (!itemXe.FirstChild.Name.Equals("suggestion", StringComparison.InvariantCultureIgnoreCase)) continue;
                //    if (itemXe.FirstChild.Attributes.Count != 1) continue;
                //    result.Add(itemXe.FirstChild.Attributes["data"].Value);
                //}

                var nodelist = xdoc.SelectNodes("/toplevel/CompleteSuggestion/suggestion");
                foreach (var item in nodelist)
                {
                    if (item is not System.Xml.XmlElement itemXe) continue;
                    if (!itemXe.HasAttribute("data")) continue;
                    result.Add(itemXe.GetAttribute("data"));
                }
                return result.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    public class SearchComplitionYahoo : ISearchComplition
    {
        public string APIUrl => "http://ff.search.yahoo.com/gossip?output=xml&command={0}";

        public async Task<IEnumerable<string>> GetComplitions(string word, CultureInfo? culture = null)
        {
            try
            {
                var response = await GetHttp(string.Format(APIUrl, System.Web.HttpUtility.UrlEncode(word)));
                if (string.IsNullOrWhiteSpace(response)) return Array.Empty<string>();
                var result = new List<string>();

                var xdoc = new System.Xml.XmlDocument();
                xdoc.LoadXml(response);
                var nodelist = xdoc.SelectNodes("/m/s");
                foreach (var item in nodelist)
                {
                    if (item is not System.Xml.XmlElement itemXe) continue;
                    if (!itemXe.HasAttribute("k")) continue;
                    result.Add(itemXe.GetAttribute("k"));
                }
                return result.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    public class SearchComplitionBing : ISearchComplition
    {
        public string APIUrl => "https://api.bing.com/osjson.aspx?market={1}&query={0}";

        public async Task<IEnumerable<string>> GetComplitions(string word, CultureInfo? culture = null)
        {
            try
            {
                culture ??= CultureInfo.CurrentCulture;
                var lang = culture?.Name ?? "en-us"; //such as "en-us".
                var response = await GetHttp(string.Format(APIUrl, System.Web.HttpUtility.UrlEncode(word), lang));
                if (string.IsNullOrWhiteSpace(response)) return Array.Empty<string>();
                var result = new List<string>();

                var document = System.Text.Json.JsonDocument.Parse(response);
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind is System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind is System.Text.Json.JsonValueKind.String)
                            {
                                var text = item.GetString();
                                if (text is not null) result.Add(text);
                            }
                        }
                        break;
                    }
                }
                return result.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}

