using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Collections.Generic;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class CombinedItem : IFileItem
    {
        public CombinedItem(params IFileItem[] contents)
        {
            Contents = new ObservableCollection<IFileItem>(contents) ?? throw new ArgumentNullException(nameof(contents));

            string commonName = null;
            foreach(var item in Contents)
            {
                commonName = commonName ?? item.Name;
                if (string.Compare(commonName, item.Name, true) != 0)
                    goto named;
            }
            this.Name = commonName;
        named:;
            this.Name = this.Name ?? "Combined";
        }

        public ObservableCollection<IFileItem> Contents { get; } = new ObservableCollection<IFileItem>();

        public string Name { get; set; } = "";

        public string Path { get; set; } = "";

        public DateTimeOffset DateCreated => Contents?.Aggregate(DateTimeOffset.MinValue, (a, b) => a > b.DateCreated ? a : b.DateCreated) ?? DateTimeOffset.MinValue;

        public bool IsFolder => true;

        public ICommand DeleteCommand => null;

        public ICommand RenameCommand => null;

        public async Task<ObservableCollection<IFileItem>> GetChildren()
        {
            if (Contents == null) return null;
            var result = new List<IFileItem>();
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
            return new ObservableCollection<IFileItem>(result.GroupBy(a => a.Name.ToLower()).Select(a =>
             {
                 var b = a.ToArray();
                 if (b.Length == 0) return b[0];
                 return new CombinedItem(b);
             }).OrderBy(a => a.Name));
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
