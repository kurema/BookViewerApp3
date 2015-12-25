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
        IPageOptions Option { get; set; }
        Task<Windows.UI.Xaml.Media.ImageSource> GetImageSourceAsync();
    }

    public interface IPageOptions
    {
        double TargetWidth { get; }
        double TargetHeight { get; }
    }

    public class PageOptions : IPageOptions
    {
        public double TargetWidth { get; set; }
        public double TargetHeight { get; set; }
    }

    public class PageOptionsControl : IPageOptions
    {
        public double TargetWidth { get { return TargetControl.ActualWidth; } }
        public double TargetHeight { get { return TargetControl.ActualHeight; } }
        public Windows.UI.Xaml.Controls.Control TargetControl { get; set; }

        public PageOptionsControl(Windows.UI.Xaml.Controls.Control control)
        {
            this.TargetControl = control;
        }

    }

    public class VirtualPage : IPage
    {
        private Func<IPage> accessor;

        private IPage PageCache = null;

        public IPageOptions Option
        {
            get { if (PageCache == null) return _Option; else return PageCache.Option; }
            set { if (PageCache == null) _Option = value; else { PageCache.Option = Option; this._Option = value; } }
        }
        private IPageOptions _Option = new PageOptions();

        public VirtualPage(Func<IPage> accessor)
        {
            this.accessor = accessor;
        }

        public async Task<IPage> GetPage()
        {
            if (PageCache == null)
                return await Task.Run(() => { PageCache = accessor(); PageCache.Option = this.Option; return PageCache; });
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
