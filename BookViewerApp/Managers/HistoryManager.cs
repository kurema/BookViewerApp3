using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace BookViewerApp.Managers
{
    public static class HistoryManager
    {
        public static Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList List => Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

        public static void AddEntry(IStorageItem item,string ID=null)
        {
            if (item == null) return;
            var metadata = new Metadata() { Name = item.Name, ID = ID, Date = DateTimeOffset.Now }.Serialize();
            //ToDo:同一ファイル排除?
            List.Add(item, metadata);
        }

        public class Metadata
        {
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
}
