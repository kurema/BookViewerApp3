using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public interface IFileItem
    {
        Task<ObservableCollection<IFileItem>> GetChildren();
        string Name { get; }
        string Path { get; }
        void Open();

        DateTimeOffset DateCreated { get; }
        Task<ulong?> GetSizeAsync();

        bool IsFolder { get; }

        System.Windows.Input.ICommand DeleteCommand { get; }
        System.Windows.Input.ICommand RenameCommand { get; }

        Task<System.IO.Stream> OpenStreamForReadAsync();
        Task<System.IO.Stream> OpenStreamForWriteAsync();
    }
}
