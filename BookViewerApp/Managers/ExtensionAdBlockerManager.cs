using BookViewerApp.Storages;
using Microsoft.Toolkit.Uwp.Helpers;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using static System.Net.WebRequestMethods;

#nullable enable

namespace BookViewerApp.Managers;
public static class ExtensionAdBlockerManager
{
    //Stay away from GPL!
    //If users download manually, I think it's safe.
    public static StorageContent<Storages.ExtensionAdBlockerItems.items> LocalLists = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/AdBlockList.xml", () => new());

    public static StorageContent<Storages.ExtensionAdBlockerItems.status> LocalInfo = new(SavePlaces.Local, "AdBlocker/status.xml", () => new() { filters = new Storages.ExtensionAdBlockerItems.item[0], selected = new Storages.ExtensionAdBlockerItems.item[0] });

    public async static Task<StorageFolder> GetDataFolderCache() => _DataFolderCache ??= await Helper.Functions.GetSaveFolderLocalCache().CreateFolderAsync("AdBlocker", CreationCollisionOption.OpenIfExists);
    public async static Task<StorageFolder> GetDataFolderLocal() => _DataFolderLocal ??= await Helper.Functions.GetSaveFolderLocal().CreateFolderAsync("AdBlocker", CreationCollisionOption.OpenIfExists);

    private static StorageFolder? _DataFolderCache;
    private static StorageFolder? _DataFolderLocal;

    public const string FileNameWhiteList = "whitelist.txt";
    public const string FileNameUser = "user.txt";
    public const string FileNameDb = "rules.db";

    public static DistillNET.FilterDbCollection? Filter { get; private set; }

    //Make sure the list is sorted and ToUpeerInvariant()ed before use.
    public static List<string> UserWhitelist { get; } = new List<string>();
    private static bool DomainsWhitelistLoaded = false;
    public const int FiltersCacheSize = 20;
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersCache = new();
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersWhitelistCache = new();
    private static DistillNET.UrlFilter[]? FiltersCacheGlobal = null;
    private static DistillNET.UrlFilter[]? FiltersWhitelistCacheGlobal = null;

    private static HttpClient? _CommonHttpClient;
    public static HttpClient CommonHttpClient => _CommonHttpClient ??= new HttpClient();

