using BookViewerApp.Storages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BookViewerApp.Views;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class BookmarksSettingPage : Page
{
    public BookmarksSettingPage()
    {
        this.InitializeComponent();

        async void LoadTextEditors()
        {
            await LibraryStorage.RoamingBookmarks.SaveAsync();
            await textEditorBookmarkXmlRoaming.LoadFile(await LibraryStorage.RoamingBookmarks.GetFileAsync());
            var f = await LibraryStorage.LocalBookmarks.GetFileAsync();
            textBoxSample.Text = await Windows.Storage.FileIO.ReadTextAsync(f);
        }

        LoadTextEditors();

        System.Threading.Tasks.Task.Run(async () =>
        {
            serializer = new System.Xml.Serialization.XmlSerializer(typeof(Storages.Library.bookmarks));

            try
            {
                schemaSet = new();
                //schemaSet.Add("https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Library.xsd",);
                //System.Xml.Schema.XmlSchema.Read()
                var f = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Storages/Library.xsd"));
                using var stream = await f.OpenStreamForReadAsync();
                schemaSet.Add(System.Xml.Schema.XmlSchema.Read(stream, (s, e) =>
                {
                    throw new Exception();
                }));

            }
            catch
            {
                schemaSet = null;
            }
        });
    }

    private async void textEditorBookmarkXmlRoaming_FileSaved(object sender, EventArgs e)
    {
        await LibraryStorage.RoamingBookmarks.ReloadAsync();
    }

    private System.Xml.Schema.XmlSchemaSet schemaSet;
    private System.Xml.Serialization.XmlSerializer serializer;

    private void textEditorBookmarkXml_FileSaving(kurema.FileExplorerControl.Views.Viewers.TextEditorPage sender, kurema.FileExplorerControl.Views.Viewers.TextEditorPage.SavingFileEventArgs args)
    {
        var loader = Managers.ResourceManager.Loader;
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sender.Text));

        StringBuilder sbError = new();
        bool xsdError = false;
        if (schemaSet is not null)
        {
            try
            {
                var xdoc = new XmlDocument();
                ms.Seek(0, SeekOrigin.Begin);
                xdoc.Schemas.Add(schemaSet);
                xdoc.Load(ms);
                xdoc.Validate((s, e) =>
                {
                    sbError.AppendLine(e.Message);
                    xsdError = true;
                });
            }
            catch (Exception e)
            {
                // When XML is invalid.
                args.Cancel(string.Format(loader.GetString("BookmarkManager/TextEditor/Error/Message"), e.Message), loader.GetString("BookmarkManager/TextEditor/Error/Title"));
                return;
            }
        }

        if (xsdError)
        {
            // When XSD validiate failed.
            args.Cancel(string.Format(loader.GetString("BookmarkManager/TextEditor/Error/Message"), sbError.ToString()), loader.GetString("BookmarkManager/TextEditor/Error/Title"));
            return;
        }

        if (serializer is not null)
        {
            try
            {
                ms.Seek(0, SeekOrigin.Begin);
                serializer.Deserialize(ms);
            }
            catch (Exception e)
            {
                // When serialize failed. Unlikely to come here.
                args.Cancel(string.Format(loader.GetString("BookmarkManager/TextEditor/Error/Message"), e.Message), loader.GetString("BookmarkManager/TextEditor/Error/Title"));
                return;
            }
        }
    }
}
