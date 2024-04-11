using BookViewerApp.Storages;
using BookViewerApp.Storages.Library;
using kurema.BrowserControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems.OPDS;

#nullable enable

public class OPDSItem : IFileItem
{
	private int ErrorCount = 0;

	System.ServiceModel.Syndication.SyndicationFeed? Content;

	public string Source { get; private set; }

	private string NameInit;
	private string? NameOverride;

	public OPDSItem(string source, string name)
	{
		Source = source ?? throw new ArgumentNullException(nameof(source));
		NameInit = name ?? throw new ArgumentNullException(nameof(name));
	}

	public string Name
	{
		get
		{
			return NameOverride ?? Content?.Title?.Text ?? NameInit;
		}
		set
		{
			//You can actually set null to reset.
			NameOverride = value;
		}
	}

	public string Path => Source;

	public string FileTypeDescription => "";//ToDo: Implement

	public DateTimeOffset DateCreated => Content?.LastUpdatedTime ?? DateTimeOffset.MinValue;

	public bool IsFolder => true;

	public ICommand? DeleteCommand => null;

	public ICommand? RenameCommand => null;

	public Func<IFileItem, MenuCommand[]> MenuCommandsProvider => throw new NotImplementedException();

	public object? Tag { get; set; }

	public event EventHandler? Updated;

	public async Task<ObservableCollection<IFileItem>> GetChildren()
	{
		if (Content is null) await LoadAsync();
		throw new NotImplementedException();
	}

	public IEnumerable<IFileItem> GetSearchResults(string word)
	{
		throw new NotImplementedException();
	}

	public Task<ulong?> GetSizeAsync() => Task.FromResult<ulong?>(0);

	public void OnUpdate()
	{
		Updated?.Invoke(this, EventArgs.Empty);
	}

	public void Open()
	{
	}

	public Task<Stream?> OpenStreamForReadAsync()
	{
		return Task.FromResult<Stream?>(null);
	}

	public Task<Stream?> OpenStreamForWriteAsync()
	{
		return Task.FromResult<Stream?>(null);
	}

	async Task<bool> LoadAsync()
	{
		if (ErrorCount > 3) return false;
		var content = await LoadAsync(Source);
		if (content is not null)
		{
			Content = content;
			return true;
		}
		ErrorCount++;
		return false;
	}

	public static async Task<SyndicationFeed?> LoadAsync(string source)
	{
		try
		{
			var uri = new Uri(source);
			if (!uri.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase)) return null;
		}
		catch { return null; }
		return await Task.Run(() =>
		{
			try
			{
				var xr = System.Xml.XmlReader.Create(source);
				return SyndicationFeed.Load(xr);
			}
			catch
			{
				return null;
			}
		});
	}
}

public class OPDSSyndicationItemItem : IFileItem
{
	SyndicationItem Content;

	public OPDSSyndicationItemItem(SyndicationItem content)
	{
		Content = content ?? throw new ArgumentNullException(nameof(content));
	}

	public string Name => Content.Title?.Text ?? string.Empty;//ToDo: Update to default name.

	public string Path { get; set; } = string.Empty;

	public string FileTypeDescription => "Fix This!";

	public DateTimeOffset DateCreated => Content.PublishDate;

	public bool IsFolder => true;

	public ICommand? DeleteCommand => null;

	public ICommand? RenameCommand => null;

	public Func<IFileItem, MenuCommand[]> MenuCommandsProvider => throw new NotImplementedException();

	public object? Tag { get; set; }

	public event EventHandler? Updated;

	public Task<ObservableCollection<IFileItem>> GetChildren()
	{
		throw new NotImplementedException();
	}

	public IEnumerable<IFileItem> GetSearchResults(string word)
	{
		throw new NotImplementedException();
	}

	public Task<ulong?> GetSizeAsync() => Task.FromResult<ulong?>(0);

	public void OnUpdate()
	{
		Updated?.Invoke(this, EventArgs.Empty);
	}

	public void Open()
	{
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
