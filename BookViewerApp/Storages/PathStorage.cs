using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

using BookViewerApp.Helper;
using BookViewerApp.Storages.Library;

namespace BookViewerApp.Storages
{
    public static class PathStorage
    {
        public static StorageContent<PathInfo[]> Content = new StorageContent<PathInfo[]>(StorageContent<PathInfo[]>.SavePlaces.Local, "Paths.xml", () => new PathInfo[0]);

        public static string GetIdFromPath(string path)
        {
            var item = Content?.Content?.FirstOrDefault(a => String.Compare(path, a.Path, true) == 0);
            if (item != null) return item.ID;
            return null;
        }

        public static bool Add(PathInfo info)
        {
            return Content.TryAdd(info);
        }

        public static bool Add(string path,string id)
        {
            return Add(new PathInfo() { Path = path, ID = id });
        }

        public class PathInfo
        {
            public string Path { get; set; }
            public string ID { get; set; }
        }
    }
}
