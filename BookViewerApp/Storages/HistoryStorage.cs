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
            await Content.GetContentAsync();
            var result = Content.Content.Where(b => b.Id != info.Id).OrderByDescending(b => b.Date).Take(MaximumHistoryCount).ToList();
            result.Insert(0, info);
            Content.Content = result.ToArray();

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
}
