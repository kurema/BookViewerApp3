using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

#nullable enable
namespace BookViewerApp.Books
{
    public interface IBook
    {
        event EventHandler? Loaded;
        string? ID { get; }
    }

    public interface IBookFixed : IBook
    {
        uint PageCount { get; }
        IPageFixed? GetPage(uint i);
    }

    public interface IPageFixed
    {
        //IPageOptions? Option { get; set; }
        //Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync();
        Task SetBitmapAsync(BitmapSource image, double width, double height);
        Task<bool> UpdateRequiredAsync(double width, double height);
        Task SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? croppedRegionRelative = null);
    }

    public interface IPageOptions : INotifyPropertyChanged
    {
        double TargetWidth { get; }
        double TargetHeight { get; }
    }

    public class BookEpub : IBook
    {
        public BookEpub(IStorageFile file)
        {
            File = file;
        }

        public string ID => Guid.NewGuid().ToString();

#pragma warning disable 0067
        public event EventHandler? Loaded;
#pragma warning restore 0067

        public IStorageFile File { get; set; }
    }

    public class PageOptions : IPageOptions
    {
        public double TargetWidth { get { return _TargetWidth; } set { _TargetWidth = value; OnPropertyChanged(nameof(TargetWidth)); } }
        private double _TargetWidth;
        public double TargetHeight { get { return _TargetHeight; } set { _TargetHeight = value; OnPropertyChanged(nameof(TargetHeight)); } }
        private double _TargetHeight;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string name) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name)); }

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

    public class VirtualPage : IPageFixed, Helper.IDisposableBasic
    {
        private Func<IPageFixed> accessor;

        private IPageFixed? PageCache = null;

        //public IPageOptions? Option
        //{
        //    get { if (PageCache == null || !(PageCache is PdfPage pdf)) return _Option; else return pdf.Option; }
        //    set { if (PageCache == null || !(PageCache is PdfPage pdf)) _Option = value; else { pdf.Option = Option; this._Option = value; } }
        //}
        //private IPageOptions? _Option = new PageOptions();

        public VirtualPage(Func<IPageFixed> accessor)
        {
            this.accessor = accessor;
        }

        public async Task<IPageFixed> GetPage()
        {
            if (PageCache is null)
                return await Task.Run(() =>
                {
                    PageCache = accessor();
                    //PageCache.Option = this.Option;
                    return PageCache;
                });
            else
                return PageCache;
        }

        //public async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync()
        //{
        //    var body = await GetPage();
        //    return await body.GetBitmapAsync();
        //}

        public async Task<bool> UpdateRequiredAsync(double width, double height)
        {
            return await (await GetPage()).UpdateRequiredAsync(width, height);
        }

        public async Task SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? Clip = null)
        {
            await (await GetPage()).SaveImageAsync(file, width, Clip);
        }

        public async Task SetBitmapAsync(BitmapSource image, double width, double height)
        {
            var body = await GetPage();
            await body.SetBitmapAsync(image, width, height);
        }

        public void DisposeBasic()
        {
            (PageCache as IDisposable)?.Dispose();
            (PageCache as Helper.IDisposableBasic)?.DisposeBasic();
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

        public string? ID => Origin.ID;

        public uint PageCount => Origin.PageCount;

        public event EventHandler? Loaded;
        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public IPageFixed? GetPage(uint i)
        {
            return Origin.GetPage(Origin.PageCount - i - 1);
        }
    }

    public interface ITocProvider
    {
        TocItem[] Toc { get; }
    }

    public class TocItem
    {
        public TocItem[]? Children { get; set; }
        public string Title { get; set; } = "";
        public int Page { get; set; }
    }

    public interface IPasswordPdovider
    {
        string? Password { get; }
        bool PasswordRemember { get; }
    }

    public interface IDirectionProvider
    {
        Direction Direction { get; }

    }

    public enum Direction
    {
        Default, R2L, L2R
    }

}
