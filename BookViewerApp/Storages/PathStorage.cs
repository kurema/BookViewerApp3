using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections.ObjectModel;

using BookViewerApp.Helper;
using BookViewerApp.Storages.Library;

#nullable enable
namespace BookViewerApp.Storages;

public static class PathStorage
{
	public static StorageContent<PathInfo[]> Content = new(SavePlaces.Local, "Paths.xml", Array.Empty<PathInfo>);

    public static string? GetIdFromPath(string path)
    {
        var item = GetInfoFromPath(path);
        if (item != null) return item.ID;
        return null;
    }

    public static PathInfo? GetInfoFromPath(string path)
    {
        return Content?.Content?.FirstOrDefault(a => a.MatchPath(path));
    }

    public static bool AddOrReplace(string path, string id, uint? size = null)
    {
        var info = PathInfo.GetEncoded(path, id, (long?)size ?? -1);
        return Content.TryOperate<PathInfo>(a =>
        {
            var f = a.FirstOrDefault(b => b.MatchPath(path));
            if (f is null)
            {
                a.Add(info);
            }
            else
            {
                a.Remove(f);
                a.Add(info);
            }
        });
    }

    public static bool Add(PathInfo info)
    {
        return Content.TryAdd(info);
    }


    public class PathInfo
    {
        protected PathInfo()
        {
            Salt = "";
            ID = "";
            PathEncoded = "";
            Size = -1;
        }

        public static PathInfo GetEncoded(string path, string id, long size = -1)
        {
            var result = new PathInfo();
            result.Salt = Guid.NewGuid().ToString();
            result.PathEncoded = GetPathEncoded(path, result.Salt);
            result.ID = id;
            result.Size = size;
            return result;
        }

        public bool MatchPath(string path)
        {
            return GetPathEncoded(path, this.Salt) == this.PathEncoded;
        }

        public static string GetPathEncoded(string path, string salt) => Functions.GetHash(salt + "\n\n" + path);

        public string PathEncoded { get; set; }
        public string ID { get; set; }
        public long Size { get; set; }

        public string Salt { get; set; }
    }
}
