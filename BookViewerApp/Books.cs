using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

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

    public class VirtualPage : IPage
    {
        private Func<IPage> accessor;

        private IPage PageCache = null;

        public VirtualPage(Func<IPage> accessor)
        {
            this.accessor = accessor;
        }

        public async Task<IPage> GetPage()
        {
            if (PageCache == null)
                return await Task.Run(() => { PageCache = accessor(); return PageCache; });
            else
                return PageCache;
        }

        public async Task<ImageSource> GetImageSourceAsync()
        {
            var body = await GetPage();
            return await body.GetImageSourceAsync();
        }
    }

}
