using BookViewerApp.Storages.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace kurema.FileExplorerControl.Models.FileItems
{
    //Not used
    public class BookmarkItem : IFileItem
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public bool IsFolder => false;

        public ICommand DeleteCommand { get; set; } = null;

        public object Tag { get; set; }

        private ICommand _RenameCommand;
        public ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((parameter) => { Name = parameter?.ToString() ?? Name; });

        public event Windows.Foundation.TypedEventHandler<BookmarkItem, string> Opened;

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public string FileTypeDescription => BookViewerApp.Managers.ResourceManager.Loader.GetString("ItemType/BookmarkItem");

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
            Opened?.Invoke(this, Path);
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
    public class StorageBookmarkItem : IFileItem, IStorageBookmark
    {
        public libraryBookmarksContainerBookmark Content;

        public StorageBookmarkItem(libraryBookmarksContainerBookmark content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public object Tag { get; set; }

        public bool IsReadOnly { get; set; } = false;

        public string FileTypeDescription => BookViewerApp.Managers.ResourceManager.Loader.GetString("ItemType/BookmarkItem");

        public string Name { get => Content.title; set => Content.title = value; }

        public string Path { get; set; } = "";

        public string TargetUrl { get => Content.url; set => Content.url = value; }

        public DateTimeOffset DateCreated
        {
            get => new DateTimeOffset(Content.created).ToLocalTime();
            set => Content.created = value.UtcDateTime;
        }

        public bool IsFolder => false;

        private ICommand _DeleteCommand;

        public ICommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(a => ActionDelete?.Invoke(), a => !(a is bool b && b == true) && ActionDelete != null && !IsReadOnly);

        private ICommand _RenameCommand;
        public ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((a) => this.Name = a.ToString(), a => !IsReadOnly);

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            return Task.FromResult(new ObservableCollection<IFileItem>());
        }

        public Action<string> ActionOpen { get; set; }

        public Action ActionDelete { get; set; }

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }


        public Task<ulong?> GetSizeAsync()
        {
            return Task.FromResult((ulong?)GetBytes().Length);
        }

        public void Open()
        {
            ActionOpen?.Invoke(this.TargetUrl);
        }

        public Task<Stream> OpenStreamForReadAsync()
        {
            //Mimic .url file.
            //It has no maening and is not tested yet.
            return Task.FromResult<Stream>(new MemoryStream(GetBytes()));
        }

        public Task<Stream> OpenStreamForWriteAsync()
        {
            return Task.FromResult<Stream>(null);
        }

        private Byte[] GetBytes() => Encoding.ASCII.GetBytes("[InternetShortcut]\nURL=" + System.Web.HttpUtility.UrlEncode(Path));
    }

    public interface IStorageBookmark : IFileItem
    {
        bool IsReadOnly { get; set; }
        Action<string> ActionOpen { get; set; }
        Action ActionDelete { get; set; }
        Func<IFileItem, MenuCommand[]> MenuCommandsProvider { set; }
    }

    public class StorageBookmarkContainer : IFileItem, IStorageBookmark
    {
        public libraryBookmarksContainer Content;

        public StorageBookmarkContainer(libraryBookmarksContainer content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public bool IsReadOnly { get; set; } = false;

        public object Tag { get; set; }

        public string Name { get => Content.title; set => Content.title = value; }

        public string FileTypeDescription => BookViewerApp.Managers.ResourceManager.Loader.GetString("ItemType/BookmarkContainer");

        public string Path { get; set; }

        public DateTimeOffset DateCreated => Content.created;

        public bool IsFolder => true;

        private ICommand _DeleteCommand;
        public ICommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(a => ActionDelete?.Invoke(), a => !(a is bool b && b == true) && ActionDelete != null && !IsReadOnly);

        private ICommand _RenameCommand;
        public ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((a) => this.Name = a.ToString(), a => !IsReadOnly);

        public Action ActionDelete { get; set; }
        public Action<string> ActionOpen { get; set; }

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            var result = new List<IFileItem>();
            if (Content.Items == null) return Task.FromResult(new ObservableCollection<IFileItem>(result));
            foreach (var item in Content.Items)
            {
                IStorageBookmark bookmark = null;
                switch (item)
                {
                    case libraryBookmarksContainer bc:
                        bookmark = new StorageBookmarkContainer(bc);
                        break;
                    case libraryBookmarksContainerBookmark bcb:
                        bookmark = new StorageBookmarkItem(bcb);
                        break;
                }
                bookmark.ActionOpen = this.ActionOpen;
                bookmark.ActionDelete = () =>
                {
                    var list = Content.Items.ToList();
                    list.Remove(item);
                    Content.Items = list.ToArray();
                };
                bookmark.MenuCommandsProvider = this.MenuCommandsProvider;
                bookmark.IsReadOnly = IsReadOnly;
                result.Add(bookmark);
            }
            return Task.FromResult(new ObservableCollection<IFileItem>(result));
        }

        public Task<ulong?> GetSizeAsync()
        {
            return Task.FromResult<ulong?>(null);
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
