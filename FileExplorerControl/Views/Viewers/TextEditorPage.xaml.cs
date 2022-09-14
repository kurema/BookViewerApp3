using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace kurema.FileExplorerControl.Views.Viewers;
/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class TextEditorPage : Page
{
    public bool CanChageSavePosition
    {
        get { return (bool)GetValue(CanChageSavePositionProperty); }
        set { SetValue(CanChageSavePositionProperty, value); }
    }

    public static readonly DependencyProperty CanChageSavePositionProperty = DependencyProperty.Register(nameof(CanChageSavePosition), typeof(bool), typeof(TextEditorPage), new PropertyMetadata(false));



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
}
