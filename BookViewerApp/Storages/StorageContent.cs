using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BookViewerApp.Helper;

namespace BookViewerApp.Storages;

public class StorageContent<T> where T : class
{
    public T Content { get; set; } = null;
    public string FileName { get; }

    private System.Threading.SemaphoreSlim Semaphore = new System.Threading.SemaphoreSlim(1, 1);

    public StorageContent(SavePlaces savePlace, string fileName, Func<T> getNewDelegate = null)
    {
        SavePlace = savePlace;
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        GetNewDelegate = getNewDelegate;
    }

    public enum SavePlaces
    {
        Local, Roaming, LocalCache, InstalledLocation
    }

    private Windows.Storage.StorageFolder DataFolder
    {
        get
        {
            switch (SavePlace)
            {
                case SavePlaces.Local:
                    return Functions.GetSaveFolderLocal();
                case SavePlaces.Roaming:
                    return Functions.GetSaveFolderRoaming();
                case SavePlaces.LocalCache:
                    return Functions.GetSaveFolderLocalCache();
                case SavePlaces.InstalledLocation:
                    //Not used. Don't use.
                    return Windows.ApplicationModel.Package.Current.InstalledLocation;
                default: return null;
            }
        }
    }

    private async Task<T> DeserializeAsync()
    {
        switch (SavePlace)
        {
            case SavePlaces.Local:
            case SavePlaces.Roaming:
            case SavePlaces.LocalCache:
                return await Functions.DeserializeAsync<T>(this.DataFolder, this.FileName, this.Semaphore);
            case SavePlaces.InstalledLocation:
                return await Functions.DeserializeAsync<T>(await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(this.FileName)), this.Semaphore);
            default:
                return null;
        }
    }

    public SavePlaces SavePlace { get; set; }

    public Func<T> GetNewDelegate { get; set; } = null;

    public T GetNew() => GetNewDelegate != null ? GetNewDelegate() : default;

    internal async Task<T> GetContentAsync() => Content ??= await DeserializeAsync() ?? GetNew();

    internal async Task SaveAsync()
    {
        if (Content != null && this.SavePlace != SavePlaces.InstalledLocation)
        {
            if (this.SavePlace != SavePlaces.Roaming)
            {
                try
                {
                    var folder = await this.DataFolder.CreateFolderAsync("backup", Windows.Storage.CreationCollisionOption.OpenIfExists);
                    var itemTarget = await folder.TryGetItemAsync(FileName);
                    if (itemTarget is Windows.Storage.StorageFolder f) await f.RenameAsync(System.IO.Path.GetRandomFileName(), Windows.Storage.NameCollisionOption.GenerateUniqueName);

                    if (itemTarget == null || itemTarget is Windows.Storage.StorageFolder || (itemTarget is Windows.Storage.StorageFile fileTarget && itemTarget.DateCreated < DateTimeOffset.Now.AddDays(-1)))
                    {
                        var fileOrigin = await DataFolder.GetFileAsync(this.FileName);
                        var prop = await fileOrigin.GetBasicPropertiesAsync();
                        if (fileOrigin != null && prop.Size > 0) { await fileOrigin.CopyAsync(folder, FileName, Windows.Storage.NameCollisionOption.ReplaceExisting); }
                    }
                }
                catch
                {
                }
            }
            try
            {
                await Functions.SerializeAsync(Content, this.DataFolder, this.FileName, this.Semaphore);
            }
            catch
            {
                //Restore?
            }
        }
    }

    public async Task<bool> ExistAsync()
    {
        var item = await DataFolder.TryGetItemAsync(FileName);
        return item != null;
    }

    public async Task TryDeleteAsync()
    {
        try
        {
            var item = await DataFolder.TryGetItemAsync(FileName);
            item?.DeleteAsync();
        }
        catch { return; }
    }

    public bool TryAdd<T2>(T2 item)
    {
        return TryOperate<T2>((a) => { a?.Add(item); });
    }

    public bool TryRemove<T2>(T2 item)
    {
        return TryOperate<T2>((a) => { a?.Remove(item); });
    }

    public bool TryOperate<T2>(Action<List<T2>> action)
    {
        if (action is null) return false;
        if (Content is IEnumerable<T2> contentEnum)
        {
            var itemList = contentEnum.ToList();
            action(itemList);

            T result = (itemList as T) ?? (itemList.ToArray() as T);
            if (result is null)
            {
                return false;
            }
            else
            {
                Content = result;
                return true;
            }
        }
        return false;
    }
}
