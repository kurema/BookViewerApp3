using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Collections.Generic;

namespace kurema.FileExplorerControl.Models.FileItems;

public class ContainerDelegateItem : IFileItem, IIconProviderProvider
{
    public ContainerDelegateItem(string fileName, string path, Func<ContainerDelegateItem, Task<IEnumerable<IFileItem>>> provider)
    {
        Name = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        ChildrenProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Only use DefaultIconLarge/DefaultIconSmall.
    /// </summary>
    public IconProviders.IIconProvider Icon { get; set; }

    public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

    public string Name { get; set; }

    public string Path { get; set; }

    public object Tag { get; set; }

    public DateTimeOffset DateCreated => DateTimeOffset.Now;

    public bool IsFolder => true;

    public Func<ContainerDelegateItem, Task<IEnumerable<IFileItem>>> ChildrenProvider { get; }

    public ObservableCollection<IFileItem> ChildrenProvided { get; private set; } = null;

    public ICommand DeleteCommand { get; set; } = null;

    public ICommand RenameCommand { get; set; } = null;

    public string FileTypeDescription { get; set; } = "";

    public async Task<ObservableCollection<IFileItem>> GetChildren()
    {
        var result = await ChildrenProvider?.Invoke(this);
        if (ChildrenProvided != null)
        {
            ChildrenProvided.Clear();
            if (result != null) foreach (var item in result) ChildrenProvided.Add(item);
            return ChildrenProvided;
        }
        else if (result is null)
        {
            return ChildrenProvided = new ObservableCollection<IFileItem>();
        }
        else
        {
            return ChildrenProvided = new ObservableCollection<IFileItem>(result);
        }
    }

    public Task<ulong?> GetSizeAsync()
    {
        return Task.FromResult<ulong?>(null);
    }

    public void Open()
    {
        return;
    }

    public Task<Stream> OpenStreamForReadAsync()
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<Stream> OpenStreamForWriteAsync()
    {
        return Task.FromResult<Stream>(null);
    }

    public event EventHandler Updated;

    public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

}
