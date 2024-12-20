﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Compression;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using SharpCompress.Archives;

#nullable enable
namespace BookViewerApp.Books
{
	public class CbzBook : IBookFixed, ITocProvider, IDisposableBasic, IExtraEntryProvider
	{
		public ZipArchive? Content { get; private set; }
		public ZipArchiveEntry[] AvailableEntries = new ZipArchiveEntry[0];

		public IEnumerable<string> EntriesGeneral { get; private set; } = Array.Empty<string>();
		public Func<Task<IArchive?>>? ArchiveProvider { get; private set; } = null;


		public uint PageCount
		{
			get
			{
				return (uint)(AvailableEntries?.Length ?? 0);
			}
		}

		public string ID
		{
			get
			{
				if (_IDCache != null) return _IDCache;
				string result = "";
				foreach (var item in AvailableEntries.OrderBy((a) => a.FullName))
				{
					result += Functions.CombineStringAndDouble(item.Name, item.Length);
				}
				return _IDCache = Functions.GetHash(result);
			}
		}

		public TocItem[] Toc { get; private set; } = new TocItem[0];

		private string? _IDCache = null;



		public event EventHandler? Loaded;

		private void OnLoaded(EventArgs e)
		{
			Loaded?.Invoke(this, e);
		}

		public IPageFixed GetPage(uint i)
		{
			return new CbzPage(AvailableEntries[i]);
		}

		private Stream? DisposableStream;

		public async Task LoadAsync(Func<Task<Stream>> streamProvider)
		{
			await Task.Run(async () =>
			{
				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				try
				{
					var stream = await (streamProvider?.Invoke() ?? Task.FromResult<Stream>(null!));
					if (stream is null) return;
					//現状日本語の場合はZIPファイルをCP932で読むという雑なハックをしているが、たまにUTF-8の場合もある。
					//事前に調べる方法はなさそうだし、パフォーマンスに影響が出る処理を入れたくはない。
					//なのでこのまま。ほとんどの日本語ファイルはCP932なので妥協。
					//ちなみにこれはソートと目次機能で使われる。ソートに影響はないはず。
					Content = new ZipArchive(stream, ZipArchiveMode.Read, false,
						System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ?
						Encoding.GetEncoding(932) : Encoding.UTF8);
					//Content = new ZipArchive(stream, ZipArchiveMode.Read, false);
					DisposableStream = stream;
					OnLoaded(new EventArgs());
				}
				catch { }
			}
			);

			if (Content is null) { return; }

			{
				ArchiveProvider = async () =>
				{
					var stream = await (streamProvider?.Invoke() ?? Task.FromResult<Stream>(null!));
					if (stream is null) return null;
					return ArchiveFactory.Open(stream);
				};
				EntriesGeneral = Content.Entries.Select(a => a.FullName)?.ToArray() ?? Array.Empty<string>();
			}


			var entries = new List<ZipArchiveEntry>();
			string[] supportedFile = ImageManager.AvailableExtensionsRead;
			var cm = Content.Mode;
			var files = Content.Entries;
			foreach (var file in files)
			{
				var s = Path.GetExtension(file.Name).ToLowerInvariant();
				var b = supportedFile.Contains(Path.GetExtension(file.Name).ToLowerInvariant());
				if (supportedFile.Contains(Path.GetExtension(file.Name).ToLowerInvariant()))
				{
					entries.Add(file);
				}
			}

			entries = Functions.SortByArchiveEntry(entries, (a) => a.FullName).ToList();

			{
				//toc関係
				var toc = new List<TocItem>();
				for (int i = 0; i < entries.Count; i++)
				{
					var dirs = Path.GetDirectoryName(entries[i].FullName).Split(Path.DirectorySeparatorChar).ToList();
					dirs.Add(".");

					var ctoc = toc;
					TocItem? lastitem = null;
					for (int j = 0; j < dirs.Count; j++)
					{
						dirs[j] = dirs[j] == "" ? "." : dirs[j];
						if (ctoc.Count == 0 || ctoc.Last().Title != dirs[j])
						{
							ctoc.Add(new TocItem() { Children = new TocItem[0], Title = dirs[j], Page = i });
						}

						if (lastitem != null) lastitem.Children = ctoc.ToArray();
						lastitem = ctoc.Last();
						ctoc = lastitem.Children.ToList();
					}
				}
				Toc = toc.ToArray();
			}

			AvailableEntries = entries.ToArray();
		}

		public void DisposeBasic()
		{
			Content?.Dispose();
			Content = null;
			DisposableStream?.Close();
			DisposableStream?.Dispose();
			DisposableStream = null;
		}

		public IPageFixed? GetPageCover()
		{
			if (AvailableEntries is null || AvailableEntries?.Length == 0) return null;
			var cover = this.AvailableEntries.FirstOrDefault(a => Functions.IsCover(a.Name));
			return cover is null ? GetPage(0) : new CbzPage(cover);
		}
	}
}

namespace BookViewerApp.Books
{
	public class CbzPage : IPageFixed, IDisposableBasic
	{
		private readonly ZipArchiveEntry Content;

		public CbzPage(ZipArchiveEntry entry)
		{
			Content = entry;
			Cache = new MemoryStreamCache()
			{
				MemoryStreamProvider = async (th) =>
				  {
					  using var s = Content.Open();
					  return await th.GetMemoryStreamAsync(s);
				  }
			};
		}

		public async Task<BitmapImage?> GetBitmapAsync()
		{
			if (Cache is null) return null;
			return await new ImagePageStream((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream()).GetBitmapAsync();
		}

		private MemoryStreamCache? Cache;

		public string Title => Content.Name;
		public string Path => Content.FullName;

		public Task<bool> UpdateRequiredAsync(double width, double height)
		{
			return Task.FromResult(false);
		}

		public async Task<bool> SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? croppedRegionRelative = null)
		{
			if (Cache is null) return false;
			try
			{
				using var fileThumb = await file.OpenAsync(FileAccessMode.ReadWrite);
				await Functions.ResizeImage((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream(), fileThumb, width, croppedRegionRelative: croppedRegionRelative, extractAction: () => { Content.ExtractToFile(file.Path, true); });
			}
			catch (Exception)
			{
				try
				{ await file.DeleteAsync(); }
				catch { }
				return false;
			}
			return true;
		}

		public async Task<bool> SetBitmapAsync(BitmapSource image, double width, double height)
		{
			if (Cache is null) return false;
			try
			{
				await new ImagePageStream((await Cache.GetMemoryStreamByProviderAsync()).AsRandomAccessStream()).SetBitmapAsync(image, width, height);
				return true;
			}
			catch
			{
				// When it's password protected, System.IO.InvalidDataException is thrown. Password is currently not supported. UWP can't use .NET 7.
				//https://github.com/dotnet/runtime/issues/1545
				//https://github.com/dotnet/runtime/issues/57197
				return false;
			}
		}

		public void DisposeBasic()
		{
			Cache?.DisposeBasic();
			Cache = null;
		}
	}

}
