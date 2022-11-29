using System;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows.Input;

using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace kurema.FileExplorerControl.Models.FileItems;

public class StorageFileItem : IFileItem
{
    public StorageFileItem(IStorageItem content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }


    private IStorageItem _Content;
    public IStorageItem Content
    {
        get => _Content; set
        {
            _Content = value;
            (RenameCommand as Helper.DelegateAsyncCommand)?.OnCanExecuteChanged();
            (DeleteCommand as Helper.DelegateAsyncCommand)?.OnCanExecuteChanged();
        }
    }

    public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }
    public Func<IFileItem, MenuCommand[]> MenuCommandsProviderCascade { get; set; }

    public string Name => Content.Name;

    public DateTimeOffset DateCreated => DateCreatedOverride ?? Content.DateCreated;

    //History用。日時を強制的に上書きする。あんまりスマートではない。
    //For history. Not smart.
    public DateTimeOffset? DateCreatedOverride { get; set; } = null;

    public bool IsFolder => Content is StorageFolder;

    public string Path => Content?.Path ?? "";

    public bool CanDelete => Content != null;

    public bool CanRename => Content != null;

    public async Task<ObservableCollection<IFileItem>> GetChildren()
    {
        if (Content is StorageFolder f)
        {
            ////https://docs.microsoft.com/en-us/windows/uwp/files/fast-file-properties
            //var folderIndexedState = await f.GetIndexedStateAsync();
            //if (folderIndexedState == Windows.Storage.Search.IndexedState.FullyIndexed)
            //{
            //    var result = new System.Collections.Generic.List<IFileItem>();
            //    result.AddRange((await f.GetFoldersAsync()).Select(a => new StorageFileItem(a) { MenuCommandsProvider = this.MenuCommandsProviderCascade, MenuCommandsProviderCascade = this.MenuCommandsProviderCascade, FileTypeDescriptionProvider = FileTypeDescriptionProvider }));

            //    uint index = 0;
            //    const uint stepSize = 100;
            //    var qResult = f.CreateFileQuery();
            //    var buffer = await qResult.GetFilesAsync(index, stepSize);
            //    while(buffer.Count != 0)
            //    {
            //        result.AddRange(buffer.Where(a => !(a is StorageFile file && !file.IsAvailable)).Select(a => new StorageFileItem(a) { MenuCommandsProvider = this.MenuCommandsProviderCascade, MenuCommandsProviderCascade = this.MenuCommandsProviderCascade, FileTypeDescriptionProvider = FileTypeDescriptionProvider }));

            //        index += stepSize;
            //        buffer = await qResult.GetFilesAsync(index, stepSize);
            //    }
            //    return new ObservableCollection<IFileItem>(result);
            //}
            //else
            {
                return new ObservableCollection<IFileItem>((await f.GetItemsAsync())
                    .Where(a => !(a is StorageFile file && !file.IsAvailable))//https://docs.microsoft.com/ja-jp/windows/uwp/files/quickstart-determining-availability-of-microsoft-onedrive-files
                    .Select(a => new StorageFileItem(a) { MenuCommandsProvider = this.MenuCommandsProviderCascade, MenuCommandsProviderCascade = this.MenuCommandsProviderCascade, FileTypeDescriptionProvider = FileTypeDescriptionProvider }));
            }
        }
        else
        {
            return new ObservableCollection<IFileItem>();
        }
    }

    public EventHandler OpenEvent;

    public void Open()
    {
        OpenEvent?.Invoke(this, new EventArgs());
    }

    public async Task<Stream> OpenStreamForReadAsync()
    {
        if (Content is StorageFile file)
        {
            return await file.OpenStreamForReadAsync();
        }
        return null;
    }

    public async Task<Stream> OpenStreamForWriteAsync()
    {
        if (Content is StorageFile file)
        {
            return await file.OpenStreamForWriteAsync();
        }
        return null;
    }

    public async Task<ulong?> GetSizeAsync()
    {
        if (IsFolder)
        {
            return null;
        }
        else if (Content is StorageFile f)
        {
            var prop = await f.GetBasicPropertiesAsync();
            return prop.Size;
        }
        else
        {
            return null;
        }
    }

    private ICommand _RenameCommand;
    public ICommand RenameCommand
    {
        get
        {
            return _RenameCommand ??= new Helper.DelegateAsyncCommand(async (parameter) =>
            {
                if (Content is null) return;
                if (parameter is null) return;
                try
                {
                    await Content?.RenameAsync(parameter.ToString());
                }
                catch
                {
                }
            });
        }
        set
        {
            _RenameCommand = value;
        }
    }


    private ICommand _DeleteCommand;
    public ICommand DeleteCommand
    {
        get => _DeleteCommand ??= new Helper.DelegateAsyncCommand(async (parameter) =>
            {
                if (parameter is bool complete)
                {
                    if (Content is null) return;
                    try
                    {
                        await Content?.DeleteAsync(complete ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default);
                    }
                    catch
                    {
                    }
                }
            }, (b) => Content != null);

        set
        {
            _DeleteCommand = value;
        }
    }

    public object Tag { get; set; }

    public string FileTypeDescription => FileTypeDescriptionProvider?.Invoke(this.Content) ?? (this.IsFolder ? Application.ResourceLoader.Loader.GetString("FileType/Folder") : GetGeneralFileType(this.Content?.Path));

    public Func<IStorageItem, string> FileTypeDescriptionProvider { get; set; }

    public static string GetGeneralFileType(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        var ext = System.IO.Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return Application.ResourceLoader.Loader.GetString("FileType/NoExtension");
        if (ext.StartsWith('.')) ext = ext.Substring(1);
        return String.Format(Application.ResourceLoader.Loader.GetString("FileType/General"), ext.ToUpperInvariant());
    }

    public event EventHandler Updated;

    public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

    public IEnumerable<IFileItem> GetSearchResults(string word) => Array.Empty<IFileItem>();

}
