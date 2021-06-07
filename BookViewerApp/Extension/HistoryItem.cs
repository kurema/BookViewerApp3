using BookViewerApp.Storages;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using BookViewerApp.Helper;

namespace kurema.FileExplorerControl.Models.FileItems
{

    [Obsolete]
    public class HistoryItem : IFileItem
    {
        public HistoryStorage.HistoryInfo Content;

        public HistoryItem(HistoryStorage.HistoryInfo content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => Content.Name;

        public string Path => Content.Path;

        public string FileTypeDescription => BookViewerApp.Managers.ResourceManager.Loader.GetString("ItemType/HistoryItem");

        public DateTimeOffset DateCreated {
            get
            {
                try
                {
                    return Content.Date;
                }
                catch
                {
                    return new DateTimeOffset();
                }
            }
        }

        public bool IsFolder => false;

        public ICommand DeleteCommand => new DelegateCommand(async (_) =>
        {
            if (!string.IsNullOrWhiteSpace(Content.Id)) await HistoryStorage.DeleteHistoryById(Content.Id);
            else if (!string.IsNullOrWhiteSpace(Content.Path)) await HistoryStorage.DeleteHistoryByPath(Content.Path);
            await HistoryStorage.Content.SaveAsync();
            LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
            LibraryStorage.GarbageCollectToken();
        }, a => !(a is bool b && b == true));

        public ICommand RenameCommand => new InvalidCommand();

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public object Tag { get; set; }

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult<ObservableCollection<IFileItem>>(null);
        }

        public async Task<ulong?> GetSizeAsync()
        {
            return (await (await GetFile())?.GetBasicPropertiesAsync())?.Size;
        }

        public void Open()
        {
            return;
        }

        private Windows.Storage.StorageFile StorageCache = null;

        public async Task<Windows.Storage.StorageFile> GetFile()
        {
            if (StorageCache != null) return StorageCache;
            StorageCache = await Content.GetFile();
            if (StorageCache is null)
            {
                //Content.CurrentlyInaccessible = true;
                if (!string.IsNullOrWhiteSpace(Content.Id)) await HistoryStorage.DeleteHistoryById(Content.Id);
                else if (!string.IsNullOrWhiteSpace(Content.Path)) await HistoryStorage.DeleteHistoryByPath(Content.Path);
                LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
                await HistoryStorage.Content.SaveAsync();
            }
            return StorageCache;
        }

        public async Task<Stream> OpenStreamForReadAsync()
        {
            return await (await GetFile())?.OpenStreamForReadAsync();
        }

        public async Task<Stream> OpenStreamForWriteAsync()
        {
            return await (await GetFile())?.OpenStreamForWriteAsync();
        }

        public event EventHandler Updated;

        public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

    }
}
