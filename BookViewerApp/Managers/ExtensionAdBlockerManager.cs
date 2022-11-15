using BookViewerApp.Storages;
using Microsoft.Toolkit;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Utilities;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Networking;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using static System.Net.WebRequestMethods;

#nullable enable

// This code may not represent the actual state of your app.
// If you are using forked version refer the forked repository.
// Other codes:
// * UIs:
//https://github.com/kurema/BookViewerApp3/tree/master/BookViewerApp/Views/BrowserAddOn
// * Library :
//https://github.com/TechnikEmpire/DistillNET

namespace BookViewerApp.Managers;
public static partial class ExtensionAdBlockerManager
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
    //public static List<string> UserWhitelist { get; } = new List<string>();
    public static Whitelist UserWhitelist { get; } = new();
    private static bool DomainsWhitelistLoaded = false;
    public const int FiltersCacheSize = 20;
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersCache = new();
    private static Dictionary<string, (DateTime, DistillNET.UrlFilter[])> FiltersWhitelistCache = new();
    private static DistillNET.UrlFilter[]? FiltersCacheGlobal = null;
    private static DistillNET.UrlFilter[]? FiltersWhitelistCacheGlobal = null;

    private static HttpClient? _CommonHttpClient;
    public static HttpClient CommonHttpClient => _CommonHttpClient ??= new HttpClient();

    public const int UserWhitelistCacheSize = 20;
    public static List<(string host, bool isInWhitelist)> UserWhitelistCache = new();

    public static bool IsInWhitelist(string domain)
    {
        var upper = domain.ToUpperInvariant();// This operation may be called twice. It's not smart in performance but ignorable.
        var cached = UserWhitelistCache.FirstOrDefault(a => a.host == upper);
        //Cache is not reordered by last access. Shoul I?
        if (cached.host == upper) return cached.isInWhitelist;
        bool result = UserWhitelist.ContainsWildcard(upper);
        UserWhitelistCache.Insert(0, (upper, result));
        if (UserWhitelistCache.Count > UserWhitelistCacheSize) UserWhitelistCache.RemoveAt(UserWhitelistCache.Count - 1);
        return result;
    }

    //public static IEnumerable<int> GetWhitelistEntries(string domain)
    //{
    //    var upper = domain.ToUpperInvariant();// This operation may be called twice. It's not smart in performance but ignorable.
    //    int index = UserWhitelist.BinarySearch(upper);
    //    if (index >= 0) yield return index;

    //    int lastIndex = -1;
    //    while (true)
    //    {
    //        //You don't need cache if wildcard is not supported.
    //        lastIndex = upper.IndexOf('.', lastIndex + 1);
    //        if (lastIndex < 0) break;
    //        var s = "*" + upper.Substring(lastIndex);
    //        index = UserWhitelist.BinarySearch("*" + upper.Substring(lastIndex));
    //        if (index >= 0) yield return index;
    //    }
    //}

    public static async Task<bool> AddUserWhitelist(string domain)
    {
        var upper = domain.ToUpperInvariant();
        if (IsInWhitelist(upper)) return true;
        UserWhitelistCache.Clear();
        UserWhitelist.Add(upper);
        try
        {
            var file = await GetWhiteListFileAsync();
            if (file is null) return false;

            using var s = await file.OpenStreamForWriteAsync();
            if (!s.CanRead) return false;
            using var sw = new StreamWriter(s);
            if (s.Length > 0)
            {
                s.Seek(-1, SeekOrigin.End);
                var buffer = new byte[1];
                await s.ReadAsync(buffer, 0, 1);
                if (!(buffer[0] is (byte)'\r' or (byte)'\n'))
                {
                    await sw.WriteAsync(sw.NewLine);
                }
            }
            sw.WriteLine(domain.ToLowerInvariant());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<(bool removeSuccess, bool saveSuccess)> RemoveUserWhitelist(string domain)
    {
        var upper = domain.ToUpperInvariant();
        bool result = UserWhitelist.Remove(upper);
        result |= UserWhitelist.RemoveWildcard(upper);
        UserWhitelistCache.Clear();
        if (!result) return (false, false);
        return (result, await SaveUserWhitelist());
    }

    public static async Task LoadUserWhitelist()
    {
        UserWhitelistCache.Clear();
        UserWhitelist.Clear();
        try
        {
            var file = await GetWhiteListFileAsync();
            if (file is null) return;
            var text = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            var list = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToArray();
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

    public static async Task<bool> SaveUserWhitelist()
    {
        var s = string.Join(Environment.NewLine, UserWhitelist).ToLowerInvariant();
        try
        {
            var file = await GetWhiteListFileAsync();
            if (file is null) return false;
            await FileIO.WriteTextAsync(file, s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void WebViewWebResourceRequested(WebView sender, WebViewWebResourceRequestedEventArgs args)
    {
        if (!(bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled)) return;
        if (Filter is null) return;
        using var deferral = args.GetDeferral();
        if (args.Request is null || args.Request.RequestUri is null) return;
        var requestUri = args.Request.RequestUri;
        var headers = new NameValueCollection();
        foreach (var item in args.Request.Headers) headers.Add(item.Key, item.Value);

        try
        {
            string scheme = string.Empty;
            string domain = string.Empty;
            //Wait() is easy fix, but bad in performance.
            var thread = System.Threading.Thread.CurrentThread;
            sender.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                scheme = sender.Source.Scheme;
                domain = sender.Source.Host.ToUpperInvariant();
            }).AsTask().Wait();
            if (!(scheme.ToUpperInvariant() is "HTTPS" or "HTTP")) return;
            if (IsInWhitelist(domain)) return;

            if (!IsBlocked(requestUri, requestUri.Host, headers))
            {
                // YouTube blocking is disabled now.
                //YouTube.WebViewWebResourceRequested(sender, args);
                return;
            }
            args.Response = new HttpResponseMessage(Windows.Web.Http.HttpStatusCode.Forbidden);
        }
        catch (Exception e)
        {
#if DEBUG
            Debug.Write(e);
#endif
            //Ignore all exceptions. It's not a big deal.
        }
        finally
        {
            deferral.Complete();
        }
    }


    public static async void WebView2WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!(bool)SettingStorage.GetValue(SettingStorage.SettingKeys.BrowserAdBlockEnabled)) return;
        if (Filter is null) return;
        if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri uriReq)) return;
        if (!Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri uri)) return;
        if (!(uri.Scheme.ToUpperInvariant() is "HTTPS" or "HTTP")) return;
        var domain = uri.Host.ToUpperInvariant();
        if (IsInWhitelist(domain)) return;
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
        cache.TryAdd(domain, (DateTime.Now, filters));
        if (cache.Count > FiltersCacheSize) cache.Remove(cache.OrderBy(a => a.Value).First().Key);
        return filters;
    }

    public static async Task<DistillNET.FilterDbCollection?> LoadRules()
    {
        bool any = false;
        try { (any, _) = await UpdateExpiredFilters(); } catch { }
        if (!DomainsWhitelistLoaded && !any) await LoadUserWhitelist();
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

    public const double DefaultDaysToExpire = 7;

    /// <summary>
    /// Update expired or deleted filters.
    /// </summary>
    /// <returns>If there's any successful download, updateAny is true. If any download failed, successAll is false.</returns>
    public static async Task<(bool updateAny, bool successAll)> UpdateExpiredFilters()
    {
        var localInfoContent = (await LocalInfo.GetContentAsync());
        var selected = localInfoContent?.selected;
        if (selected is null or { Length: 0 }) return (false, true);
        var folder = await GetDataFolderCache();
        var filters = (await LocalLists.GetContentAsync()).group.SelectMany(a => a.item).ToList();
        if (localInfoContent?.filters is not null and { Length: > 0 }) filters.AddRange(localInfoContent.filters);
        bool updateAny = false;
        bool success = true;

        foreach (var item in selected)
        {
            if (string.IsNullOrEmpty(item?.filename)) continue;
            if (item?.expires is not null && item.expiresSpecified && item.expires <= DateTime.UtcNow && item.expires >= new DateTime(1800, 1, 1)) goto download; //If expired, download.
            var file = await folder.TryGetItemAsync(item!.filename) as StorageFile;
            if (file is null) goto download; //If filter is not downloaded, download.
            if (item?.expires is not null && item.expiresSpecified) continue; //If filter is downloaded and not expired, continue.
            var prop = await file.GetBasicPropertiesAsync();
            if (prop.DateModified.ToUniversalTime().AddDays(DefaultDaysToExpire) < DateTime.UtcNow) goto download; //If `expired` is not specified, assume 7 days.
            continue;
        download:;
            try
            {
                var filter = filters.FirstOrDefault(f => f.filename.Equals(item!.filename, StringComparison.InvariantCultureIgnoreCase));
                if (filter is null) continue;
                bool result = await TryDownloadList(filter);
                success &= result;
                updateAny |= result;
            }
            catch
            {
                success = false;
            }
        }
        if (updateAny) await LocalInfo.SaveAsync();
        return (updateAny, success);
    }

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
                    if (string.IsNullOrEmpty(fn?.filename)) continue;
                    var file = await folder.TryGetItemAsync(fn!.filename) as StorageFile;
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
        Storages.ExtensionAdBlockerItems.item itemInfo;
        if (!Uri.TryCreate(item.source, UriKind.Absolute, out var uri)) return false;

        {
            var info = await LocalInfo.GetContentAsync();
            var list = info.selected.ToList();
            itemInfo = info.selected.FirstOrDefault(a => a.filename.Equals(item.filename, StringComparison.InvariantCultureIgnoreCase));
            if (itemInfo is null) list.Add(itemInfo = new Storages.ExtensionAdBlockerItems.item() { filename = item.filename });
            info.selected = list.ToArray();
        }

        try
        {
            var folder = await GetDataFolderCache();
            var file = await folder.CreateFileAsync($"{item.filename}.dl", CreationCollisionOption.ReplaceExisting);
            if (file is null) return false;
            using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            //var wc = new WebClient();
            //wc.DownloadFileAsync(uri, System.IO.Path.Combine(path, item.filename));
            var hc = CommonHttpClient;
            var result = await hc.TryGetAsync(uri);
            if (!result.Succeeded) return false;
            result.ResponseMessage.EnsureSuccessStatusCode();
            await result.ResponseMessage.Content.WriteToStreamAsync(stream);

            var text = await FileIO.ReadTextAsync(file);

            {
                //For peterlowe.blocklist.txt
                //https://pgl.yoyo.org/adservers/serverlist.php?hostformat=adblockplus&showintro=0
                var match = new Regex(@"<pre>\r?\n?(.+)</pre>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline).Match(text);
                if (match.Success && match.Value.Length > text.Length * 0.5)
                {
                    text = match.Groups[1].Value;
                    stream.Seek(0);
                    stream.Size = 0;
                    using var w = new DataWriter(stream);
                    w.WriteString(text);
                    await w.StoreAsync();
                    //await FileIO.WriteTextAsync(file, text);
                }
            }
            {
                var match = new Regex(@"^!\s*Expires:\s*(\d+)\s+days", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline).Match(text);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                {
                    itemInfo.expiresSpecified = true;
                    //Should expire based on downloaded time?
                    //Or should I parse "! Last modified:"?
                    //Expireの日時はダウンロード時点を基準にすべきか、ファイル中の最終編集日時を参照すべきか。
                    //ダウンロード時点の場合、例えば4日なら毎回3日遅れになる可能性がある。
                    //一方、最終編集日時の4日後だとタイミングがズレて割合でほぼ4日遅れになる。そして日時がアメリカ表記でパースがめんどくさいし危うい。
                    //とりあえずダウンロード時ベースで。ダウンロード時点はファイルを見れば分かるはずなので、Expireが無ければわざわざ書かない。
                    itemInfo.expires = DateTime.UtcNow.AddDays(days);
                }
                else
                {
                    itemInfo.expiresSpecified = false;
                    itemInfo.expires = DateTime.UtcNow;
                }
            }

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

        public static void WebViewWebResourceRequested(WebView sender, WebViewWebResourceRequestedEventArgs args)
        {
            //You defenitely need await for http access. It's crazy to just Wait() for that. So it's impossible. I think.
            if (sender.Source is null) return;
            if (IsUriYouTube(sender.Source))
            {
                //YouTube specific ad blocking.
                if (IsUriWatchPage(sender.Source))
                {
                    //await WebView2WebResourceRequestedCommon(sender, args, text =>
                    //{
                    //    return Regex.Replace(text, @"var ytInitialPlayerResponse\s*=\s*(\{.+?\});", (a) =>
                    //    {
                    //        return a.Value.Replace(a.Groups[1].Value, RemoveAdsFromJson(a.Groups[1].Value));
                    //    });
                    //});

                }
                else if (args.Request.RequestUri.LocalPath.Equals("/youtubei/v1/player", StringComparison.InvariantCultureIgnoreCase))
                {
                    //await WebView2WebResourceRequestedCommon(sender, args, text => RemoveAdsFromJson(text));
                }
            }
        }

        public static bool IsUriYouTube(Uri uriReq) => uriReq.Host.EndsWith("youtube.com", StringComparison.InvariantCultureIgnoreCase) || uriReq.Host.EndsWith("youtubekids.com", StringComparison.InvariantCultureIgnoreCase) || uriReq.Host.EndsWith("youtube-nocookie.com", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsUriWatchPage(Uri uriReq) => uriReq.LocalPath.Equals("/watch", StringComparison.InvariantCultureIgnoreCase) || uriReq.LocalPath.StartsWith("/shorts/", StringComparison.InvariantCultureIgnoreCase) || uriReq.LocalPath.Equals("/live", StringComparison.InvariantCultureIgnoreCase);

        public static async Task WebView2WebResourceRequestedCommon(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args, Func<string, string> removeAdsFunction)
        {
            var d = args.GetDeferral();
            await WebResourceRequestedCommon(removeAdsFunction, (ms, s) =>
            {
                args.Response = sender.Environment.CreateWebResourceResponse(ms, 200, "OK", s);
            },
                args.Request.Uri, args.Request.Method, args.Request.Content, args.Request.Headers.ToArray() ?? new KeyValuePair<string, string>[0]);
            d.Complete();
        }


        public static async Task WebResourceRequestedCommon(Func<string, string> removeAdsFunction,Action<InMemoryRandomAccessStream,string> setResponse, string requestUri, string? requestMethod, IRandomAccessStream requestContent, KeyValuePair<string, string>[] requestHeaders)
        {
            if (!Uri.TryCreate(requestUri, UriKind.Absolute, out Uri uriReq)) return;
            var client = CommonHttpClient;
            var message = new HttpRequestMessage(HttpMethod.Get, uriReq);
            message.Method = requestMethod?.ToUpperInvariant() switch
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
            if (requestContent is not null)
            {
                message.Content = new HttpStreamContent(requestContent);
                foreach (var item in requestHeaders)
                {
                    try
                    {
                        message.Content.Headers.Add(item.Key, item.Value);
                    }
                    catch { }
                }
            }
            message.Headers.Clear();
            foreach (var item in requestHeaders)
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
            if (response.StatusCode == Windows.Web.Http.HttpStatusCode.Ok) setResponse(ms, hs.ToString());
            //args.Response = sender.Environment.CreateWebResourceResponse(ms, (int)response.StatusCode, response.ReasonPhrase, hs.ToString());
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

    public static string? GetHostOfUri(string? uri)
    {
        if (uri is null) return null;
        if (Uri.TryCreate(uri, UriKind.Absolute, out var uri2)) return uri2.Host;
        return null;
    }
}
