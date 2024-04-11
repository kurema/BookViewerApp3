using kurema.FileExplorerControl.Models;
using kurema.FileExplorerControl.Models.FileItems;
using kurema.FileExplorerControl.Models.IconProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BookViewerApp.Extension;
public class DelegateItem : IFileItem, IIconProviderProvider
{
	public DelegateItem(string name, string path, Action<IFileItem> openAction)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Path = path ?? throw new ArgumentNullException(nameof(path));
		OpenAction = openAction ?? throw new ArgumentNullException(nameof(openAction));
	}

	public string Name { get; set; } = string.Empty;

	public string Path { get; set; } = string.Empty;

	public string FileTypeDescription { get; set; } = string.Empty;

	public DateTimeOffset DateCreated { get; set; } = new();

	public bool IsFolder => false;

	public ICommand DeleteCommand { get; set; } = null;

	public ICommand RenameCommand { get; set; } = null;

	public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; } = new Func<IFileItem, MenuCommand[]>(_ => Array.Empty<MenuCommand>());

	public object Tag { get; set; }

	public event EventHandler Updated;

	public Action<IFileItem> OpenAction { get; set; }
	public IIconProvider Icon { get; set; } = null;

	public Task<ObservableCollection<IFileItem>> GetChildren()
	{
		return Task.FromResult(new ObservableCollection<IFileItem>());
	}

	public IEnumerable<IFileItem> GetSearchResults(string word)
	{
		return Array.Empty<IFileItem>();
	}

	public Task<ulong?> GetSizeAsync()
	{
		return Task.FromResult<ulong?>(0);
	}

	public void OnUpdate()
	{
		Updated?.Invoke(this, new EventArgs());
	}

	public void Open()
	{
		OpenAction?.Invoke(this);
	}

	public Task<Stream> OpenStreamForReadAsync()
	{
		return Task.FromResult(Stream.Null);
	}

	public Task<Stream> OpenStreamForWriteAsync()
	{
		return Task.FromResult(Stream.Null);
	}
}
