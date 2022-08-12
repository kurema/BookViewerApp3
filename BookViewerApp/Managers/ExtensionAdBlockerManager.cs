using BookViewerApp.Storages;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Web.Http;

#nullable enable

namespace BookViewerApp.Managers;
internal class ExtensionAdBlockerManager
{
    //Stay away from GPL!
    //If users download manually, I think it's safe.
    public static StorageContent<Storages.ExtensionAdBlockerItems.items> LocalLists = new(StorageContent<Storages.ExtensionAdBlockerItems.items>.SavePlaces.InstalledLocation, "ms-appx:///res/values/AdBlockList.xml", () => new());

    private async static Task<StorageFolder> GetDataFolder() => _DataDolder ??= await Helper.Functions.GetSaveFolderLocalCache().CreateFolderAsync("AdBlockerLists", CreationCollisionOption.OpenIfExists);

    private static StorageFolder? _DataDolder;

    public const string FileNameWhiteList = "whitelist.txt";
    public const string FileNameUser = "user.txt";

    //Make sure the list is sorted and ToUpeerInvariant()ed before use.
    public static List<string> DomainsBlackList { get; private set; } = new List<string>();
    public static List<string> DomainsWhiteList { get; private set; } = new List<string>();

    public bool IsBlocked(Uri uri, Uri uriOriginal)
    {
        var host = uri.Host.ToUpperInvariant();
        if (DomainsWhiteList.BinarySearch(uriOriginal.Host.ToUpperInvariant()) >= 0) return false;
        if (DomainsWhiteList.BinarySearch(host) >= 0) return false;
        if (DomainsBlackList.BinarySearch(host) >= 0) return true;
        return false;
    }

    public async Task<bool> TryDownloadList(Storages.ExtensionAdBlockerItems.itemsGroupItem item)
    {
        if (!Uri.TryCreate(item.source, UriKind.Absolute, out var uri)) return false;

        try
        {
            var folder = await GetDataFolder();
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
}
