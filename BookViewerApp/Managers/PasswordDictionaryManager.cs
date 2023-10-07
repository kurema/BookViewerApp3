using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

#nullable enable
namespace BookViewerApp.Managers;
public static class PasswordDictionaryManager
{
	public static string[]? ListPasswords { get; private set; }
	//public static string[]? ListWords { get; private set; }
	public static string[]? ListWordsAdditional { get; private set; }

	public const string FilenamePasswords = "passwords.gz";
	public const string FilenameWords = "words.gz";
	public const string FilenameWordsAdditional = "additional_passwords.txt";
	public const string BasePath = "ms-appx:///res/passwords/";

	public static async Task<bool> LoadAsync()
	{
		bool success = true;
		try { ListPasswords ??= await GetGzLists(System.IO.Path.Combine(BasePath, FilenamePasswords)); } catch { success = false; }
		//try { ListWords ??= await GetGzLists(System.IO.Path.Combine(BasePath, FilenameWords)); } catch { success = false; }
		try { ListWordsAdditional ??= await GetSimpleLists(System.IO.Path.Combine(BasePath, FilenameWordsAdditional)); } catch { success = false; }
		return success;
	}

	public static IEnumerable<string> ListCombined
	{
		get
		{
			var passSetting = ((Storages.SettingStorage.GetValue(Storages.SettingStorage.SettingKeys.PdfPasswordDictionary) as string) ?? string.Empty).Split('\n', '\r');
			foreach (var item in passSetting) yield return item;
			foreach (var item in ListWordsAdditional ?? Array.Empty<string>()) yield return item;
			foreach (var item in ListPasswords ?? Array.Empty<string>()) yield return item;
			//foreach (var item in ListWords ?? Array.Empty<string>()) yield return item;
		}
	}

	public static IEnumerable<string> ListCombinedBasic
	{
		get
		{
			var passSetting = ((Storages.SettingStorage.GetValue(Storages.SettingStorage.SettingKeys.PdfPasswordDictionary) as string) ?? string.Empty).Split('\n', '\r');
			foreach (var item in passSetting) yield return item;
			foreach (var item in ListWordsAdditional ?? Array.Empty<string>()) yield return item;
		}
	}


	private static async Task<string[]> GetGzLists(string path)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
		using var st = await file.OpenReadAsync();
		using var gz = new System.IO.Compression.GZipStream(st.AsStream(), System.IO.Compression.CompressionMode.Decompress);
		using var sr = new StreamReader(gz);
		return (await sr.ReadToEndAsync()).Split('\r', '\n').Where(a => !string.IsNullOrEmpty(a)).ToArray();
	}

	private static async Task<string[]> GetSimpleLists(string path)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
		using var st = await file.OpenReadAsync();
		using var sr = new StreamReader(st.AsStream());
		return (await sr.ReadToEndAsync()).Split('\r', '\n').Where(a=>!string.IsNullOrEmpty(a)).ToArray();
	}
}
