using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

namespace BookViewerApp.Storages
{
    public static class HistoryStorage
    {
        public static StorageContent<HistoryInfo[]> Content = new StorageContent<HistoryInfo[]>(StorageContent<HistoryInfo[]>.SavePlaces.Local, "Histories.xml", () => new HistoryInfo[0]);

        public static readonly int MaximumHistoryCount = 100;


        public async static Task AddHistory(HistoryInfo info)
        {
            Content.TryOperate<HistoryInfo>(a => {
                a.Add(info);

                var sorted = a.OrderByDescending(b => b.Date).ToArray();
                for (int i = MaximumHistoryCount; i < sorted.Length; i++) a.Remove(sorted[i]);
            });
            await Content.SaveAsync();
        }

        public class HistoryInfo
        {
            public string Token { get; set; }
            public string PathRelative { get; set; }
            public string Path { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }

            public DateTimeOffset Date { get; set; }

            public async Task<Windows.Storage.StorageFile> GetFile()
            {
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
}
