namespace BookViewerApp.ViewModels
{
    public class Bookshelf2BookViewModel:Helper.ViewModelBase
    {
        private kurema.FileExplorerControl.ViewModels.FileItemViewModel _File;
        public kurema.FileExplorerControl.ViewModels.FileItemViewModel File { get => _File; set => SetProperty(ref _File, value); }
    }
}
