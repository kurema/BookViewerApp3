using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace BookViewerApp.Books
{
    public interface IBook
    {
        event EventHandler Loaded;
        string ID { get; }
    }

    public interface IBookFixed:IBook
    {
        uint PageCount { get; }
        IPageFixed GetPage(uint i);
    }

    public interface IPageFixed
    {
        IPageOptions Option { get; set; }
        //Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync();
        Task SetBitmapAsync(Windows.UI.Xaml.Media.Imaging.BitmapImage image);
        Task<bool> UpdateRequiredAsync();
        Task SaveImageAsync(Windows.Storage.StorageFile file,uint width);
    }

    public interface IPageOptions:System.ComponentModel.INotifyPropertyChanged
    {
        double TargetWidth { get; }
        double TargetHeight { get; }
    }

    public class PageOptions : IPageOptions
    {
        public double TargetWidth { get { return _TargetWidth; } set { _TargetWidth = value; OnPropertyChanged(nameof(TargetWidth)); } }
        private double _TargetWidth;
        public double TargetHeight { get { return _TargetHeight; } set { _TargetHeight = value; OnPropertyChanged(nameof(TargetHeight)); } }
        private double _TargetHeight;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }
    }

    public class PageOptionsControl : IPageOptions
    {
        public double TargetWidth { get { return TargetControl.ActualWidth; } }
        public double TargetHeight { get { return TargetControl.ActualHeight; } }

        public double Scale = 1.0;

        public Windows.UI.Xaml.Controls.Control TargetControl
        {
            get { return _TargetControl; }
            set
            {
                _TargetControl.SizeChanged -= Control_SizeChanged;
                _TargetControl = value;
                _TargetControl.SizeChanged += Control_SizeChanged;
                OnPropertyChanged(nameof(TargetWidth));
                OnPropertyChanged(nameof(TargetHeight));
            }
        }
        private Windows.UI.Xaml.Controls.Control _TargetControl;

        public event EventHandler Updated;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs( name)); }

        public PageOptionsControl(Windows.UI.Xaml.Controls.Control control)
        {
            this._TargetControl = control;

            control.SizeChanged += Control_SizeChanged;
        }

        private void Control_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TargetWidth));
            OnPropertyChanged(nameof(TargetHeight));
        }

        public static explicit operator PageOptions(PageOptionsControl item)
        {
            return new PageOptions() { TargetWidth = item.TargetWidth, TargetHeight = item.TargetHeight };
        }
    }

    public class VirtualPage : IPageFixed
    {
        private Func<IPageFixed> accessor;

        private IPageFixed PageCache = null;

        public IPageOptions Option
        {
            get { if (PageCache == null) return _Option; else return PageCache.Option; }
            set { if (PageCache == null) _Option = value; else { PageCache.Option = Option; this._Option = value; } }
        }
        private IPageOptions _Option = new PageOptions();

        public VirtualPage(Func<IPageFixed> accessor)
        {
            this.accessor = accessor;
        }

        public async Task<IPageFixed> GetPage()
        {
            if (PageCache == null)
                return await Task.Run(() => { PageCache = accessor(); PageCache.Option = this.Option; return PageCache; });
            else
                return PageCache;
        }

        //public async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync()
        //{
        //    var body = await GetPage();
        //    return await body.GetBitmapAsync();
        //}

        public async Task<bool> UpdateRequiredAsync()
        {
            return await(await GetPage()).UpdateRequiredAsync();
        }

        public async Task SaveImageAsync(StorageFile file,uint width)
        {
            await (await GetPage()).SaveImageAsync(file,width);
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            var body = await GetPage();
            await body.SetBitmapAsync(image);
        }
    }

    public class ReversedBook : IBookFixed
    {
        public IBookFixed Origin { get; private set; }

        public ReversedBook(IBookFixed origin)
        {
            this.Origin = origin;
            this.Loaded += (s, e) => { OnLoaded(e); };
        }

        public string ID
        {
            get
            {
                return Origin.ID;
            }
        }

        public uint PageCount
        {
            get
            {
                return Origin.PageCount;
            }
        }

        public event EventHandler Loaded;
        private void OnLoaded(EventArgs e)
        {
            if (Loaded != null) Loaded(this, e);
        }

        public IPageFixed GetPage(uint i)
        {
            return Origin.GetPage(Origin.PageCount - i - 1);
        }
    }
}
