using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Zipangu;

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


    public bool IsJapanese
    {
        get { return (bool)GetValue(IsJapaneseProperty); }
        set { SetValue(IsJapaneseProperty, value); }
    }

    public static readonly DependencyProperty IsJapaneseProperty = DependencyProperty.Register(nameof(IsJapanese), typeof(bool), typeof(TextEditorPage), new PropertyMetadata(false));

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
}
