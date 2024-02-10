using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Storages;
public static class NetworkInfoStorage
{
	public static StorageContent<NetworkInfo.networks> Content = new(SavePlaces.Local, "Networks.xml", () => new());
	public static StorageContent<NetworkInfo.networks> LocalOPDS = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/Networks.xml", () => new());

	public static async Task Load()
	{
		var result = await Content.GetContentAsync() ?? new NetworkInfo.networks();
		var list = result?.OPDSBookmarks?.ToList() ?? new List<NetworkInfo.networksOPDSEntry>();
		var dic = list.Where(a => !string.IsNullOrEmpty(a.id)).ToDictionary(a => a.id, a => a);
		var pre = await LocalOPDS.GetContentAsync();
		if (pre.OPDSBookmarks is null) return;
		foreach (var entry in pre.OPDSBookmarks)
		{
			if (!dic.ContainsKey(entry.id)) list.Add(new NetworkInfo.networksOPDSEntry()
			{
				@ref=entry.id,
			});
		}
	}

	static IEnumerable<NetworkInfo.networksOPDSEntry> GetFlat(NetworkInfo.networksOPDSEntry[] entries, NetworkInfo.networksOPDSEntry[] pre)
	{
		foreach(var item in Content.Content?.OPDSBookmarks)
		{
			if (string.IsNullOrEmpty(item.@ref))
			{
				yield return item;
			}
			else
			{
				var current = pre.FirstOrDefault(a => a.id == item.@ref);
				if (current is not null) yield return current;
			}
		}
	}
}
