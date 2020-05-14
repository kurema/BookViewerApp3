﻿using System;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows.Input;

using Windows.Storage;
using System.IO;

namespace kurema.FileExplorerControl.Models.FileItems
{
    public class StorageFileItem : IFileItem
    {
        public StorageFileItem(IStorageItem content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }


        private IStorageItem _Content;
        public IStorageItem Content
        {
            get => _Content; set
            {
                _Content = value;
                (RenameCommand as Helper.DelegateAsyncCommand)?.OnCanExecuteChanged();
                (DeleteCommand as Helper.DelegateAsyncCommand)?.OnCanExecuteChanged();
            }
        }

        public Func<IFileItem, MenuCommand[]> MenuCommandsProvider { get; set; }
        public Func<IFileItem, MenuCommand[]> MenuCommandsProviderCascade { get; set; }

        public string Name => Content.Name;

        public DateTimeOffset DateCreated => DateCreatedOverride ?? Content.DateCreated;

        //History用。日時を強制的に上書きする。あんまりスマートではない。
        //For history. Not smart.
        public DateTimeOffset? DateCreatedOverride { get; set; } = null;

        public bool IsFolder => Content is StorageFolder;

        public string Path => Content?.Path ?? "";

        public bool CanDelete => Content != null;

        public bool CanRename => Content != null;

        public async Task<ObservableCollection<IFileItem>> GetChildren()
        {
            if (Content is StorageFolder f)
            {
                return new ObservableCollection<IFileItem>((await f.GetItemsAsync()).Select(a => new StorageFileItem(a) { MenuCommandsProvider = this.MenuCommandsProviderCascade, MenuCommandsProviderCascade = this.MenuCommandsProviderCascade }));
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

        public async Task<Stream> OpenStreamForReadAsync()
        {
            if (Content is StorageFile file)
            {
                return await file.OpenStreamForReadAsync();
            }
            return null;
        }

        public async Task<Stream> OpenStreamForWriteAsync()
        {
            if (Content is StorageFile file)
            {
                return await file.OpenStreamForWriteAsync();
            }
            return null;
        }

        public async Task<ulong?> GetSizeAsync()
        {
            if (IsFolder)
            {
                return null;
            }
            else if (Content is StorageFile f)
            {
                var prop = await f.GetBasicPropertiesAsync();
                return prop.Size;
            }
            else
            {
                return null;
            }
        }

        private ICommand _RenameCommand;
        public ICommand RenameCommand
        {
            get
            {
                return _RenameCommand = _RenameCommand ?? new Helper.DelegateAsyncCommand(async (parameter) =>
                {
                    if (Content == null) return;
                    if (parameter == null) return;
                    try
                    {
                        await Content?.RenameAsync(parameter.ToString());
                    }
                    catch
                    {
                    }
                });
            }
            set
            {
                _RenameCommand = value;
            }
        }


        private ICommand _DeleteCommand;
        public ICommand DeleteCommand
        {
            get => _DeleteCommand = _DeleteCommand ?? new Helper.DelegateAsyncCommand(async (parameter) =>
                {
                    if (parameter is bool complete)
                    {
                        if (Content == null) return;
                        try
                        {
                            await Content?.DeleteAsync(complete ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default);
                        }
                        catch
                        {
                        }
                    }
                }, (b) => Content != null);

            set
            {
                _DeleteCommand = value;
            }
        }

        public object Tag { get; set; }
    }
}
