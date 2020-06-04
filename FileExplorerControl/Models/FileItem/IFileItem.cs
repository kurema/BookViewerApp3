using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public interface IFileItem
    {
        event EventHandler Updated;
        void OnUpdate();

        Task<ObservableCollection<IFileItem>> GetChildren();
        string Name { get; }
        string Path { get; }
        string FileTypeDescription { get; }
        void Open();

        DateTimeOffset DateCreated { get; }
        Task<ulong?> GetSizeAsync();

        bool IsFolder { get; }

        System.Windows.Input.ICommand DeleteCommand { get; }
        System.Windows.Input.ICommand RenameCommand { get; }

        Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; }
        

        Task<System.IO.Stream> OpenStreamForReadAsync();
        Task<System.IO.Stream> OpenStreamForWriteAsync();

        object Tag { get; set; }
    }

    public interface IIconProviderProvider
    {
        IconProviders.IIconProvider Icon { get; set; }
    }
}
