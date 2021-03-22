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
using System.Runtime.InteropServices.WindowsRuntime;

#nullable enable
namespace BookViewerApp.Books
{
    public class PdfBook : IBookFixed, IDirectionProvider, ITocProvider, IPasswordPdovider
    {
        public pdf.PdfDocument? Content { get; private set; }
        private bool PageLoaded = false;

        public string? Password { get; private set; } = null;

        public event EventHandler? Loaded;

        public uint PageCount => PageLoaded ? Content?.PageCount ?? 0 : 0;

        public string? ID
        {
            get; private set;
        }

        public Direction Direction { get; private set; } = Direction.Default;

        public TocItem[] Toc { get; private set; } = new TocItem[0];

        public bool PasswordRemember { get; private set; }

        public IPageFixed? GetPage(uint i)
        {
            if (Content is null) return null;
            if (i < PageCount) return new PdfPage(Content.GetPage(i));
            else throw new Exception("Incorrect page.");
        }

        public static iTextSharp.text.pdf.PdfReader? GetPdfReader(Stream stream, string? password)
        {
            try
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                if (password is null)
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

        public static TocItem[] GetTocs(Array? list, Hashtable nd, List<iTextSharp.text.pdf.PrIndirectReference> pageRefs)
        {
            int GetPage(iTextSharp.text.pdf.PdfIndirectReference pref)
            {
                return pageRefs.FindIndex(a =>
                {
                    return a.Number == pref.Number && a.Generation == pref.Generation;
                });
            }

            if (list is null) return new TocItem[0];

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
                            if (res.Length > 0 && int.TryParse(res[0], out int page))
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

        public async Task Load(IStorageFile file, Func<int, Task<(string password, bool remember)>> passwordRequestedCallback, string[]? defaultPassword = null)
        {
            using Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenReadAsync();
            await Load(stream, file.Name, passwordRequestedCallback, defaultPassword);
        }

        public async Task Load(Windows.Storage.Streams.IRandomAccessStream stream,string fileName, Func<int, Task<(string password, bool remember)>> passwordRequestedCallback, string[]? defaultPassword = null)
        {
            string? password = null;
            bool passSave = false;

            string? id = null;

            try
            {
                iTextSharp.text.pdf.PdfReader? pr = null;
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
                        passSave = true;
                        goto PasswordSuccess;
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    var result = await passwordRequestedCallback(i);
                    password = result.password;
                    passSave = result.remember;
                    pr = GetPdfReader(streamClassic, password);
                    if (pr != null) goto PasswordSuccess;
                }
                throw new iTextSharp.text.pdf.BadPasswordException("All passwords wrong");
            #endregion

            PasswordSuccess:;

                try
                {
                    //Get page direction information.

                    //It took some hours to write next line. Thanks for
                    //http://itext.2136553.n4.nabble.com/Using-getSimpleViewerPreferences-td2167775.html
                    //私が書いた記事はこちら。
                    //https://qiita.com/kurema/items/3f274507aa5cf9e845a8
                    var vp = iTextSharp.text.pdf.intern.PdfViewerPreferencesImp.GetViewerPreferences(pr.Catalog).GetViewerPreferences();

                    //L2R document often don't have Direction information.
                    //But it look like "L2R" in Acrobat Reader.
                    //Direction = Direction.Default
                    Direction = Direction.L2R;

                    if (vp.Contains(iTextSharp.text.pdf.PdfName.Direction))
                    {
                        var name = vp.GetAsName(iTextSharp.text.pdf.PdfName.Direction);

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

                try
                {
                    //https://github.com/VahidN/iTextSharp.LGPLv2.Core/blob/73605fa82fb00e9e8670991c9e410c684731f9f8/src/iTextSharp.LGPLv2.Core/iTextSharp/text/pdf/PdfReader.cs#L3366
                    //It seems to be...
                    //documentIDs[0] doesn't change when you edit.
                    //documentIDs[1] does. documentIDs[1] does not always exist.
                    var documentIDs = pr.Trailer.GetAsArray(iTextSharp.text.pdf.PdfName.Id);
                    if (documentIDs != null && documentIDs.Size > 0)
                    {
                        id = Functions.GetHash(documentIDs[0].GetBytes().AsBuffer());
                    }
                }
                catch
                {
                }

                if (id is null)
                {
                    try
                    {
                        //When PDF ID is unavailable, Hash(Hash([first 2048 bytes of the file])+"\nSize:"+file size) is used.
                        //Problems are
                        //・Some books may have the same first 2048 bytes.
                        //・You may edit PDF file with same file size.
                        //But it's much better than just fileName+pageCount.

                        uint length = 2048;
                        var buffer = new Windows.Storage.Streams.Buffer(length);
                        stream.Seek(0);
                        await stream.ReadAsync(buffer, length, Windows.Storage.Streams.InputStreamOptions.Partial);
                        id = Functions.GetHash(Functions.GetHash(buffer) + "\nSize:" + stream.Size);

                        stream.Seek(0);
                    }
                    catch
                    {
                        id = null;
                    }
                }

                var bm = iTextSharp.text.pdf.SimpleBookmark.GetBookmark(pr)?.ToArray();
                var nd = pr.GetNamedDestination(false);

                //Pdf's page start from 1 somehow.
                var pageRefs = new List<iTextSharp.text.pdf.PrIndirectReference>();
                for (int i = 1; i <= pr.NumberOfPages; i++)
                {
                    var oref = pr.GetPageOrigRef(i);
                    pageRefs.Add(oref);
                }

                Toc = GetTocs(bm, nd, pageRefs);
                pr.Close();
            }
            catch { }

            if (password is null)
            {
                Content = await pdf.PdfDocument.LoadFromStreamAsync(stream);
            }
            else
            {
                Content = await pdf.PdfDocument.LoadFromStreamAsync(stream, password);
                Password = password;
                PasswordRemember = passSave;
            }
            OnLoaded(new EventArgs());
            PageLoaded = true;
            ID = id ?? Functions.GetHash(Functions.CombineStringAndDouble(fileName, Content.PageCount));
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

        //public IPageOptions? Option
        //{
        //    get; set;
        //}
        //public IPageOptions? LastOption;

        private pdf.PdfPageRenderOptions? LastPdfOption;

        public PdfPage(pdf.PdfPage page)
        {
            Content = page;
        }

        protected double RenderScaleDefault => ((bool)Storages.SettingStorage.GetValue("PdfRenderScaling")) ? 2.0 : 1.0;
        protected double RenderScaleMinimum => ((bool)Storages.SettingStorage.GetValue("PdfRenderScaling")) ? 1.3 : 0.95;

        public async Task RenderToStreamAsync(Windows.Storage.Streams.IRandomAccessStream stream, double width, double height)
        {
            if (width == 0 || height == 0)
            {
                try
                {
                    var bound = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
                    var scaleFactor = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                    width = bound.Width * scaleFactor;
                    height = bound.Height * scaleFactor;
                }
                catch
                {
                    await Content.RenderToStreamAsync(stream);
                    return;
                }
            }

            var pdfOption = new pdf.PdfPageRenderOptions();
            if (height / Content.Size.Height < width / Content.Size.Width)
            {
                pdfOption.DestinationHeight = (uint)(height * RenderScaleDefault);
            }
            else
            {
                pdfOption.DestinationWidth = (uint)(width * RenderScaleDefault);
            }
            LastPdfOption = pdfOption;
            await Content.RenderToStreamAsync(stream, pdfOption);


            //if (Option != null)
            //{
            //    //Strange code. Maybe fix needed.
            //    if (Option is PageOptionsControl)
            //    {
            //        LastOption = (PageOptions)(PageOptionsControl)Option;
            //    }
            //    else { LastOption = Option; }

            //    var pdfOption = new pdf.PdfPageRenderOptions();
            //    if (Option.TargetHeight / Content.Size.Height < Option.TargetWidth / Content.Size.Width)
            //    {
            //        pdfOption.DestinationHeight = (uint)Option.TargetHeight * 2;
            //    }
            //    else
            //    {
            //        pdfOption.DestinationWidth = (uint)Option.TargetWidth * 2;
            //    }
            //    await Content.RenderToStreamAsync(stream, pdfOption);
            //}
            //else
            //{
            //    await Content.RenderToStreamAsync(stream);
            //}
        }

        public async Task PreparePageAsync()
        {
            await Content.PreparePageAsync();
        }

        //public async Task<BitmapImage> GetBitmapAsync()
        //{
        //    var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        //    await RenderToStreamAsync(stream);
        //    var result = new BitmapImage();
        //    await result.SetSourceAsync(stream);
        //    return result;
        //}

        public Task<bool> UpdateRequiredAsync(double width, double height)
        {
            //if (LastOption != null && Option != null && (LastOption.TargetHeight * 1.3 < Option.TargetHeight || LastOption.TargetWidth * 1.3 < Option.TargetWidth))
            //if (LastOption != null && Option != null)
            if (LastPdfOption == null || (LastPdfOption.DestinationHeight < height * RenderScaleMinimum && LastPdfOption.DestinationWidth < width * RenderScaleMinimum))
            { return Task.FromResult(true); }
            else { return Task.FromResult(false); }
        }

        public async Task SaveImageAsync(StorageFile file, uint width, Windows.Foundation.Rect? croppedRegionRelative = null)
        {
            var pdfOption = new pdf.PdfPageRenderOptions
            {
                DestinationWidth = width
            };
            if(croppedRegionRelative.HasValue)
            {
                pdfOption.SourceRect = croppedRegionRelative.Value;
            }
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await Content.RenderToStreamAsync(stream, pdfOption);
            await Functions.SaveStreamToFile(stream, file);
        }

        public async Task SetBitmapAsync(BitmapSource image, double width, double height)
        {
            var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await RenderToStreamAsync(stream, width, height);
            await image?.SetSourceAsync(stream);
        }
    }
}
