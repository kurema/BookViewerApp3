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
    public class BookmarkItem : IFileItem
    {
        public string FileName { get; set; }

        public string Path { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public bool IsFolder => false;

        public ICommand DeleteCommand { get; set; } = null;


        private System.Windows.Input.ICommand _RenameCommand;
        public System.Windows.Input.ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((parameter) => { this.FileName = parameter?.ToString() ?? FileName; });

        public event Windows.Foundation.TypedEventHandler<BookmarkItem, string> Opened;


        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult(new ObservableCollection<IFileItem>());
        }

        public Task<ulong?> GetSizeAsync()
        {
            return Task.FromResult<ulong?>((ulong?)Path?.Length ?? 0);
        }

        public void Open()
        {
            Opened?.Invoke(this, this.Path);
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
