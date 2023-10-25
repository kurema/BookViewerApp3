using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

using System.Runtime.InteropServices.WindowsRuntime;

using BookViewerApp.Helper;
using BookViewerApp.Managers;
using BookViewerApp.Storages;
using SharpCompress.Archives;

#nullable enable
namespace BookViewerApp.Books
{
	public class CompressedBook : IBookFixed, ITocProvider, IDisposableBasic, IExtraEntryProvider
	{
		public string ID
		{
			get
			{
				return IDCache ??= GetID();
			}
		}

		private string? IDCache = null;
		private string GetID()
		{
			string result = "";
			foreach (var item in Entries.OrderBy((a) => a.Key))
			{
				result += Functions.CombineStringAndDouble(item.Key, item.Size);
			}
			return Functions.GetHash(result);
		}

		public uint PageCount => (uint)(Entries?.Length ?? 0);

		public event EventHandler? Loaded;
		private void OnLoaded()
		{
			Loaded?.Invoke(this, new EventArgs());
		}

		public TocItem[] Toc { get; private set; } = new TocItem[0];

		public IEnumerable<string> EntriesGeneral { get; private set; } = Array.Empty<string>();
		public Func<Task<IArchive?>>? ArchiveProvider { get; private set; } = null;

		//private SharpCompress.Archive.IArchiveEntry Target;
		private SharpCompress.Archives.IArchiveEntry[] Entries = new SharpCompress.Archives.IArchiveEntry[0];

		private SharpCompress.Archives.IArchive? DisposableContent;//To Dispose
		private Stream? DisposableStream;

		public async Task LoadAsync(Stream stream)
		{
			await LoadAsync(() => Task.FromResult(stream));
		}

		public async Task LoadAsync(Func<Task<Stream>> streamProvider)
		{
			SharpCompress.Archives.IArchive archive;
			await Task.Run(async () =>
			{
				try
				{
					Stream sr;
					{
						var temp = streamProvider?.Invoke();
						if (temp is null) return;
						sr = await temp;
					}

					try
					{
						DisposableContent = archive = SharpCompress.Archives.ArchiveFactory.Open(sr);
					}
					catch
					{
						// No exception even if it's encrypted.
						throw;
					}
					{
						ArchiveProvider = async () =>
						{
							var stream = await (streamProvider?.Invoke() ?? Task.FromResult<Stream>(null!));
							if (stream is null) return null;
							return ArchiveFactory.Open(stream);
						};
						EntriesGeneral = archive.Entries.Select(a => a.Key)?.ToArray() ?? Array.Empty<string>();
					}
					DisposableStream = sr;
					var entries = new List<SharpCompress.Archives.IArchiveEntry>();
					foreach (var entry in archive.Entries)
					{
						if (!entry.IsDirectory && !entry.IsEncrypted)
						{
							if (ImageManager.AvailableExtensionsRead.Contains(Path.GetExtension(entry.Key).ToLowerInvariant()))
							{
								entries.Add(entry);
							}
						}
					}

					entries = Functions.SortByArchiveEntry(entries, (a) => a.Key).ToList();

					{
						//toc関係
						var toc = new List<TocItem>();
						for (int i = 0; i < entries.Count; i++)
						{
							var dirs = Path.GetDirectoryName(entries[i].Key).Split(Path.DirectorySeparatorChar).ToList();
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

					Entries = entries.ToArray();
					OnLoaded();
				}
				catch { Entries = new SharpCompress.Archives.IArchiveEntry[0]; }
			});
		}

		public IPageFixed GetPage(uint i)
		{
			return new CompressedPage(Entries[i]);
		}

		public void DisposeBasic()
		{
			DisposableContent?.Dispose();
			DisposableContent = null;
			DisposableStream?.Close();
			DisposableStream?.Dispose();
			DisposableStream = null;
		}

		public IPageFixed? GetPageCover()
		{
			if (Entries is null || Entries?.Length == 0) return null;
			var cover = this.Entries.FirstOrDefault(a => Functions.IsCover(a.Key));
			return cover is null ? GetPage(0) : new CompressedPage(cover);
		}
	}
}

namespace BookViewerApp.Books
{
	public class CompressedPage : IPageFixed, IDisposableBasic
	{
		public IPageOptions? Option
		{
			get; set;
		}

		public string Title { get; private set; }
		public string Path { get; private set; }

		//private SharpCompress.Archives.IArchiveEntry Entry;

		public CompressedPage(SharpCompress.Archives.IArchiveEntry Entry)
		{
			//this.Entry = Entry;
			Title = System.IO.Path.GetFileName(Entry.Key);
			Path = Entry.Key;
			Cache = new MemoryStreamCache()
			{
				MemoryStreamProvider = async (th) =>
				{
					using var s = Entry.OpenEntryStream();
					return await th.GetMemoryStreamAsync(s);
				}
			};
		}

		private readonly MemoryStreamCache Cache;

		public async Task<bool> SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? croppedRegionRelative = null)
		{
			try
			{
				//await Functions.SaveStreamToFile(GetStream(), file);
				using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
				var ms = await Cache.GetMemoryStreamByProviderAsync();
				await Functions.ResizeImage(ms.AsRandomAccessStream(), fileStream, width, croppedRegionRelative: croppedRegionRelative, extractAction: async () =>
				  {
					  using var s = await Cache.GetMemoryStreamByProviderAsync();
					  var buffer = new byte[s.Length];
					  await s.ReadAsync(buffer, 0, (int)s.Length);
					  var ibuffer = buffer.AsBuffer();
					  await fileStream.WriteAsync(ibuffer);
				  });
				return true;
			}
			catch (Exception)
			{
				try
				{
					await file.DeleteAsync();
				}
				catch { }
				return false;
			}
		}


		public async Task<bool> SetBitmapAsync(BitmapSource image, double width, double height)
		{
			var stream = await Cache.GetMemoryStreamByProviderAsync();
			if (stream is null) return false;
			try
			{
				Task? task = new ImagePageStream(stream.AsRandomAccessStream())?.SetBitmapAsync(image, width, height);
				if (task != null) await task; else return false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public Task<bool> UpdateRequiredAsync(double width, double height)
		{
			return Task.FromResult(false);
		}

		public void DisposeBasic()
		{
			Cache?.DisposeBasic();
		}
	}

}
