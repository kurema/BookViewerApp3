﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace kurema.FileExplorerControl.Models
{
    public interface IFileItem
    {
        Task<ObservableCollection<IFileItem>> GetChildren();
        string FileName { get; }
        void Open();

        DateTimeOffset DateCreated { get; }

        bool IsFolder { get; }
    }
}
