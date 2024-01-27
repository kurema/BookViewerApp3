using kurema.FileExplorerControl.Models.FileItems;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace BookViewerApp.Extension.OPDS.FileTypes;

public static class Helper
{
	static FileType[] fileTypes = new FileType[] {

	};

	static IFileItem? GetFileItem(SyndicationItem item, SyndicationLink link)
	{
		return fileTypes.FirstOrDefault(a => a.MimeTypes.Contains(link.MediaType))?.GetFileItem(item, link);
	}
}