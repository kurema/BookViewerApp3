using BookViewerApp.Storages;
using Microsoft.Toolkit.Uwp.Helpers;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

#nullable enable

namespace BookViewerApp.Managers;
public static class ExtensionAdBlockerManager
{
    //Stay away from GPL!
    //If users download manually, I think it's safe.
    public static StorageContent<Storages.ExtensionAdBlockerItems.items> LocalLists = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/AdBlockList.xml", () => new());

    private async static Task<StorageFolder> GetDataFolderCache() => _DataFolderCache ??= await Helper.Functions.GetSaveFolderLocalCache().CreateFolderAsync("AdBlocker", CreationCollisionOption.OpenIfExists);
    private async static Task<StorageFolder> GetDataFolderLocal() => _DataFolderLocal ??= await Helper.Functions.GetSaveFolderLocal().CreateFolderAsync("AdBlocker", CreationCollisionOption.OpenIfExists);


    private static StorageFolder? _DataFolderCache;
    private static StorageFolder? _DataFolderLocal;

    public const string FileNameWhiteList = "whitelist.txt";
    public const string FileNameUser = "user.txt";
    public const string FileNameDb = "rules.db";

    public static DistillNET.FilterDbCollection? Filter { get; private set; }

    //Make sure the list is sorted and ToUpeerInvariant()ed before use.
    public static List<string> DomainsWhitelist { get; } = new List<string>();
    public const int FiltersCacheSize = 20;
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersCache = new();
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersWhitelistCache = new();
    private static DistillNET.UrlFilter[]? FiltersCacheGlobal = null;
    private static DistillNET.UrlFilter[]? FiltersWhitelistCacheGlobal = null;

    private static HttpClient? _CommonHttpClient;
    public static HttpClient CommonHttpClient => _CommonHttpClient ??= new HttpClient();

