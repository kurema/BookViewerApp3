using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

using System.IO.Compression;
using System.IO;

using System.Runtime.InteropServices.WindowsRuntime;

namespace BookViewerApp
{
    public class EpubResolver : Windows.Web.IUriToStreamResolver
    {
        public ZipArchive Content { get; private set; }

        public event EventHandler Loaded;
        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public async Task LoadAsync(Stream stream)
        {
            await Task.Run(() => 
            {
                Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                try
                {
                    Content = new ZipArchive(stream, ZipArchiveMode.Read, false,
                        System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ?
                        Encoding.GetEncoding(932) : Encoding.UTF8);
                    OnLoaded(new EventArgs());
                }
                catch { }
            }
            );

        }

        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            return GetContent(uri).AsAsyncOperation();
        }

        protected async Task<IInputStream> GetContent(Uri uri)
        {
            var invalid = new Exception("Invalid Path");
            if (uri == null) throw invalid;
            try
            {
                //Security!
                if (uri.LocalPath.ToLower().StartsWith("/reader/"))
                {
                    var pathTail = uri.LocalPath.Replace("/reader/", "", StringComparison.OrdinalIgnoreCase);
                    var f = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(Path.Combine("ms-appx:///res/reader/", pathTail)));
                    return await f.OpenAsync(Windows.Storage.FileAccessMode.Read);
                }
                if (uri.LocalPath.ToLower().StartsWith("/contents/"))
                {
                    var pathTail = uri.LocalPath.Replace("/contents/", "", StringComparison.OrdinalIgnoreCase);
                    if (Content == null) throw invalid;

                    var entry = Content.Entries.FirstOrDefault(a => a.FullName.ToLower() == pathTail.ToLower());
                    if (entry == null) throw invalid;


                    //The answer is here!
                    //We can't make AsRandomAccessStream with text and pass it to UriToStreamAsync method.
                    //https://stackoverflow.com/questions/59185615/how-to-make-a-custom-response-to-my-webview-with-a-iuritostreamresolver
                    //Why? Why? Why?
                    //What UriToStreamAsync is for?
                    //fuck


                    ////This works somohow. (well... Its not perfect, but thats not the point.)
                    //var item = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.Combine("epub", System.IO.Path.GetRandomFileName()));
                    //await Task.Run(() =>
                    //{
                    //    try
                    //    {
                    //        entry.ExtractToFile(item.Path, true);
                    //    }
                    //    catch { }
                    //});
                    //return await item.OpenReadAsync();

                    byte[] buf = new byte[32768];
                    var buffer = new Windows.Storage.Streams.Buffer(32768);

                    using (var s = entry.Open()){
                        using (var mss = new InMemoryRandomAccessStream())
                        {
                            while (true)
                            {
                                mss.Seek(0);
                                int read = s.Read(buf, 0, buf.Length);
                                if (read == buf.Length)
                                {
                                    await mss.WriteAsync(buf.AsBuffer());
                                }
                                else if (read > 0)
                                {
                                    await mss.WriteAsync(buf.Take(read).ToArray().AsBuffer());
                                }
                                else
                                {
                                    //{
                                    //    mss.Seek(0);
                                    //    var b= new Windows.Storage.Streams.Buffer(32768);
                                    //    await mss.ReadAsync(b, 32768, InputStreamOptions.None);
                                    //    var a = Encoding.UTF8.GetString(b.ToArray());
                                    //}


                                    mss.Seek(0);
                                    return mss;
                                }
                            }
                        }
                    }

                    //AsInputStream() or AsRandomAccessStream() just dont work for MemoryStream... At least for UriToStreamAsync...
                }
                throw invalid;
            }
            catch (Exception) { throw invalid; }
        }
    }
}
