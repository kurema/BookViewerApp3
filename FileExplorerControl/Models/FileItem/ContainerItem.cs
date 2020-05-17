using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class ContainerItem : IFileItem, IIconProviderProvider
    {
        public ContainerItem(string fileName, string path, params IFileItem[] children)
        {
            Name = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Children = new ObservableCollection<IFileItem>(children ?? throw new ArgumentNullException(nameof(children)));
        }

        public ContainerItem(string fileName, string path, ObservableCollection<IFileItem> children)
        {
            Name = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }


        /// <summary>
        /// Only use DefaultIconLarge/DefaultIconSmall.
        /// </summary>
        public IconProviders.IIconProvider Icon { get; set; }

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public object Tag { get; set; }

        public DateTimeOffset DateCreated => DateTimeOffset.Now;

        public bool IsFolder => true;

        public ObservableCollection<IFileItem> Children { get; } = new ObservableCollection<IFileItem>();

        public ICommand DeleteCommand { get; set; } = null;

        public ICommand RenameCommand { get; set; } = null;

        public string FileTypeDescription { get; set; } = "";

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
