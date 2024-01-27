using kurema.FileExplorerControl.Models.FileItems;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace BookViewerApp.Extension.OPDS.FileTypes;

public interface IFileType
{
	string[] MimeTypes { get; }
	IFileItem GetFileItem(SyndicationItem item, SyndicationLink link);
}

public class FileType : IFileType
{
	public string[] MimeTypes { get; }

	Func<SyndicationItem, SyndicationLink, IFileItem> getFunc;

	public FileType(string[] mimeTypes, Func<SyndicationItem, SyndicationLink, IFileItem> getFunc)
	{
		MimeTypes = mimeTypes ?? throw new ArgumentNullException(nameof(mimeTypes));
		this.getFunc = getFunc ?? throw new ArgumentNullException(nameof(getFunc));
	}

	public IFileItem GetFileItem(SyndicationItem item, SyndicationLink link)
	{
		return getFunc.Invoke(item, link);
	}
}
