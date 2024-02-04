using kurema.FileExplorerControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.ViewModels;
public class OPDSManagerViewModel : BaseViewModel
{
	private bool _ShowAllLanguages;
	public bool ShowAllLanguages { get => _ShowAllLanguages; set => SetProperty(ref _ShowAllLanguages, value); }


	private string _AddEntryURL;
	public string AddEntryURL { get => _AddEntryURL; set => SetProperty(ref _AddEntryURL, value); }


	public System.Windows.Input.ICommand AddEntryCommand { get; private set; }


}
