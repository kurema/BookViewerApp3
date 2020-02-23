using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using pdf = Windows.Data.Pdf;

using System.IO;
using System.Collections;

using BookViewerApp.Helper;

namespace BookViewerApp.Books
{
    public class PdfBook : IBookFixed, IDirectionProvider, ITocProvider, IPasswordPdovider
    {
        public pdf.PdfDocument Content { get; private set; }
        private bool PageLoaded = false;

        public string Password { get; private set; } = null;

        public event EventHandler Loaded;

        public uint PageCount
        {
            get
            {
                if (PageLoaded) return Content.PageCount;
                else return 0;
            }
        }

        public string ID
        {
            get; private set;
        }

        public Direction Direction { get; private set; } = Direction.Default;

        public TocItem[] Toc { get; private set; }

        public IPageFixed GetPage(uint i)
        {
            if (i < PageCount) return new PdfPage(Content.GetPage(i));
            else throw new Exception("Incorrect page.");
        }

        public static iTextSharp.text.pdf.PdfReader GetPdfReader(Stream stream, string password)
        {
            try
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                if (password == null)
                {
                    return new iTextSharp.text.pdf.PdfReader(stream);
                }
                else
                {
                    //Encoding of password depends on the implementation if it's not ISO 32000-2.
                    //https://stackoverflow.com/questions/57328735/use-itext-7-to-open-a-password-protected-pdf-file-with-non-ascii-characters
                    //It's not correct... but UTF8 should be fine for many cases.
                    return new iTextSharp.text.pdf.PdfReader(stream, Encoding.UTF8.GetBytes(password));
                }
            }
            catch (iTextSharp.text.pdf.BadPasswordException)
            {
                return null;
            }
        }

