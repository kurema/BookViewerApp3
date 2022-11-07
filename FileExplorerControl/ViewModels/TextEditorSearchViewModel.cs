using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace kurema.FileExplorerControl.ViewModels;
public class TextEditorSearchViewModel : BaseViewModel
{

    private string _WordSearch = string.Empty;
    public string WordSearch
    {
        get => _WordSearch; set
        {
            SetProperty(ref _WordSearch, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private string _WordReplace = string.Empty;
    public string WordReplace { get => _WordReplace; set => SetProperty(ref _WordReplace, value); }

    private int _PointStart = 0;
    public int PointStart { get => _PointStart; set => SetProperty(ref _PointStart, value); }

    private int _PointCurrent = 0;
    public int PointCurrent { get => _PointCurrent; set => SetProperty(ref _PointCurrent, value); }

    private bool _Replace = false;
    public bool Replace { get => _Replace; set => SetProperty(ref _Replace, value); }

    private bool _Repeat = true;
    public bool Repeat { get => _Repeat; set => SetProperty(ref _Repeat, value); }

    private bool _RegexError = false;
    public bool RegexError { get => _RegexError; set => SetProperty(ref _RegexError, value); }

    private bool _Regex = false;
    public bool Regex
    {
        get => _Regex; set
        {
            SetProperty(ref _Regex, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private bool _CaseSensitive = false;
    public bool CaseSensitive
    {
        get => _CaseSensitive; set
        {
            SetProperty(ref _CaseSensitive, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private StringComparison StringComparisonCurrentCulture => CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

    private Regex RegexCache;
    private Regex RegexCacheR2L;

    private bool ConstructRegexCache()
    {
        try
        {
            var option = CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            RegexCache = new Regex(WordSearch, option | RegexOptions.Compiled);
            RegexCacheR2L = new Regex(WordSearch, option | RegexOptions.Compiled | RegexOptions.RightToLeft);
            return true;
        }
        catch
        {
            RegexCache = null;
            RegexCacheR2L = null;
            RegexError = true;
            return false;
        }
    }

    private void ExecuteSearchEach(TextBox textBox, int index, int length, bool replace)
    {
        if (replace)
        {
            // args (index and length) is before replacement. So fix them.
            int lengthOriginal = textBox.SelectedText.Length;
            int lengthNew = lengthOriginal;
            bool goBack = textBox.SelectionStart > index;
            bool selectionSame = (textBox.SelectionStart == index) && (textBox.SelectionLength == length);

            // If selected text match the condition, replace.
            // Then proceed to next match.
            if (string.IsNullOrEmpty(textBox.SelectedText)) goto select;
            if (WordReplace is null) goto select;
            if (Regex)
            {
                if (RegexCache is null) goto select;
                if (!RegexCache.IsMatch(textBox.SelectedText)) goto select;
                textBox.SelectedText = RegexCache.Replace(textBox.SelectedText, WordReplace);
            }
            else
            {
                if (!textBox.SelectedText.Equals(WordSearch, StringComparisonCurrentCulture)) goto select;
                textBox.SelectedText = WordReplace;
            }
            lengthNew = textBox.SelectedText.Length;
            if (selectionSame) return;
        select:;
            textBox.Select(index + (goBack ? 0 : lengthNew - lengthOriginal), length);
        }
        else
        {
            textBox.Select(index, length);
        }
    }

    public void ExecuteSeach(TextBox textBox) => ExecuteSeach(textBox, Replace);

    public void ExecuteSeach(TextBox textBox, bool replace)
    {
        if (string.IsNullOrEmpty(WordSearch)) return;
        int startAt = Math.Max(textBox.SelectionStart, 0) + Math.Max(0, textBox.SelectionLength);
        if (Regex)
        {
            if (RegexCache is null && ConstructRegexCache() is false) return;
            if (startAt < textBox.Text.Length)
            {
                var match = RegexCache.Match(textBox.Text, startAt);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length, replace);
                    return;
                }
                if (!Repeat) return;// Should we play error sound here?
            }
            {
                var match = RegexCache.Match(textBox.Text);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length, replace);
                    return;
                }
            }
            return;
        }
        else
        {
            if (startAt < textBox.Text.Length)
            {
                int hit = textBox.Text.IndexOf(WordSearch, startAt, StringComparisonCurrentCulture);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length, replace);
                    return;
                }
                if (!Repeat) return;
            }
            {
                int hit = textBox.Text.IndexOf(WordSearch, StringComparisonCurrentCulture);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length, replace);
                    return;
                }
            }
            return;
        }
    }

    public void ReplaceAll(TextBox textBox)
    {
        if (Regex)
        {
            if (RegexCache is null && ConstructRegexCache() is false) return;
            textBox.Text = RegexCache.Replace(textBox.Text, WordReplace);
        }
        else
        {
            textBox.Text = textBox.Text.Replace(WordSearch, WordReplace, StringComparisonCurrentCulture);
        }
        textBox.Select(0, 0);
    }

    public void ExecuteSeachUp(TextBox textBox) => ExecuteSeachUp(textBox, Replace);

    public void ExecuteSeachUp(TextBox textBox, bool replace)
    {
        if (string.IsNullOrEmpty(WordSearch)) return;
        int startAt = Math.Max(textBox.SelectionStart, 0);
        if (Regex)
        {
            if (RegexCacheR2L is null && ConstructRegexCache() is false) return;
            if (startAt > 0)
            {
                var match = RegexCacheR2L.Match(textBox.Text, startAt - 1);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length, replace);
                    return;
                }
            }
            if (Repeat)
            {
                var match = RegexCacheR2L.Match(textBox.Text);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length, replace);
                    return;
                }
            }
            return;
        }
        else
        {
            if (startAt > 0)
            {
                int hit = textBox.Text.LastIndexOf(WordSearch, startAt - 1, StringComparisonCurrentCulture);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length, replace);
                    return;
                }
            }
            if (Repeat)
            {
                int hit = textBox.Text.LastIndexOf(WordSearch, StringComparisonCurrentCulture);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length, replace);
                    return;
                }
            }
            return;
        }
    }

}
