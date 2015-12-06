using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Books
{
    public interface IBookFixed
    {
        uint PageCount { get; }
        IPage GetPage(uint i);
    }

    public interface IPage
    {
    }

    public interface IPageUrl : IPage
    {
        Uri Uri { get; }
    }

    public interface IPageStream : IPage
    {
        Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream);
        Task PreparePageAsync();
    }
}