        public static TocItem[] GetTocs(Array list, Hashtable nd, List<iTextSharp.text.pdf.PRIndirectReference> pageRefs)
        {
            int GetPage(iTextSharp.text.pdf.PdfIndirectReference pref)
            {
                return pageRefs.FindIndex(a =>
                {
                    return a.Number == pref.Number && a.Generation == pref.Generation;
                });
            }

            if (list == null) return new TocItem[0];

            var result = new List<TocItem>();
            foreach (var item in list)
            {
                TocItem tocItem = new TocItem();
                if (item is Hashtable itemd)
                {
                    if (itemd.ContainsKey("Named") && itemd["Named"] is string named)
                    {
                        if (nd.ContainsKey(named) && nd[named] is iTextSharp.text.pdf.PdfArray nditem)
                        {
                            //Note
                            //http://www.pdf-tools.trustss.co.jp/Syntax/catalog.html#destinations

                            if (nditem.Length > 0 && nditem[0] is iTextSharp.text.pdf.PdfIndirectReference pir)
                            {
                                //This pir thing is not page number. This is reference to the /Page.
                                //https://stackoverflow.com/questions/30855432/how-to-get-the-page-number-of-an-arbitrary-pdf-object
                                tocItem.Page = GetPage(pir);
                            }
                        }
                        else if (nd.ContainsKey(named) && nd[named] is iTextSharp.text.pdf.PdfDictionary ndDict)
                        {
                            //I dont have PDF file to test this...
                            continue;
                        }
                    }
                    else if (itemd.ContainsKey("Page"))
                    {
                        if (itemd["Page"] is string s)
                        {
                            var res = s.Split(' ');
                            int page = 0;
                            if (res.Length > 0 && int.TryParse(res[0], out page))
                            {
                                tocItem.Page = page - 1;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (itemd["Page"] is iTextSharp.text.pdf.PdfArray nditem)
                        {
                            //There may be the case where this is PdfIndirectReference... But I dont have one.
                            //Does this really happen?
                            if (nditem.Length > 0 && nditem[0] is iTextSharp.text.pdf.PdfIndirectReference pir)
                            {
                                tocItem.Page = GetPage(pir);
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else { continue; }
                    if (itemd.ContainsKey("Title") && itemd["Title"] is string title)
                    {
                        tocItem.Title = title;
                    }
                    else { continue; }
                    if (itemd.ContainsKey("Kids") && itemd["Kids"] is ArrayList kids)
                    {
                        tocItem.Children = GetTocs(kids.ToArray(), nd, pageRefs);
                    }
                    result.Add(tocItem);
                }
            }
            return result.ToArray();
        }


        public async Task Load(IStorageFile file, Func<int, Task<string>> passwordRequestedCallback, string[] defaultPassword = null)
        {
            using (var stream = await file.OpenReadAsync())
            {
                string password = null;

                try
                {
                    iTextSharp.text.pdf.PdfReader pr = null;
                    var streamClassic = stream.AsStream();

                    #region Password

                    pr = GetPdfReader(streamClassic, null);
                    if (pr != null) goto PasswordSuccess;

                    foreach (var item in defaultPassword ?? new string[0])
                    {
                        pr = GetPdfReader(streamClassic, item);
                        if (pr != null)
                        {
                            password = item;
                            goto PasswordSuccess;
                        }
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        password = await passwordRequestedCallback(i);
                        pr = GetPdfReader(streamClassic, password);
                        if (pr != null) goto PasswordSuccess;
                    }
                    throw new iTextSharp.text.pdf.BadPasswordException("All passwords wrong");
                #endregion

                PasswordSuccess:;

                    try
                    {
                        //Get page direction information.

                        //It took some hour to write next line. Thanks for
                        //http://itext.2136553.n4.nabble.com/Using-getSimpleViewerPreferences-td2167775.html
                        //私が書いた記事はこちら。
                        //https://qiita.com/kurema/items/3f274507aa5cf9e845a8
                        var vp = iTextSharp.text.pdf.intern.PdfViewerPreferencesImp.GetViewerPreferences(pr.Catalog).GetViewerPreferences();

                        Direction = Direction.Default;

                        if (vp.Contains(iTextSharp.text.pdf.PdfName.DIRECTION))
                        {
                            var name = vp.GetAsName(iTextSharp.text.pdf.PdfName.DIRECTION);

                            if (name == iTextSharp.text.pdf.PdfName.R2L)
                            {
                                Direction = Direction.R2L;
                            }
                            else if (name == iTextSharp.text.pdf.PdfName.L2R)
                            {
                                Direction = Direction.L2R;
                            }
                        }
                    }
                    catch { }

                    var bm = iTextSharp.text.pdf.SimpleBookmark.GetBookmark(pr)?.ToArray();
                    var nd = pr.GetNamedDestination(false);

                    //Pdf's page start from 1 somehow.
                    var pageRefs = new List<iTextSharp.text.pdf.PRIndirectReference>();
                    for (int i = 1; i <= pr.NumberOfPages; i++)
                    {
                        var oref = pr.GetPageOrigRef(i);
                        pageRefs.Add(oref);
                    }

                    Toc = GetTocs(bm, nd, pageRefs);
                    pr.Close();
                }
                catch { }

                if (password == null)
                {
                    Content = await pdf.PdfDocument.LoadFromStreamAsync(stream);
                }
                else
                {
                    Content = await pdf.PdfDocument.LoadFromStreamAsync(stream, password);
                    Password = password;
                }
                OnLoaded(new EventArgs());
                PageLoaded = true;
                ID = Functions.CombineStringAndDouble(file.Name, Content.PageCount);
            }
        }

        private void OnLoaded(EventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

    }
}

namespace BookViewerApp.Books
{
    public class PdfPage : IPageFixed
    {
        public pdf.PdfPage Content { get; private set; }

        public IPageOptions Option
        {
            get; set;
        }
        public IPageOptions LastOption;

        public PdfPage(pdf.PdfPage page)
        {
            Content = page;
        }

        public async Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            if (Option != null)
            {
                //Strange code. Maybe fix needed.
                if (Option is PageOptionsControl)
                {
                    LastOption = (PageOptions)(PageOptionsControl)Option;
                }
                else { LastOption = Option; }

                var pdfOption = new pdf.PdfPageRenderOptions();
                if (Option.TargetHeight / Content.Size.Height < Option.TargetWidth / Content.Size.Width)
                {
                    pdfOption.DestinationHeight = (uint)Option.TargetHeight * 2;
                }
                else
                {
                    pdfOption.DestinationWidth = (uint)Option.TargetWidth * 2;
                }
                await Content.RenderToStreamAsync(stream, pdfOption);
            }
            else
            {
                await Content.RenderToStreamAsync(stream);
            }
        }

        public async Task PreparePageAsync()
        {
            await Content.PreparePageAsync();
        }

        public async Task<BitmapImage> GetBitmapAsync()
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            var result = new BitmapImage();
            await result.SetSourceAsync(stream);
            return result;
        }

        public async Task<bool> UpdateRequiredAsync()
        {
            if (LastOption != null && Option != null && LastOption.TargetHeight * 1.3 < Option.TargetHeight || LastOption.TargetWidth * 1.3 < Option.TargetWidth)
            //if (LastOption != null && Option != null)
            { return true; }
            else { return false; }
        }

        public async Task SaveImageAsync(StorageFile file, uint width)
        {
            var pdfOption = new pdf.PdfPageRenderOptions();
            pdfOption.DestinationWidth = width;
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await Content.RenderToStreamAsync(stream, pdfOption);
            await Functions.SaveStreamToFile(stream, file);
        }

        public async Task SetBitmapAsync(BitmapImage image)
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            await image.SetSourceAsync(stream);
        }
    }
}
