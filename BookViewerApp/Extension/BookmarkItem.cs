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

    public class StorageBookmarkItem : IFileItem
    {
        public libraryBookmarksContainerBookmark Content;

        public StorageBookmarkItem(libraryBookmarksContainerBookmark content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public object Tag { get; set; }

        public string Name { get => Content.title; set => Content.title = value; }

        public string Path { get; set; } = "";

        public string TargetUrl { get => Content.url; set => Content.url = value; }

        public DateTimeOffset DateCreated { get => new DateTimeOffset(Content.created).ToLocalTime();
            set => Content.created = value.UtcDateTime; }

        public bool IsFolder => false;

        private ICommand _DeleteCommand;

        public ICommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(a => ActionDelete?.Invoke(), a => ActionDelete != null);

        private ICommand _RenameCommand;
        public ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((a) => this.Name = a.ToString());

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

    public class StorageBookmarkContainer : IFileItem
    {
        public libraryBookmarksContainer Content;

        public StorageBookmarkContainer(libraryBookmarksContainer content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public object Tag { get; set; }

        public string Name { get => Content.title; set => Content.title = value; }

        public string Path { get; set; }

        public DateTimeOffset DateCreated => Content.created;

        public bool IsFolder => true;

        private ICommand _DeleteCommand;
        public ICommand DeleteCommand => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateCommand(a => ActionDelete?.Invoke(), a => ActionDelete != null);

        private ICommand _RenameCommand;
        public ICommand RenameCommand => _RenameCommand = _RenameCommand ?? new Helper.DelegateCommand((a) => this.Name = a.ToString());

        public Action ActionDelete { get; set; }
        public Action<string> ActionOpen { get; set; }

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }

        public Task<ObservableCollection<IFileItem>> GetChildren()
        {
            var result = new List<IFileItem>();
            foreach(var item in Content.Items)
            {
                switch (item)
                {
                    case libraryBookmarksContainer bc:
                        {
                            var temp = new StorageBookmarkContainer(bc);
                            temp.ActionOpen = this.ActionOpen;
                            temp.ActionDelete = () =>
                            {
                                var list = Content.Items.ToList();
                                list.Remove(bc);
                                Content.Items = list.ToArray();
                            };
                            temp.MenuCommandsProvider = this.MenuCommandsProvider;
                            result.Add(temp);
                        }
                        break;
                    case libraryBookmarksContainerBookmark bcb:
                        {
                            var temp = new StorageBookmarkItem(bcb);
                            temp.ActionOpen = this.ActionOpen;
                            temp.ActionDelete = () =>
                            {
                                var list = Content.Items.ToList();
                                list.Remove(bcb);
                                Content.Items = list.ToArray();
                            };
                            temp.MenuCommandsProvider = this.MenuCommandsProvider;
                            result.Add(temp);
                        }
                        break;
                }
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
