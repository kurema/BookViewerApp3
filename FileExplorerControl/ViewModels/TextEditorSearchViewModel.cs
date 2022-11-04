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

    private string _WordSearch;
    public string WordSearch
    {
        get => _WordSearch; set
        {
            SetProperty(ref _WordSearch, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private string _WordReplace;
    public string WordReplace { get => _WordReplace; set => SetProperty(ref _WordReplace, value); }

    private int _PointStart = 0;
    public int PointStart { get => _PointStart; set => SetProperty(ref _PointStart, value); }

    private int _PointCurrent = 0;
    public int PointCurrent { get => _PointCurrent; set => SetProperty(ref _PointCurrent, value); }

    private bool _Replace = false;
    public bool Replace { get => _Replace; set => SetProperty(ref _Replace, value); }

    private bool _Repeat;
    public bool Repeat { get => _Repeat; set => SetProperty(ref _Repeat, value); }

    private bool _RegexError = false;
    public bool RegexError { get => _RegexError; set => SetProperty(ref _RegexError, value); }

    private bool _Regex;
    public bool Regex
    {
        get => _Regex; set
        {
            SetProperty(ref _Regex, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private bool _IgnoreCase;
    public bool IgnoreCase
    {
        get => _IgnoreCase; set
        {
            SetProperty(ref _IgnoreCase, value);
            RegexCache = null;
            RegexCacheR2L = null;
        }
    }

    private Regex RegexCache;
    private Regex RegexCacheR2L;

    private bool ConstructRegexCache()
    {
        try
        {
            var option = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
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

    private void ExecuteSearchEach(TextBox textBox, int index, int length)
    {
        if (Replace)
        {
            textBox.Select(index, length);

        }
        else
        {
            textBox.Select(index, length);
        }
    }

    public void ExecuteSeach(TextBox textBox)
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
                    ExecuteSearchEach(textBox, match.Index, match.Length);
                    return;
                }
                if (!Repeat) return;// Should we play error sound here?
            }
            {
                var match = RegexCache.Match(textBox.Text);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length);
                    return;
                }
            }
            return;
        }
        else
        {
            if (startAt < textBox.Text.Length)
            {
                int hit = textBox.Text.IndexOf(WordSearch, startAt);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length);
                    return;
                }
                if (!Repeat) return;
            }
            {
                int hit = textBox.Text.IndexOf(WordSearch, 0);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length);
                    return;
                }
            }
            return;
        }
    }

    public void ExecuteSeachUp(TextBox textBox)
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
                    ExecuteSearchEach(textBox, match.Index, match.Length);
                    return;
                }
                if (!Repeat) return;
            }

            {
                var match = RegexCacheR2L.Match(textBox.Text);
                if (match.Success)
                {
                    ExecuteSearchEach(textBox, match.Index, match.Length);
                    return;
                }
            }
            return;
        }
        else
        {
            if (startAt > 0)
            {
                int hit = textBox.Text.LastIndexOf(WordSearch, startAt - 1);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length);
                    return;
                }
                if (!Repeat) return;
            }
            {
                int hit = textBox.Text.LastIndexOf(WordSearch, 0);
                if (hit >= 0)
                {
                    ExecuteSearchEach(textBox, hit, WordSearch.Length);
                    return;
                }
            }
            return;
        }
    }

}
