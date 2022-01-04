using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using BookViewerApp.Storages;

#nullable enable
namespace BookViewerApp.ViewModels;

public class Bookshelf2BookViewModel : Helper.ViewModelBase
{
    private kurema.FileExplorerControl.ViewModels.FileItemViewModel _File = new(new kurema.FileExplorerControl.Models.FileItems.FileItemPlaceHolder());
    public kurema.FileExplorerControl.ViewModels.FileItemViewModel File { get => _File; set => SetProperty(ref _File, value); }

    protected async Task GetFromBookInfoStorageAsync(string ID, long PageSize)
    {
        var bookInfo = await BookInfoStorage.GetBookInfoByIDAsync(ID);
        if (!(bookInfo is null))
        {
            switch (bookInfo.PageDirection)
            {
                case Books.Direction.L2R: IsR2L = false; break;
                case Books.Direction.R2L: IsR2L = true; break;
                case Books.Direction.Default: default: IsR2L = (bool)SettingStorage.GetValue("DefaultPageReverse"); break;
            }
            if (PageSize <= 0) ReadRate = 0; else ReadRate = (bookInfo.GetLastReadPage()?.Page ?? 0) / (double)PageSize;
        }
        else
        {
            IsR2L = false;
            ReadRate = 0;
        }
    }

    private double _ReadRate = 0;
    public double ReadRate { get => _ReadRate; set => SetProperty(ref _ReadRate, value); }

    private bool _IsR2L = false;

    public Bookshelf2BookViewModel()
    {
    }

    public async Task Load(kurema.FileExplorerControl.ViewModels.FileItemViewModel file)
    {
        var id = PathStorage.GetInfoFromPath(file.Path);
        this.File = file;
        if (id?.ID is null)
        {
            ReadRate = 0;
            IsR2L = false;
            return;
        }
        await GetFromBookInfoStorageAsync(id.ID, id.Size);
    }


    public bool IsR2L { get => _IsR2L; set => SetProperty(ref _IsR2L, value); }
}
