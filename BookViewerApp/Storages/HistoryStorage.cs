using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

namespace BookViewerApp.Storages;

[Obsolete]
public static class HistoryStorage
{
    public static StorageContent<HistoryInfo[]> Content = new StorageContent<HistoryInfo[]>(StorageContent<HistoryInfo[]>.SavePlaces.Local, "Histories.xml", () => new HistoryInfo[0]);

    public static int FutureAccessListMargin = 50;

    ////https://docs.microsoft.com/en-us/windows/uwp/files/how-to-track-recently-used-files-and-folders
    //public static Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList MRU = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

    public async static Task AddHistory(Windows.Storage.IStorageFile file, string id)
    {
        await Content.GetContentAsync();
        var lib = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Entries.Count + FutureAccessListMargin < Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed ?
            await Managers.BookManager.GetTokenFromPathOrRegister(file) : await Managers.BookManager.GetTokenFromPath(file.Path);
        if (lib != null) await AddHistory(new HistoryInfo() { Date = DateTime.Now, Id = id, Name = file.Name, Path = file.Path, Token = lib.token, PathRelative = lib.path });
    }

    public async static Task AddHistory(HistoryInfo info)
    {
        int MaximumHistoryCount = (int)SettingStorage.GetValue("MaximumHistoryCount");

        await Content.GetContentAsync();
        var result = Content.Content.Where(b => (string.IsNullOrWhiteSpace(info.Id) && (string.IsNullOrWhiteSpace(info.Path) || b.Path != info.Path)) || b.Id != info.Id).OrderByDescending(b => b.Date).ToList();
        {
            while (result.Count > MaximumHistoryCount)
            {
                var last = result.Last();
                result.Remove(last);
            }
        }
        result.Insert(0, info);
        Content.Content = result.ToArray();

        LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
        LibraryStorage.GarbageCollectToken();
        await Content.SaveAsync();
    }

    public async static Task DeleteHistoryById(string Id)
    {
        await Content.GetContentAsync();
        var result = Content.Content.Where(b => b.Id != Id).ToList();
        Content.Content = result.ToArray();

        LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
        await Content.SaveAsync();
    }

    public async static Task DeleteHistoryByPath(string path)
    {
        await Content.GetContentAsync();
        var result = Content.Content.Where(b => b.Path != path).ToList();
        Content.Content = result.ToArray();

        LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
        await Content.SaveAsync();
    }

    public async static Task DeleteHistoryByToken(string token)
    {
        await Content.GetContentAsync();
        var result = Content.Content.Where(b => b.Token != token).ToList();
        Content.Content = result.ToArray();

        LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
        await Content.SaveAsync();
    }


    public class HistoryInfo
    {
        public string Token { get; set; }
        public string PathRelative { get; set; }
        public string Path { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }

        public DateTime Date { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public bool CurrentlyInaccessible { get; set; } = false;

        public async Task<Windows.Storage.StorageFile> GetFile()
        {
            if (!string.IsNullOrWhiteSpace(Path))
            {
                var lib = await Managers.BookManager.GetTokenFromPath(Path);
                if (lib != null) return (await lib.GetStorageFolderAsync()) as Windows.Storage.StorageFile;
            }
            if (!string.IsNullOrWhiteSpace(Token))
            {
                if (string.IsNullOrWhiteSpace(Path))
                {
                    return (await Managers.BookManager.StorageItemGet(Token)) as Windows.Storage.StorageFile;
                }
                else
                {
                    return (await Managers.BookManager.StorageItemGet(Token, PathRelative)) as Windows.Storage.StorageFile;
                }
            }
            return null;
        }
    }
}
