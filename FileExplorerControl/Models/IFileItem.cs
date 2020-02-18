using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace kurema.FileExplorerControl.Models
{
    public interface IFileItem
    {
        Task<ObservableCollection<IFileItem>> GetChildren();
        string FileName { get; }
        string Path { get; }
        void Open();

        DateTimeOffset DateCreated { get; }
        Task<ulong?> GetSizeAsync();

        bool IsFolder { get; }

        Task<System.IO.Stream> OpenStreamForReadAsync();
        Task<System.IO.Stream> OpenStreamForWriteAsync();
    }
}
