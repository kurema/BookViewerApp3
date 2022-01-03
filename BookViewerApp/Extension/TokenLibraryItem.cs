using BookViewerApp.Storages.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems;

public class TokenLibraryItem : IFileItem
{
    private ICommand renameCommand;

    public IFileItem Parent { get; set; }

    public TokenLibraryItem(libraryFolder content, StorageFileItem contentFileItem)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentFileItem = contentFileItem ?? throw new ArgumentNullException(nameof(contentFileItem));
    }

    //public async static Task<TokenLibraryItem> GetFromItem(libraryFolder content, Func<IFileItem, MenuCommand[]> menuCommandCascade)
    //{
    //    if (content is null) return null;
    //    var fileItem = await content.AsTokenLibraryItem();
    //    fileItem.ContentFileItem.MenuCommandsProviderCascade = menuCommandCascade;
    //    fileItem.ContentFileItem.MenuCommandsProvider = menuCommandCascade;

    //    return fileItem;
    //}

    public libraryFolder Content { get; }
    public StorageFileItem ContentFileItem { get; }

    public object Tag { get; set; }

    public string Name => String.IsNullOrWhiteSpace(Content?.title) ? ContentFileItem?.Name : Content.title;

    public string Path => ContentFileItem?.Path;

    public DateTimeOffset DateCreated => ContentFileItem?.DateCreated ?? DateTimeOffset.UtcNow;

    public bool IsFolder => ContentFileItem?.IsFolder ?? false;

    public ICommand DeleteCommand { get; set; } = new Helper.DelegateCommand(a => { }, a => false);

    public ICommand RenameCommand { get => renameCommand ??= new Helper.DelegateCommand(a => Content.title = a.ToString(), a => Content != null); set => renameCommand = value; }
    public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

    public string FileTypeDescription => BookViewerApp.Helper.UIHelper.GetFileTypeDescription(ContentFileItem?.Content) ?? "";

    public async Task<ObservableCollection<IFileItem>> GetChildren()
    {
        return await ContentFileItem?.GetChildren() ?? new ObservableCollection<IFileItem>();
    }

    public async Task<ulong?> GetSizeAsync()
    {
        return await ContentFileItem?.GetSizeAsync();
    }

    public void Open()
    {
        ContentFileItem?.Open();
    }

    public async Task<Stream> OpenStreamForReadAsync()
    {
        return await ContentFileItem?.OpenStreamForReadAsync();
    }

    public async Task<Stream> OpenStreamForWriteAsync()
    {
        return await ContentFileItem?.OpenStreamForWriteAsync();
    }

    public event EventHandler Updated;

    public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

}
