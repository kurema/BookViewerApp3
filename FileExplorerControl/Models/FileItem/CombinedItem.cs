using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class CombinedItem : IFileItem
    {
        public ObservableCollection<IFileItem> Contents { get; } = new ObservableCollection<IFileItem>();

        public string FileName { get; set; }

        public string Path { get; set; } = "";

        public DateTimeOffset DateCreated => Contents?.Aggregate(DateTimeOffset.MinValue, (a, b) => a > b.DateCreated ? a : b.DateCreated) ?? DateTimeOffset.MinValue;

        public bool IsFolder => true;

        public ICommand DeleteCommand => null;

        public ICommand RenameCommand => null;

        public async Task<ObservableCollection<IFileItem>> GetChildren()
        {
            if (Contents == null) return null;
            var result = new ObservableCollection<IFileItem>();
            foreach (var item in Contents)
            {
                if (item.IsFolder)
                {
                    var children = await item.GetChildren();
                    foreach (var item2 in children)
                    {
                        result.Add(item2);
                    }
                }
                else
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public async Task<ulong?> GetSizeAsync()
        {
            ulong? result = 0;
            foreach(var item in Contents)
            {
                var val = await item.GetSizeAsync();
                if (val == null) return null;
                result += val;
            }
            return result;
        }

        public void Open()
        {
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
