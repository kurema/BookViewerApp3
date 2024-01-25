using BookViewerApp.Storages;
using BookViewerApp.Storages.Library;
using kurema.BrowserControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems.OPDS;

public class OPDSItem : IFileItem
{
	public string Name => throw new NotImplementedException();

	public string Path => throw new NotImplementedException();

	public string FileTypeDescription => throw new NotImplementedException();

	public DateTimeOffset DateCreated => throw new NotImplementedException();

	public bool IsFolder => true;

	public ICommand DeleteCommand => null;

	public ICommand RenameCommand => null;

	public Func<IFileItem, MenuCommand[]> MenuCommandsProvider => throw new NotImplementedException();

	public object Tag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public event EventHandler Updated;

	public Task<ObservableCollection<IFileItem>> GetChildren()
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IFileItem> GetSearchResults(string word)
	{
		throw new NotImplementedException();
	}

	public Task<ulong?> GetSizeAsync()
	{
		throw new NotImplementedException();
	}

	public void OnUpdate()
	{
		throw new NotImplementedException();
	}

	public void Open()
	{
		throw new NotImplementedException();
	}

	public Task<Stream> OpenStreamForReadAsync()
	{
		throw new NotImplementedException();
	}

	public Task<Stream> OpenStreamForWriteAsync()
	{
		throw new NotImplementedException();
	}

	async Task Load()
	{
		throw new NotImplementedException();
	}
}