    public static async Task<bool> AddUserWhitelist(string domain)
    {
        var upper = domain.ToUpperInvariant();
        if (UserWhitelist.BinarySearch(upper) >= 0) return true;
        UserWhitelist.Add(upper);
        try
        {
            var file = await GetWhiteListFileAsync();
            await FileIO.AppendLinesAsync(file, new[] { domain.ToLowerInvariant() });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> RemoveUserWhitelist(string domain)
    {
        var upper = domain.ToUpperInvariant();
        if (!(UserWhitelist.BinarySearch(upper) >= 0)) return false;
        UserWhitelist.Remove(upper);
        try
        {
            var file = await GetWhiteListFileAsync();
            var text = string.Join('\n', UserWhitelist.Select(a => a.ToLowerInvariant()));
            await FileIO.WriteTextAsync(file, text, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            return true;
        }
        catch
        {
            return false;
        }
    }


    public static async Task LoadUserWhitelist()
    {
        UserWhitelist.Clear();
        try
        {
            var file = await GetWhiteListFileAsync();
            if (file is null) return;
            var text = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            var list = text.Split('\n', '\r').Where(a => !a.StartsWith("#") && !string.IsNullOrWhiteSpace(a)).Select(a => a.ToUpperInvariant()).ToArray();
            UserWhitelist.AddRange(list);
            DomainsWhitelistLoaded = true;
        }
        catch
        {
            UserWhitelist.Clear();
            DomainsWhitelistLoaded = false;
        }
        return;
    }

    public static async void WebView2WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!(bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled)) return;
        if (Filter is null) return;
        if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uriReq)) return;
        if (!Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri uri)) return;
        if (!(uri.Scheme.ToUpperInvariant() is "HTTPS" or "HTTP")) return;
        var domain = uri.Host.ToUpperInvariant();
        if (UserWhitelist.BinarySearch(domain) >= 0) return;
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
        if (!IsBlocked(uriReq, uriReq.Host, headers))
        {
            await YouTube.WebView2WebResourceRequested(sender, args);
            return;
        }
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
        if (!DomainsWhitelistLoaded) await LoadUserWhitelist();
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
        async Task<bool> LoadFilterFile(StorageFile file)
        {
            if (file is null) return false;
            if (Filter is null) return false;
            try
            {
                var stream = await file.OpenReadAsync();
                if (stream is null) return false;
                using var streamNative = stream.AsStream();
                var result = Filter?.ParseStoreRulesFromStream(streamNative, 1);
                return true;
            }
            catch
            {
                return false;
            }
        }

        var info = await LocalInfo.GetContentAsync();
        await SemaphoreLoadFromText.WaitAsync();
        try
        {
            bool success = true;
            {
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
                foreach (var fn in info.selected)
                {
                    var file = await folder.TryGetItemAsync(fn.filename) as StorageFile;
                    if (file is null) continue;
                    //if (Path.GetFileName(file.Path) is FileNameWhiteList or FileNameUser or FileNameDb) continue;
                    if (Path.GetFileName(file.Path) is FileNameDb) continue;
                    if (Path.GetExtension(file.Path).ToUpperInvariant() != ".TXT") continue;
                    success &= await LoadFilterFile(file);
                }
            }
            {
                var file = await GetCustomFiltersFileAsync();
                if (file is not null) success &= await LoadFilterFile(file);
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

    public static async Task<StorageFile?> GetCustomFiltersFileAsync(bool create = false) => await GetDataFolderFileAsync(FileNameUser, create);
    public static async Task<StorageFile?> GetWhiteListFileAsync(bool create = false) => await GetDataFolderFileAsync(FileNameWhiteList, create);

    private static async Task<StorageFile?> GetDataFolderFileAsync(string fileName, bool create)
    {
        var folder = await GetDataFolderLocal();
        if (folder is null) return null;
        if (create)
        {
            return await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }
        else
        {
            return await folder.TryGetItemAsync(fileName) as StorageFile;
        }
    }

    public static async Task TryRemoveListFromInfo(string filename)
    {
        var info = await LocalInfo.GetContentAsync();
        info.selected = info.selected.Where(a => !a.filename.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).ToArray();
    }

    public static async Task<bool> TryRemoveList(Storages.ExtensionAdBlockerItems.item item)
    {
        string? filename = item?.filename;
        if (filename is null) return false;
        {
            var info = await LocalInfo.GetContentAsync();
            //This is not efficient, but ignorable. I ignore.
            info.selected = info.selected.Where(a => !a.filename.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).ToArray();
        }
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

    public static async Task<bool> TryDownloadList(Storages.ExtensionAdBlockerItems.item item)
    {
        if (!Uri.TryCreate(item.source, UriKind.Absolute, out var uri)) return false;
        {
            var info = await LocalInfo.GetContentAsync();
            var list = info.selected.ToList();
            if (!info.selected.Any(a => a.filename.Equals(item.filename, StringComparison.InvariantCultureIgnoreCase))) list.Add(new Storages.ExtensionAdBlockerItems.item() { filename = item.filename });
            info.selected = list.ToArray();
        }

        try
        {
            var folder = await GetDataFolderCache();
            var file = await folder.CreateFileAsync($"{item.filename}.dl", CreationCollisionOption.ReplaceExisting);
            if (file is null) return false;
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            //var wc = new WebClient();
            //wc.DownloadFileAsync(uri, System.IO.Path.Combine(path, item.filename));
            var hc = CommonHttpClient;
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

    public static async Task<bool> IsItemLoaded(Storages.ExtensionAdBlockerItems.item item)
    {
        var cache = await GetDataFolderCache();
        return await cache.FileExistsAsync(item.filename);
    }

    public static class YouTube
    {
        public static async Task WebView2WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
        {
            if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uriReq)) return;
            if (IsUriYouTube(uriReq))
            {
                //YouTube specific ad blocking.
                if (IsUriWatchPage(uriReq))
                {
                    await WebView2WebResourceRequestedCommon(sender, args, text =>
                    {
                        return Regex.Replace(text, @"var ytInitialPlayerResponse\s*=\s*(\{.+?\});", (a) =>
                        {
                            return a.Value.Replace(a.Groups[1].Value, RemoveAdsFromJson(a.Groups[1].Value));
                        });
                    });

                }
                else if (uriReq.LocalPath.Equals("/youtubei/v1/player", StringComparison.InvariantCultureIgnoreCase))
                {
                    await WebView2WebResourceRequestedCommon(sender, args, text => RemoveAdsFromJson(text));
                }
            }
        }

        public static bool IsUriYouTube(Uri uriReq) => uriReq.Host.EndsWith("youtube.com", StringComparison.InvariantCultureIgnoreCase) || uriReq.Host.EndsWith("youtubekids.com", StringComparison.InvariantCultureIgnoreCase) || uriReq.Host.EndsWith("youtube-nocookie.com", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsUriWatchPage(Uri uriReq) => uriReq.LocalPath.Equals("/watch", StringComparison.InvariantCultureIgnoreCase) || uriReq.LocalPath.StartsWith("/shorts/", StringComparison.InvariantCultureIgnoreCase) || uriReq.LocalPath.Equals("/live", StringComparison.InvariantCultureIgnoreCase);

        public static async Task WebView2WebResourceRequestedCommon(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args, Func<string, string> removeAdsFunction)
        {
            if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uriReq)) return;
            var d = args.GetDeferral();
            var client = CommonHttpClient;
            var message = new HttpRequestMessage(HttpMethod.Get, uriReq);
            message.Method = args.Request.Method?.ToUpperInvariant() switch
            {
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "HEAD" => HttpMethod.Head,
                "PATCH" => HttpMethod.Patch,
                "OPTIONS" => HttpMethod.Options,
                "GET" => HttpMethod.Get,
                _ => HttpMethod.Get,
            };
            if (args.Request.Content is not null)
            {
                message.Content = new HttpStreamContent(args.Request.Content);
                foreach (var item in args.Request.Headers)
                {
                    try
                    {
                        message.Content.Headers.Add(item.Key, item.Value);
                    }
                    catch { }
                }
            }
            message.Headers.Clear();
            foreach (var item in args.Request.Headers)
            {
                try
                {
                    message.Headers.Add(item.Key, item.Value);
                }
                catch { }
            }

            var response = await client.SendRequestAsync(message);
            if (!response.IsSuccessStatusCode) return;
            var text = await response.Content.ReadAsStringAsync();
            text = removeAdsFunction?.Invoke(text) ?? text;
            var ms = new InMemoryRandomAccessStream();
            using var dw = new DataWriter(ms);
            dw.WriteString(text);
            await dw.StoreAsync();
            await ms.FlushAsync();
            ms.Seek(0);
            var hs = new StringBuilder();
            foreach (var h in response.Headers)
            {
                hs.AppendLine($"{h.Key}:{h.Value}");
            }
            if (response.StatusCode == Windows.Web.Http.HttpStatusCode.Ok) args.Response = sender.Environment.CreateWebResourceResponse(ms, 200, "OK", hs.ToString());
            //args.Response = sender.Environment.CreateWebResourceResponse(ms, (int)response.StatusCode, response.ReasonPhrase, hs.ToString());
            d.Complete();
            return;
        }

        public static string RemoveAdsFromJson(string text)
        {
            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(text);
                if (json is not null)
                {
                    if (json.ContainsKey("playerAds")) json.Remove("playerAds");
                    if (json.ContainsKey("adPlacements")) json.Remove("adPlacements");
                    text = JsonSerializer.Serialize(json);
                }
            }
            catch { }
            return text;
        }
    }
}
