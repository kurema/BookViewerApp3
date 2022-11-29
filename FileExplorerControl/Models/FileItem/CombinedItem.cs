using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Collections.Generic;

namespace kurema.FileExplorerControl.Models.FileItems;

public class CombinedItem : IFileItem
{
    public CombinedItem(params IFileItem[] contents)
    {
        Contents = new ObservableCollection<IFileItem>(contents) ?? throw new ArgumentNullException(nameof(contents));

        string commonName = null;
        foreach (var item in Contents)
        {
            commonName ??= item.Name;
            if (string.Compare(commonName, item.Name, true) != 0)
                goto named;
        }
        this.Name = commonName;
    named:;
        this.Name ??= "Combined";

        //Contents.CollectionChanged += (s, e) => {
        //    void Item_ChildrenUpdated(object sender, EventArgs e2) => OnChildrenUpdated();

        //    OnChildrenUpdated();
        //    if (e.OldItems != null)
        //    {
        //        foreach(IFileItem item in e.OldItems)
        //        {
        //            item.ChildrenUpdated -= Item_ChildrenUpdated;
        //        }
        //    }
        //    if(e.NewItems != null)
        //    {
        //        foreach (IFileItem item in e.NewItems)
        //        {
        //            item.ChildrenUpdated += Item_ChildrenUpdated;
        //        }
        //    }
        //};
    }

    public ObservableCollection<IFileItem> Contents { get; } = new ObservableCollection<IFileItem>();

    public object Tag { get; set; }

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public DateTimeOffset DateCreated => Contents?.Aggregate(DateTimeOffset.MinValue, (a, b) => a > b.DateCreated ? a : b.DateCreated) ?? DateTimeOffset.MinValue;

    public bool IsFolder => true;

    public ICommand DeleteCommand { get; set; } = null;

    public ICommand RenameCommand { get; set; } = null;

    public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

    public Func<IFileItem, MenuCommand[]> MenuCommandsProviderCascade { get; set; }

    public string FileTypeDescription { get; set; } = Application.ResourceLoader.Loader.GetString("FileType/Combined");

    public string FileTypeDescriptionCascade { get; set; } = Application.ResourceLoader.Loader.GetString("FileType/Combined");

    public async Task<ObservableCollection<IFileItem>> GetChildren()
    {
        if (Contents is null) return null;
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
             if (b.Length == 1) return b[0];
             return new CombinedItem(b) { MenuCommandsProvider = this.MenuCommandsProviderCascade, MenuCommandsProviderCascade = this.MenuCommandsProviderCascade, FileTypeDescription = FileTypeDescriptionCascade };
         }).OrderBy(a => !a.IsFolder).ThenBy(a => a.Name));
    }

    public async Task<ulong?> GetSizeAsync()
    {
        ulong? result = 0;
        foreach (var item in Contents)
        {
            var val = await item.GetSizeAsync();
            if (val is null) return null;
            result += val;
        }
        return result;
    }

    public event EventHandler Updated;

    public void OnUpdate() { Updated?.Invoke(this, new EventArgs()); }

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

    public IEnumerable<IFileItem> GetSearchResults(string word)
    {
        if (Contents is null) return null;
        var result = new List<IFileItem>();
        foreach (var item in Contents)
        {
            if (item is null) continue;
            var s = item.GetSearchResults(word);
            if (s is not null) result.AddRange(s);
        }
        return result.ToArray();
    }

}
