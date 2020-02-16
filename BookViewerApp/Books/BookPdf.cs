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

namespace BookViewerApp.Books.Pdf
{
    public class PdfBook : IBookFixed,IDirectionProvider,ITocProvider
    {
        public pdf.PdfDocument Content { get; private set; }
        private bool PageLoaded = false;

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
            get;private set;
        }

        public Direction Direction { get; private set; } = Direction.Default;

        public TocItem[] Toc { get; private set; }

        public IPageFixed GetPage(uint i)
        {
            if (i < PageCount) return new PdfPage(Content.GetPage(i));
            else throw new Exception("Incorrect page.");
        }

        public async Task Load(Windows.Storage.IStorageFile file)
        {
            using (var stream = await file.OpenReadAsync())
            {

                try
                {
                    var pr = new iTextSharp.text.pdf.PdfReader(stream.AsStream());
                    var bm = iTextSharp.text.pdf.SimpleBookmark.GetBookmark(pr)?.ToArray();
                    var nd = pr.GetNamedDestination(false);

                    try
                    {
                        //Get page direction information.

                        //It took some hour to write next line. Thanks for
                        //http://itext.2136553.n4.nabble.com/Using-getSimpleViewerPreferences-td2167775.html
                        var vp = iTextSharp.text.pdf.intern.PdfViewerPreferencesImp.GetViewerPreferences(pr.Catalog).GetViewerPreferences();
                        if (vp.Contains(iTextSharp.text.pdf.PdfName.DIRECTION))
                        {
                            var name = vp.GetAsName(iTextSharp.text.pdf.PdfName.DIRECTION);

                            if (name == iTextSharp.text.pdf.PdfName.R2L)
                            {
                                this.Direction = Direction.R2L;
                            }
                            else if (name == iTextSharp.text.pdf.PdfName.L2R)
                            {
                                this.Direction = Direction.L2R;
                            }
                            else
                            {
                                this.Direction = Direction.Default;
                            }
                        }
                        else
                        {
                            this.Direction = Direction.Default;
                        }
                    }
                    catch { }

                    var pageRefs = new List<iTextSharp.text.pdf.PRIndirectReference>();
                    for (int i = 1; i <= pr.NumberOfPages; i++)
                    {
                        var oref = pr.GetPageOrigRef(i);
                        pageRefs.Add(oref);
                        if (oref == null)
                        {

                        }
                    }

                    int GetPage(iTextSharp.text.pdf.PdfIndirectReference pref)
                    {
                        return pageRefs.FindIndex(a =>
                        {
                            return a.Number == pref.Number && a.Generation == pref.Generation;
                        });
                    }


                    TocItem[] GetTocs(Array list)
                    {
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

                                        if (nditem.Length>0 && nditem[0] is iTextSharp.text.pdf.PdfIndirectReference pir)
                                        {
                                            //This pir thing is not page number. This is reference to the /Page.
                                            //https://stackoverflow.com/questions/30855432/how-to-get-the-page-number-of-an-arbitrary-pdf-object
                                            tocItem.Page = GetPage(pir);
                                        }
                                    }else if (nd.ContainsKey(named) && nd[named] is iTextSharp.text.pdf.PdfDictionary ndDict)
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

                                            //There may be the case where this is PdfIndirectReference... But I dont have one.
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    else if (itemd["Page"] is iTextSharp.text.pdf.PdfArray nditem)
                                    {
                                        //Does this really happen?
                                        if (nditem.Length > 0 && nditem[0] is iTextSharp.text.pdf.PdfIndirectReference pir)
                                        {
                                            tocItem.Page = GetPage(pir);
                                        }
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
                                    tocItem.Children = GetTocs(kids.ToArray());
                                }
                                result.Add(tocItem);
                            }
                        }
                        return result.ToArray();
                    }

                    this.Toc = GetTocs(bm);
                    pr.Close();
                }
                catch { }

                Content = await pdf.PdfDocument.LoadFromStreamAsync(stream);
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
                    LastOption = (PageOptions)((PageOptionsControl)this.Option);
                }
                else { LastOption = Option; }

                var pdfOption = new pdf.PdfPageRenderOptions();
                if (Option.TargetHeight/Content.Size.Height < Option.TargetWidth/Content.Size.Width)
                {
                    pdfOption.DestinationHeight = (uint)Option.TargetHeight*2;
                }
                else {
                    pdfOption.DestinationWidth = (uint)Option.TargetWidth*2;
                }
                await Content.RenderToStreamAsync(stream,pdfOption);
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

        public async Task<Windows.UI.Xaml.Media.Imaging.BitmapImage> GetBitmapAsync()
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream);
            var result = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
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

        public async Task SaveImageAsync(StorageFile file,uint width)
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
