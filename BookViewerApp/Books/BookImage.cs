using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

using BookViewerApp.Helper;

#nullable enable
namespace BookViewerApp.Books
{
	public class ImagePageUrl : IPageFixed
	{
		public Uri Uri
		{
			get; private set;
		}

		public string Title => System.IO.Path.GetFileName(Uri.LocalPath);
		public string Path => Uri.AbsolutePath;

		public ImagePageUrl(Uri uri) { Uri = uri; }

		public Task<BitmapImage> GetBitmapAsync()
		{
			return Task.FromResult(new BitmapImage(Uri));
		}

		public Task<bool> UpdateRequiredAsync(double width, double height)
		{
			return Task.FromResult(false);
		}

		public async Task<bool> SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? Clip = null)
		{
			//ToDo: Fix me!
			if (Uri.IsFile)
			{
				//StorageFileのGetThumbnailAsyncを使ってます。サイズもいい加減だし明らかに微妙。
				//Clip is ignored.
				var thm = await (await StorageFile.GetFileFromApplicationUriAsync(Uri)).GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView);

				await Functions.SaveStreamToFile(thm, file);
				return true;
			}
			return false;
		}

		public Task<bool> SetBitmapAsync(BitmapSource image, double width, double height)
		{
			if (image is BitmapImage bimage)
			{
				bimage.UriSource = Uri;
			}
			else
			{
				throw new NotImplementedException();
			}
			return Task.FromResult(true);
		}
	}
}

namespace BookViewerApp.Books
{
	public class ImagePageStream : IPageFixed
	{
		private readonly IRandomAccessStream stream;

		public ImagePageStream(IRandomAccessStream stream, string? title = null, string? path = null)
		{
			this.stream = stream;
			Title = title ?? string.Empty;
			Path = path ?? string.Empty;
		}

		public string Title { get; private set; }

		public string Path { get; private set; }

		public Task<BitmapImage?> GetBitmapAsync()
		{
			var image = new BitmapImage();
			stream.Seek(0);
			image?.SetSource(stream);
			return Task.FromResult(image);
		}

		public async Task<bool> SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? Clip = null)
		{
			//Clip is ignored.
			await Functions.SaveStreamToFile(stream, file);
			return true;
		}

		public Task<bool> SetBitmapAsync(BitmapSource image, double width, double height)
		{
			if (stream is null)
			{
				return Task.FromResult(false);
			}
			stream.Seek(0);
			image?.SetSource(stream);
			return Task.FromResult(true);
		}

		public Task<bool> UpdateRequiredAsync(double width, double height)
		{
			return Task.FromResult(false);
		}

	}
}

namespace BookViewerApp.Books
{
	public class ImageBookUriCollection : IBookFixed
	{
		public Uri[] Content { get { return _Content; } private set { _Content = value; OnLoaded(new EventArgs()); } }
		private Uri[] _Content = new Uri[0];

		public ImageBookUriCollection(params Uri[] uri)
		{
			Content = uri;
		}

		public ImageBookUriCollection(params string[] uri)
		{
			var result = new Uri[uri.Length];
			for (int i = 0; i < uri.Length; i++)
			{
				result[i] = new Uri(uri[i]);
			}
			Content = result;
		}

		public uint PageCount => (uint)(Content?.Length ?? 0);

		public string ID
		{
			get
			{
				string result = "";
				foreach (var item in Content)
				{
					result += "\"" + item.GetHashCode() + "\"";
				}
				return Functions.GetHash(result);
			}
		}

		public event EventHandler? Loaded;

		private void OnLoaded(EventArgs e)
		{
			Loaded?.Invoke(this, e);
		}

		public IPageFixed GetPage(uint i)
		{
			return new ImagePageUrl(Content[i]);
		}

		public IPageFixed? GetPageCover()
		{
			var cover = Content.FirstOrDefault(a => Functions.IsCover(a.Segments?.Last()));
			return cover is null ? GetPage(0) : new ImagePageUrl(cover);
		}
	}
}
