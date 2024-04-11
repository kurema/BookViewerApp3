using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Storages;
public static class NetworkInfoStorage
{
	public static StorageContent<NetworkInfo.networks> NetworkInfoLocal = new(SavePlaces.Local, "Networks.xml", () => new());
	public static StorageContent<NetworkInfo.networks> OPDSPreset = new(SavePlaces.InstalledLocation, "ms-appx:///res/values/Networks.xml", () => new());

	public static async Task Load()
	{
		var result = await NetworkInfoLocal.GetContentAsync() ?? new NetworkInfo.networks();
		var list = result?.OPDSBookmarks?.ToList() ?? new List<NetworkInfo.networksOPDSEntry>();
		var dic = list.Where(a => !string.IsNullOrEmpty(a.id)).ToDictionary(a => a.id, a => a);
		var pre = await OPDSPreset.GetContentAsync();
		if (pre.OPDSBookmarks is null) return;
		foreach (var entry in pre.OPDSBookmarks)
		{
			if (!dic.ContainsKey(entry.id)) list.Add(new NetworkInfo.networksOPDSEntry()
			{
				@ref = entry.id,
			});
		}
	}

	public static async Task<IEnumerable<NetworkInfo.networksOPDSEntry>> GetFlatOPDS()
	{
		if (OPDSPreset.Content is null) await OPDSPreset.ReloadAsync();
		if (NetworkInfoLocal.Content is null) await NetworkInfoLocal.ReloadAsync();
		if (OPDSPreset.Content is null || NetworkInfoLocal.Content is null) return Array.Empty<NetworkInfo.networksOPDSEntry>();
		return GetFlatOPDS(NetworkInfoLocal.Content.OPDSBookmarks, OPDSPreset.Content.OPDSBookmarks);
	}

	static IEnumerable<NetworkInfo.networksOPDSEntry> GetFlatOPDS(NetworkInfo.networksOPDSEntry[] entries, NetworkInfo.networksOPDSEntry[] preset)
	{
		foreach (var item in NetworkInfoLocal.Content?.OPDSBookmarks)
		{
			if (string.IsNullOrEmpty(item.@ref))
			{
				yield return item;
			}
			else
			{
				var current = preset.FirstOrDefault(a => a.id == item.@ref);
				if (current is not null) yield return current;
			}
		}
	}
}
