using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using BookViewerApp.Helper;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class HistoryMRUItem : IFileItem
    {
        public HistoryMRUItem(Windows.Storage.AccessCache.AccessListEntry access)
        {
            this.Content = BookViewerApp.Managers.HistoryManager.Metadata.Deserialize(access.Metadata);
            this.Token = access.Token;
        }

        BookViewerApp.Managers.HistoryManager.Metadata Content;
        string Token;

        public bool IsParentAccessible { get; set; } = true;

        public string Id => Content?.ID;

        public string Name => Content.Name;

        public string Path => throw new NotImplementedException();

        public string FileTypeDescription => BookViewerApp.Managers.ResourceManager.Loader.GetString("ItemType/HistoryItem");

        public DateTimeOffset DateCreated => Content.Date;

        public bool IsFolder => false;

        public ICommand DeleteCommand => new DelegateCommand((_) =>
        {
            if (BookViewerApp.Managers.HistoryManager.List.ContainsItem(this.Token)) BookViewerApp.Managers.HistoryManager.List.Remove(this.Token);
            this.OnUpdate();
        }, a => !(a is bool b && b == true));
        public ICommand RenameCommand => new InvalidCommand();

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public object Tag { get; set; }

        public event EventHandler Updated;

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult<ObservableCollection<IFileItem>>(null);
        }

        public async Task<ulong?> GetSizeAsync()
        {
            return (await (await GetFile())?.GetBasicPropertiesAsync())?.Size;
        }

        public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

        public void Open()
        {
            return;
        }

        public async Task<Stream> OpenStreamForReadAsync()
        {
            return await (await GetFile())?.OpenStreamForReadAsync();
        }

        public async Task<Stream> OpenStreamForWriteAsync()
        {
            return await (await GetFile())?.OpenStreamForWriteAsync();
        }

        private Windows.Storage.StorageFile StorageCache = null;

        public async Task<Windows.Storage.StorageFile> GetFile()
        {
            if (StorageCache != null) return StorageCache;
            if (!BookViewerApp.Managers.HistoryManager.List.ContainsItem(this.Token))
            {
                return null;
            }
            if (StorageCache == null)
            {
                try
                {
                    //What is AccessCacheOptions!?
                    //https://docs.microsoft.com/en-us/uwp/api/windows.storage.accesscache.accesscacheoptions?view=winrt-19041
                    StorageCache = await BookViewerApp.Managers.HistoryManager.List.GetFileAsync(this.Token);
                }
                catch
                {
                    return null;
                }
            }
            return StorageCache;
        }

    }
}
