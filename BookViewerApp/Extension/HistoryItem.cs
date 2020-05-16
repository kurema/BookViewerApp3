using BookViewerApp.Storages;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using BookViewerApp.Helper;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class HistoryItem : IFileItem
    {
        public BookViewerApp.Storages.HistoryStorage.HistoryInfo Content;

        public HistoryItem(HistoryStorage.HistoryInfo content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => Content.Name;

        public string Path => Content.Path;

        public DateTimeOffset DateCreated => Content.Date;

        public bool IsFolder => false;

        public ICommand DeleteCommand => new DelegateCommand(async (_) =>
        {
            await HistoryStorage.DeleteHistory(Content.Id);
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
            if (StorageCache == null)
            {
                //Content.CurrentlyInaccessible = true;
                await HistoryStorage.DeleteHistory(Content.Id);
                LibraryStorage.OnLibraryUpdateRequest(LibraryStorage.LibraryKind.History);
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
    }
}
