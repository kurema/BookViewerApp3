using System;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows.Input;

using Windows.Storage;

namespace kurema.FileExplorerControl.Models
{
    public class StorageFileItem : IFileItem
    {
        public StorageFileItem(IStorageItem content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public IStorageItem Content { get; set; }


        public string FileName => Content.Name;

        public DateTimeOffset DateCreated => Content.DateCreated;

        public bool IsFolder => Content is StorageFolder;

        public async Task<ObservableCollection<IFileItem>> GetChildren()
        {
            if(Content is StorageFolder f)
            {
                return new ObservableCollection<IFileItem>((await f.GetItemsAsync()).Select(a => new StorageFileItem(a)));
            }
            else
            {
                return new ObservableCollection<IFileItem>();
            }
        }

        public EventHandler OpenEvent;

        public void Open()
        {
            OpenEvent?.Invoke(this, new EventArgs());
        }
    }

}