    public static async void WebView2WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!(bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled)) return;
        if (Filter is null) return;
        if (!Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri uri)) return;
        if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uriReq)) return;
        if (!(uri.Scheme.ToUpperInvariant() is "HTTPS" or "HTTP")) return;
        var domain = uri.Host.ToUpperInvariant();
        if (DomainsWhitelist.BinarySearch(domain) >= 0) return;
        //YouTube specific ad blocking.
        if (args.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && args.Request.Content is null &&
            (uriReq.Host.EndsWith("youtube.com", StringComparison.InvariantCultureIgnoreCase)
            || uriReq.Host.EndsWith("youtubekids.com", StringComparison.InvariantCultureIgnoreCase)
            || uriReq.Host.EndsWith("youtube-nocookie.com", StringComparison.InvariantCultureIgnoreCase))
            && (uriReq.LocalPath.Equals("/watch", StringComparison.InvariantCultureIgnoreCase)
            || uriReq.LocalPath.StartsWith("/shorts/", StringComparison.InvariantCultureIgnoreCase)
            || uriReq.LocalPath.Equals("/live", StringComparison.InvariantCultureIgnoreCase)))
        {
            var d = args.GetDeferral();
            var client = CommonHttpClient;
            var message = new HttpRequestMessage(HttpMethod.Get, uriReq);
            message.Headers.Clear();
            foreach (var item in args.Request.Headers) message.Headers.Add(item.Key, item.Value);
            var response = await client.SendRequestAsync(message);
            var input = await response.Content.ReadAsStringAsync();
            Regex.Replace(input, @"var ytInitialPlayerResponse\s*=\s*(\{.+?\});var meta", (a) =>
            {
                var json= System.Text.Json.JsonDocument.Parse(a.Captures[1].Value);
                return a.Value;
            });
            var ms = new InMemoryRandomAccessStream();
            var dw = new DataWriter(ms);
            dw.WriteString(input);
            args.Response = sender.Environment.CreateWebResourceResponse(ms, (int)response.StatusCode, response.ReasonPhrase, response.Headers.ToString());
            d.Complete();
        }
        var headers = new System.Collections.Specialized.NameValueCollection();
        foreach (var item in args.Request.Headers) headers.Add(item.Key, item.Value);
        switch (args.ResourceContext)
        {
            case Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.XmlHttpRequest:
                headers.Add("X-Requested-With", "XmlHttpRequest");
                break;
            case Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.Script:
                headers.Add("Content-Type", "script");
                break;
        }
        if (!IsBlocked(uriReq, uriReq.Host, headers)) return;
        args.Response = sender.Environment.CreateWebResourceResponse(null, 403, "Forbidden", "");
    }

    public static bool IsBlocked(Uri uri, string domain, System.Collections.Specialized.NameValueCollection rawHeaders)
    {
        if (Filter is null) return false;
        FiltersCacheGlobal ??= Filter?.GetFiltersForDomain()?.ToArray();
        FiltersWhitelistCacheGlobal ??= Filter?.GetWhitelistFiltersForDomain()?.ToArray();
        if (FiltersCacheGlobal is null) return false;
        if (FiltersWhitelistCacheGlobal is null) return false;
        DistillNET.UrlFilter[] filters = LoadFilters(domain);
        DistillNET.UrlFilter[] filtersWhite = LoadWhitelistFilters(domain);
        foreach (var item in filtersWhite) if (item.IsMatch(uri, rawHeaders)) return false;
        foreach (var item in FiltersWhitelistCacheGlobal) if (item.IsMatch(uri, rawHeaders)) return false;
        foreach (var item in filters) if (!string.IsNullOrWhiteSpace(item.OriginalRule) && item.IsMatch(uri, rawHeaders)) return true;
        foreach (var item in FiltersCacheGlobal) if (!string.IsNullOrWhiteSpace(item.OriginalRule) && item.IsMatch(uri, rawHeaders)) return true;
        return false;
    }

    public static void ResetCache()
    {
        FiltersCacheGlobal = null;
        FiltersWhitelistCacheGlobal = null;
        FiltersCache.Clear();
        FiltersWhitelistCache.Clear();
    }

    public static DistillNET.UrlFilter[] LoadFilters(string domain) => LoadFiltersCommon(domain, FiltersCache, (domain) => Filter?.GetFiltersForDomain(domain));
    public static DistillNET.UrlFilter[] LoadWhitelistFilters(string domain) => LoadFiltersCommon(domain, FiltersWhitelistCache, (domain) => Filter?.GetWhitelistFiltersForDomain(domain));
    private static DistillNET.UrlFilter[] LoadFiltersCommon(string domain, Dictionary<string, (DateTime, DistillNET.UrlFilter[])> cache, Func<string, IEnumerable<DistillNET.UrlFilter>?> getFiltersForDomain)
    {
        if (cache is null || Filter is null || getFiltersForDomain is null) return new DistillNET.UrlFilter[0];
        if (cache.ContainsKey(domain)) return cache[domain].Item2;
        var filters = getFiltersForDomain(domain)?.ToArray() ?? new DistillNET.UrlFilter[0];
        cache.Add(domain, (DateTime.Now, filters));
        if (cache.Count > FiltersCacheSize) cache.Remove(cache.OrderBy(a => a.Value).First().Key);
        return filters;
    }

    public static async Task<DistillNET.FilterDbCollection?> LoadRules()
    {
        return await LoadRulesFromDb() ?? (await LoadRulesFromText()).result;
    }

    public static async Task<DistillNET.FilterDbCollection?> LoadRulesFromDb()
    {
        var folder = await GetDataFolderCache();
        if (!await folder.FileExistsAsync(FileNameDb)) return null;
        try
        {
            return Filter = new DistillNET.FilterDbCollection(Path.Combine(folder.Path, FileNameDb), false, false);
        }
        catch
        {
            return null;
        }
    }

    private static System.Threading.SemaphoreSlim SemaphoreLoadFromText = new(1, 1);

    public static async Task<(DistillNET.FilterDbCollection? result, bool success)> LoadRulesFromText()
    {
        await SemaphoreLoadFromText.WaitAsync();
        try
        {
            bool success = true;
            var folder = await GetDataFolderCache();
            Filter?.Dispose();
            Filter = null;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    Filter = new DistillNET.FilterDbCollection(Path.Combine(folder.Path, FileNameDb), true, false);
                    break;
                }
                catch
                {
                    Filter = null;
                    await Task.Delay(1000);
                }
            }
            if (Filter is null) return (null, false);
            foreach (var file in await folder.GetFilesAsync())
            {
                if (Path.GetFileName(file.Path) is FileNameWhiteList or FileNameUser or FileNameDb) continue;
                if (Path.GetExtension(file.Path).ToUpperInvariant() != ".TXT") continue;
                try
                {
                    var stream = await file.OpenReadAsync();
                    if (stream is null) continue;
                    using var streamNative = stream.AsStream();
                    var result = Filter.ParseStoreRulesFromStream(streamNative, 1);
                }
                catch
                {
                    success = false;
                }
            }
            Filter.FinalizeForRead();
            ResetCache();
            return (Filter, success);
        }
        finally
        {
            SemaphoreLoadFromText.Release();
        }
    }

    public static async Task<bool> TryRemoveList(Storages.ExtensionAdBlockerItems.itemsGroupItem item)
    {
        string? filename = item?.filename;
        if (filename is null) return false;
        try
        {
            var folder = await GetDataFolderCache();
            var file = await folder.GetFileAsync(filename);
            if (!file.IsAvailable) return false;
            //await file.RenameAsync($"{filename}.old", NameCollisionOption.ReplaceExisting);
            await file.DeleteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryDownloadList(Storages.ExtensionAdBlockerItems.itemsGroupItem item)
    {
        if (!Uri.TryCreate(item.source, UriKind.Absolute, out var uri)) return false;

        try
        {
            var folder = await GetDataFolderCache();
            var file = await folder.CreateFileAsync($"{item.filename}.dl", CreationCollisionOption.ReplaceExisting);
            if (file is null) return false;
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            //var wc = new WebClient();
            //wc.DownloadFileAsync(uri, System.IO.Path.Combine(path, item.filename));
            var hc = new HttpClient();
            var result = await hc.TryGetAsync(uri);
            if (!result.Succeeded) return false;
            result.ResponseMessage.EnsureSuccessStatusCode();
            await result.ResponseMessage.Content.WriteToStreamAsync(stream);
            await file.RenameAsync(item.filename, NameCollisionOption.ReplaceExisting);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsItemLoaded(Storages.ExtensionAdBlockerItems.itemsGroupItem item)
    {
        var cache = await GetDataFolderCache();
        return await cache.FileExistsAsync(item.filename);
    }
}
