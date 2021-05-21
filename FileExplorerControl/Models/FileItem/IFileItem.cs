using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;

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

    public class FileItemPlaceHolder : IFileItem
    {
        public string Name =>
#if DEBUG
            "Test";
#else
            "";
#endif

        public string Path => "";

        public string FileTypeDescription => "";

        public DateTimeOffset DateCreated => new DateTimeOffset();

        public bool IsFolder => false;

        public ICommand DeleteCommand => new Helper.DelegateCommand((_) => { });

        public ICommand RenameCommand => new Helper.DelegateCommand((_) => { });

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider => (_) => new MenuCommand[0];

        private object _Tag;
        public object Tag { get => _Tag; set => _Tag = value; }

        public event EventHandler Updated;

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult(new ObservableCollection<IFileItem>());
        }

        public Task<ulong?> GetSizeAsync()
        {
            return Task.FromResult<ulong?>(null);
        }

        public void OnUpdate()
        {
            Updated?.Invoke(this,new EventArgs());
        }

        public void Open()
        {
        }

        public Task<Stream> OpenStreamForReadAsync()
        {
            return Task.FromResult<Stream>( new MemoryStream());
        }

        public Task<Stream> OpenStreamForWriteAsync()
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }
}
