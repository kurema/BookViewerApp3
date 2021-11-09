using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace BookViewerApp.Managers;

public static class HistoryManager
{
    /// <summary>
    /// Call OnUpdated when you changed the content
    /// </summary>
    public static Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList List => Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

    public static EventHandler Updated;

    [Obsolete]
    public async static Task IntegrateAsync()
    {
        if (!await Storages.HistoryStorage.Content.ExistAsync()) return;

        foreach (var item in (await Storages.HistoryStorage.Content.GetContentAsync()).OrderBy(a => a.Date))
        {
            try
            {
                var file = await item.GetFile();
                var metadata = new Metadata() { Date = item.Date, ID = item.Id, Name = item.Name }.Serialize();
                List.Add(file, metadata);
            }
            catch { }
        }
        OnUpdated();
        await Storages.HistoryStorage.Content.TryDeleteAsync();
    }


    public static void OnUpdated(object sender = null) => Updated?.Invoke(sender, new EventArgs());

    public static void AddEntry(IStorageItem file, string ID = null)
    {
        if (file is null) return;
        var metadata = new Metadata() { Name = file.Name, ID = ID ?? "", Date = DateTimeOffset.Now }.Serialize();
        List.Add(file, metadata);

        OnUpdated();
    }

    public class Metadata
    {
        public Metadata()
        {
            Name = "";
            ID = "";
            Date = DateTime.Now;
        }

        public string Name { get; set; }
        public string ID { get; set; }

        public DateTimeOffset Date { get; set; }

        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public static Metadata Deserialize(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Metadata>(data);
            }
            catch
            {
                return null;
            }
        }
    }
}
