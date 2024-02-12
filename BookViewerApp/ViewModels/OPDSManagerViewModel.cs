using BookViewerApp.Helper;
using kurema.FileExplorerControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace BookViewerApp.ViewModels;
public class OPDSManagerViewModel : BaseViewModel
{
	private bool _ShowAllLanguages;
	public bool ShowAllLanguages { get => _ShowAllLanguages; set => SetProperty(ref _ShowAllLanguages, value); }

	List<Storages.NetworkInfo.networksOPDSEntry> OPDSList = new();


	private string? _AddEntryURL;

	public OPDSManagerViewModel()
	{
		AddEntryCommand = new DelegateCommand(_ => { });
		ExcludeEntryCommand = new DelegateCommand(_ => { });
	}

	public string? AddEntryURL { get => _AddEntryURL; set => SetProperty(ref _AddEntryURL, value); }


	public System.Windows.Input.ICommand AddEntryCommand { get; private set; }
	public System.Windows.Input.ICommand ExcludeEntryCommand { get; private set; }


	IEnumerable<Storages.NetworkInfo.networksOPDSEntry> OPDSEntries => OPDSList.ToArray();
	IEnumerable<Storages.NetworkInfo.networksOPDSEntry> OPDSExcluded =>OPDSList.Where(a=>a.excluded).ToArray();
	Storages.NetworkInfo.networksOPDSEntry? SelectedEntry { get; set; }


}
