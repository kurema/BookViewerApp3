using System;

namespace BookViewerApp.Managers;

public static class ImageManager
{
    public static class Extensions
    {
        public const string Jpeg1 = ".jpeg";
        public const string Jpeg2 = ".jpg";
        public const string Gif = ".gif";
        public const string Png = ".png";
        public const string Bitmap = ".bmp";
        public const string Tiff1 = ".tiff";
        public const string Tiff2 = ".tif";
        public const string JpegXr1 = ".hdp";
        public const string JpegXr2 = ".wdp";
        public const string JpegXr3 = ".jxr";
        public const string Avif = ".avif";
        public const string Webp = ".webp";
    }

    public static string[] AvailableExtensionsRead
    {
        get
		{
			return [Extensions.Jpeg1, Extensions.Jpeg2, Extensions.Gif, Extensions.Png, Extensions.Bitmap, Extensions.Tiff1, Extensions.Tiff2, Extensions.JpegXr1, Extensions.JpegXr2, Extensions.JpegXr3, Extensions.Avif,Extensions.Webp];
        }
    }

    public static Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat? GetBitmapFileFormatFromExtension(string ext)
    {
        ext = ext.ToLowerInvariant();
        switch (ext)
        {
            case Extensions.Jpeg1:
            case Extensions.Jpeg2:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Jpeg;
            case Extensions.Png:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Png;
            case Extensions.Bitmap:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Bmp;
            case Extensions.JpegXr1:
            case Extensions.JpegXr2:
            case Extensions.JpegXr3:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.JpegXR;
            case Extensions.Gif:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Gif;
            case Extensions.Tiff1:
            case Extensions.Tiff2:
                return Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Tiff;
            default:
                return null;
        }
    }

    public static Microsoft.Graphics.Canvas.CanvasBitmapFileFormat? GetCanvasBitmapFileFormatFromExtension(string ext)
    {
        ext = ext.ToLowerInvariant();
        switch (ext)
        {
            case Extensions.Jpeg1:
            case Extensions.Jpeg2:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Jpeg;
            case Extensions.Png:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Png;
            case Extensions.Bitmap:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Bmp;
            case Extensions.JpegXr1:
            case Extensions.JpegXr2:
            case Extensions.JpegXr3:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.JpegXR;
            case Extensions.Gif:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Gif;
            case Extensions.Tiff1:
            case Extensions.Tiff2:
                return Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Tiff;
            default:
                return null;
        }
    }

    public static Guid? GetBitmapEncoderGuidFromExtension(string ext)
    {
        ext = ext.ToLowerInvariant();
        switch (ext)
        {
            case Extensions.Jpeg1:
            case Extensions.Jpeg2:
                return Windows.Graphics.Imaging.BitmapEncoder.JpegEncoderId;
            case Extensions.Png:
                return Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId;
            case Extensions.Bitmap:
                return Windows.Graphics.Imaging.BitmapEncoder.BmpEncoderId;
            case Extensions.JpegXr1:
            case Extensions.JpegXr2:
            case Extensions.JpegXr3:
                return Windows.Graphics.Imaging.BitmapEncoder.JpegXREncoderId;
            case Extensions.Gif:
                return Windows.Graphics.Imaging.BitmapEncoder.GifEncoderId;
            case Extensions.Tiff1:
            case Extensions.Tiff2:
                return Windows.Graphics.Imaging.BitmapEncoder.TiffEncoderId;
			default:
                return null;
        }
    }

    public static (string description, string[] extensions)[] GetFileExtensionsEncodable()
    {
        return new[] {
            ("PNG",new[]{Extensions.Png}),
            ("JPEG",new[]{Extensions.Jpeg1,Extensions.Jpeg2}),
            ("Windows bitmap",new[]{Extensions.Bitmap}),
            ("GIF",new[]{Extensions.Gif}),
            ("TIFF",new[]{Extensions.Tiff1,Extensions.Tiff2}),
            ("JPEG XR",new[]{Extensions.JpegXr1,Extensions.JpegXr2,Extensions.JpegXr3}),
        };
    }
}
