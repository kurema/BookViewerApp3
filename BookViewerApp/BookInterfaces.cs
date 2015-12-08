using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Books
{
    public interface IBook
    {
        event EventHandler Loaded;
    }

    public interface IBookFixed:IBook
    {
        uint PageCount { get; }
        IPage GetPage(uint i);
    }

    public interface IPage
    {
        Task<Windows.UI.Xaml.Media.ImageSource> GetImageSourceAsync();
    }

    public interface IPageUrl : IPage
    {
        Uri Uri { get; }
    }

    public interface IPageSourceStream : IPage
    {
        Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream);
        Task PreparePageAsync();
    }

    public interface IPageFileStream : IPage
    {
        System.IO.Stream Open();
        void Close();
    }

}
