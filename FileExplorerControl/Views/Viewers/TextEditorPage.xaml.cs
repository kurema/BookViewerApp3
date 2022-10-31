using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Zipangu;
using static kurema.FileExplorerControl.Views.Viewers.TextEditorPage;
using static System.Net.Mime.MediaTypeNames;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace kurema.FileExplorerControl.Views.Viewers;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class TextEditorPage : Page
{
    public bool CanChageSavePath
    {
        get { return (bool)GetValue(CanChageSavePathProperty); }
        set { SetValue(CanChageSavePathProperty, value); }
    }

    public static readonly DependencyProperty CanChageSavePathProperty = DependencyProperty.Register(nameof(CanChageSavePath), typeof(bool), typeof(TextEditorPage), new PropertyMetadata(false));


    public bool CanOverwrite
    {
        get { return (bool)GetValue(CanOverwriteProperty); }
        set { SetValue(CanOverwriteProperty, value); }
    }

    public static readonly DependencyProperty CanOverwriteProperty = DependencyProperty.Register("CanOverwrite", typeof(bool), typeof(TextEditorPage), new PropertyMetadata(true));

    public bool IsSpellCheckEnabled
    {
        get { return (bool)GetValue(IsSpellCheckEnabledProperty); }
        set { SetValue(IsSpellCheckEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsSpellCheckEnabledProperty = DependencyProperty.Register("IsSpellCheckEnabled", typeof(bool), typeof(TextEditorPage), new PropertyMetadata(true));



    public bool SelectionNotEmpty
    {
        get { return (bool)GetValue(SelectionNotEmptyProperty); }
        set { SetValue(SelectionNotEmptyProperty, value); }
    }

    public static readonly DependencyProperty SelectionNotEmptyProperty = DependencyProperty.Register("SelectionNotEmpty", typeof(bool), typeof(TextEditorPage), new PropertyMetadata(false));



    public bool IsJapanese
    {
        get { return (bool)GetValue(IsJapaneseProperty); }
        set { SetValue(IsJapaneseProperty, value); }
    }

    public static readonly DependencyProperty IsJapaneseProperty = DependencyProperty.Register(nameof(IsJapanese), typeof(bool), typeof(TextEditorPage), new PropertyMetadata(false));

    public bool IsToUpperLowerStarange
    {
        get
        {
            var abcChar = new char[26];
            for (byte i = 0; i < 26; i++)
            {
                abcChar[i] = (char)('a' + i);
            }
            var abc = new string(abcChar);
            var ABC = abc.ToUpperInvariant();
            return (abc.ToUpper() != abc.ToUpperInvariant()) || (ABC.ToLower() != ABC.ToLowerInvariant());
        }
    }

    public Models.FileItems.IFileItem File { get; set; }

    public System.Text.Encoding Encoding { get; set; } = null;

    public Action<Windows.Storage.Pickers.FileSavePicker> DefaultSetupDialog { get; set; } = null;

    public async Task<bool> Load()
    {
        if (File is null) return false;
        using var stream = await File.OpenStreamForReadAsync();
        if (stream is null) return false;
        using var sr = new StreamReader(stream, Encoding ?? System.Text.Encoding.UTF8);
        var text = await sr.ReadToEndAsync();
        if (text is null) return false;
        MainTextBox.Text = text;
        return true;
    }

    public async Task<bool> LoadFile(IStorageFile file)
    {
        //x:bind cannot use overload.
        if (file is null) return false;
        var fileitem = new Models.FileItems.StorageFileItem(file);
        File = fileitem;
        return await Load();
    }

    public async Task Save()
    {
        await SaveGeneral(File, Encoding, SaveMode.Save);
    }

    public async Task<bool> SaveGeneral(Models.FileItems.IFileItem file, System.Text.Encoding encoding, SaveMode saveMode)
    {
        if (file is null) return false;
        var ea = new SavingFileEventArgs(file, saveMode);
        FileSaving?.Invoke(this, ea);
        if (!ea.ContinueSaving)
        {
            if (ea.ErrorMessage is not null)
            {
                var dialog = ea.ErrorTitle is null ? new MessageDialog(ea.ErrorMessage) : new MessageDialog(ea.ErrorMessage, ea.ErrorTitle);
                await dialog.ShowAsync();
            }
            return false;
        }
        try
        {
            {
                using var stream = await file.OpenStreamForWriteAsync();
                if (stream is null) return false;
                var text = MainTextBox.Text;
                //You need .SetLength(0);
                //https://www.moonmile.net/blog/archives/3937
                stream.SetLength(0);
                using var sw = new StreamWriter(stream, encoding ?? System.Text.Encoding.UTF8);
                await sw.WriteAsync(text);
                sw.Close();
                stream.Close();
            }
            FileSaved?.Invoke(this, new EventArgs());
            return true;
        }
        catch {
            return false;
        }
    }

    public async Task Open()
    {
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".txt");
        var file = await openPicker.PickSingleFileAsync();
        if (file is null) return;
        File = new Models.FileItems.StorageFileItem(file);
        await Load();
    }

    public async Task SaveAsGeneral(Action<Windows.Storage.Pickers.FileSavePicker> setupDialog, SaveMode saveMode, bool saveCopy = false)
    {
        var savePicker = new Windows.Storage.Pickers.FileSavePicker();
        if (setupDialog is not null)
        {
            setupDialog(savePicker);
        }
        else
        {
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = File?.Name ?? "New Text";
        }
        var file = await savePicker.PickSaveFileAsync();
        if (file is null) return;
        if (saveCopy)
        {
            await SaveGeneral(new Models.FileItems.StorageFileItem(file), Encoding, saveMode);
        }
        else
        {
            File = new Models.FileItems.StorageFileItem(file);
            await Save();
        }
    }

    public async Task SaveAs()
    {
        await SaveAsGeneral(DefaultSetupDialog, SaveMode.SaveAs);
    }

    public async Task SaveAsCopy()
    {
        await SaveAsGeneral(DefaultSetupDialog, SaveMode.SaveAsCopy, true);
    }

    public TextEditorPage()
    {
        this.InitializeComponent();

        this.IsJapanese = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.InvariantCulture);
    }

    private async Task CutAndAppendToClipboard() => await AppendToClipboard(true);

    private async Task CopyAndAppendToClipboard() => await AppendToClipboard(false);

    private async Task AppendToClipboard(bool cut = false)
    {
        if (MainTextBox.SelectionStart < 0 || MainTextBox.SelectionLength <= 0) return;
        var text = MainTextBox.SelectedText;
        if (string.IsNullOrEmpty(text)) return;
        string clipText = string.Empty;
        var clip = Clipboard.GetContent();
        if (clip is not null && clip.AvailableFormats.Contains("Text")) clipText = (await clip.GetTextAsync() ?? string.Empty) + Environment.NewLine;
        var package = new DataPackage();
        package.SetText(clipText + text);
        Clipboard.SetContent(package);
        if (cut) MainTextBox.Text = MainTextBox.Text.Remove(MainTextBox.SelectionStart, MainTextBox.SelectionLength);
    }

    private void MenuFlyoutItem_Click_Convert(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fi) return;
        var (s, l) = (MainTextBox.SelectionStart, MainTextBox.SelectionLength);
        if (s < 0 || l <= 0) return;
        var text = MainTextBox.SelectedText;
        if (string.IsNullOrEmpty(text)) return;
        switch (fi.Tag.ToString())
        {
            case "UpperCase":
                text = text.ToUpper();
                break;
            case "LowerCase":
                text = text.ToLower();
                break;
            case "UpperCaseInvariant":
                text = text.ToUpperInvariant();
                break;
            case "LowerCaseInvariant":
                text = text.ToLowerInvariant();
                break;
            case "JpAllKanaToHiragana":
                text = text.Convert(KanaConv.AllKanaToHiragana);
                break;
            case "JpAllKanaToKatakana":
                text = text.Convert(KanaConv.AllKanaToKatakana);
                break;
            case "JpHiraganaToKatakana":
                text = text.Convert(KanaConv.HiraganaToKatakana);
                break;
            case "JpKatakanaToHiragana":
                text = text.Convert(KanaConv.KatakanaToHiragana);
                break;
            case "JpHalfKatakanaToHiragana":
                text = text.Convert(KanaConv.HalfKatakanaToHiragana);
                break;
            case "JpHalfKatakanaToKatakana":
                text = text.Convert(KanaConv.HalfKatakanaToKatakana);
                break;
            case "AsciiWide":
                text = text.Convert(ascii: AsciiConv.ToWide);
                break;
            case "AsciiNarrow":
                text = text.Convert(ascii: AsciiConv.ToNarrow);
                break;
            case "TabToSpaces":
                text = text.Replace("\t", "        ");
                break;
            case "SpacesToTab":
                text = text.Replace("        ", "\t");
                break;
            case "Indent":
                {
                    string header = string.Empty;
                    if (s == 0 || MainTextBox.Text[s - 1] is '\n' or '\r') header = "\t";
                    text = header + Regex.Replace(text, @"(\n|\r\n|\r)", "$1\t");
                    break;
                }
            case "Unindent":
                {
                    if (s == 0 || MainTextBox.Text[s - 1] is '\n' or '\r')
                    {
                        if (text.Length > 0 && text[0] is '\t' or ' ') text = text.Substring(1);
                    }
                    text = Regex.Replace(text, @"(\n|\r\n|\r)([\t\s])", "$1");
                    break;
                }
            case "ArrowWide":
                text = text.Replace("->", "→").Replace("=>", "⇒").Replace("<-", "←").Replace("<=", "⇐").Replace("<->", "↔").Replace("<=>", "⇔");
                break;
            case "ArrowNarrow":
                text = text.Replace("→", "->").Replace("⇒", "=>").Replace("←", "<-").Replace("⇐", "<=").Replace("↔", "<->").Replace("⇔", "<=>");
                break;
        }
        MainTextBox.Text = MainTextBox.Text.Remove(s, l).Insert(s, text);
        MainTextBox.Select(s, text.Length);
    }

    private void MainTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        SelectionNotEmpty = tb.SelectionLength > 0;
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        MainTextBox.Text = MainTextBox.Text.Remove(MainTextBox.SelectionStart, MainTextBox.SelectionLength);
    }

    private void MenuFlyoutItem_Click_ChangeFontSize(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (!double.TryParse(fe.Tag.ToString(), out double fontsize)) return;
        MainTextBox.FontSize = fontsize;

    }

    public delegate void GenericsEventHandler<T>(TextEditorPage sender, T args);
    public event GenericsEventHandler<SavingFileEventArgs> FileSaving;
    public event EventHandler FileSaved;

    public string Text { get => MainTextBox.Text; set => MainTextBox.Text = value; }

    public class SavingFileEventArgs : EventArgs
    {
        public SavingFileEventArgs(Models.FileItems.IFileItem file, SaveMode mode)
        {
            Mode = mode;
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public bool ContinueSaving { get; set; } = true;
        public void Cancel(string errorMesasge = null, string errorTitle = null)
        {
            ContinueSaving = false;
            ErrorMessage = errorMesasge;
            ErrorTitle = errorTitle;
        }

        public string ErrorMessage { get; set; } = null;
        public string ErrorTitle { get; set; } = null;

        public SaveMode Mode { get; }

        public Models.FileItems.IFileItem File { get; }
    }

    public enum SaveMode
    {
        Save, SaveAs, SaveAsCopy,
    }
}
