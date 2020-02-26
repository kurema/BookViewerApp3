using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models
{
    public class ContainerItem : IFileItem
    {
        public ContainerItem(string fileName, string path, params IFileItem[] children)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Children = new ObservableCollection<IFileItem>(children) ?? throw new ArgumentNullException(nameof(children));
        }

        

        public string FileName { get; set; }

        public string Path { get; set; }

        public DateTimeOffset DateCreated => DateTimeOffset.Now;

        public bool IsFolder => true;

        public ObservableCollection<IFileItem> Children { get; } = new ObservableCollection<IFileItem>();

        public ICommand DeleteCommand => null;

        public ICommand RenameCommand => null;

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult(Children);
        }

        public Task<ulong?> GetSizeAsync()
        {
            return Task.FromResult<ulong?>(null);
        }

        public void Open()
        {
            return;
        }

        public Task<Stream> OpenStreamForReadAsync()
        {
            return Task.FromResult<Stream>(null);
        }

        public Task<Stream> OpenStreamForWriteAsync()
        {
            return Task.FromResult<Stream>(null);
        }

    }
}
