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
        Task<bool> UpdateRequiredAsync();
    }

    public interface IPageOptions
    {
        double TargetWidth { get; }
        double TargetHeight { get; }
        event EventHandler Updated;
    }

    public class PageOptions : IPageOptions
    {
        public double TargetWidth { get { return _TargetWidth; } set { _TargetWidth = value; OnUpdated(new EventArgs()); } }
        private double _TargetWidth;
        public double TargetHeight { get { return _TargetHeight; } set { _TargetHeight = value; OnUpdated(new EventArgs()); } }
        private double _TargetHeight;

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e) { if (Updated != null) Updated(this, e); }
    }

    public class PageOptionsControl : IPageOptions
    {
        public double TargetWidth { get { return TargetControl.ActualWidth; } }
        public double TargetHeight { get { return TargetControl.ActualHeight; } }
        public Windows.UI.Xaml.Controls.Control TargetControl
        {
            get { return _TargetControl; }
            set
            {
                _TargetControl.SizeChanged -= Control_SizeChanged;
                _TargetControl = value;
                _TargetControl.SizeChanged += Control_SizeChanged;
                OnUpdated(new EventArgs());

            }
        }
        private Windows.UI.Xaml.Controls.Control _TargetControl;

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e) { if (Updated != null) Updated(this, e); }

        public PageOptionsControl(Windows.UI.Xaml.Controls.Control control)
        {
            this._TargetControl = control;

            control.SizeChanged += Control_SizeChanged;
        }

        private void Control_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            OnUpdated(new EventArgs());
        }

        public static explicit operator PageOptions(PageOptionsControl item)
        {
            return new PageOptions() { TargetWidth = item.TargetWidth, TargetHeight = item.TargetHeight };
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

        public async Task<bool> UpdateRequiredAsync()
        {
            return await(await GetPage()).UpdateRequiredAsync();
        }
    }

}
