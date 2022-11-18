using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

namespace BookViewerApp.Storages.ExtensionAdBlockerItems;

partial class itemsGroup
{
    /// <summary>
    /// Get title for specific culture.
    /// </summary>
    /// <param name="culture">Target culture. If null, CurrentCulture.</param>
    /// <returns>Result. Can be null. Not string. Use .Value for string.</returns>
    public title GetTitleForCulture(CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        title defaultTitle = null;
        if (title is null) return null;
        foreach (var item in title)
        {
            if (culture != null && string.Equals(item.language, culture.TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase)) return item;
            if (item.@default) defaultTitle = item;
        }
        return defaultTitle;
    }
}

partial class item
{
    public bool IsValidEntry
    {
        get
        {
            if (string.IsNullOrWhiteSpace(title1) && title is null or { Length: 0 }) return false;
            if (source is null) return false;
            if (!Uri.TryCreate(source, UriKind.Absolute, out Uri uri)) return false;
            if (uri.Scheme.ToUpperInvariant() is not "HTTP" and not "HTTPS") return false;
            if (string.IsNullOrEmpty(filename)) return false;
            if (System.IO.Path.GetInvalidFileNameChars().Any(a => filename.Contains(a))) return false;
            return true;
        }
    }

    public string GetTitleForCulture(CultureInfo culture = null)
    {
        if (!string.IsNullOrEmpty(title1)) return title1;
        culture ??= CultureInfo.CurrentCulture;
        title defaultTitle = null;
        //VS says title can not be null but why?
        if (title is null) return null;
        foreach (var item in title)
        {
            if (culture != null && string.Equals(item.language, culture.TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase)) return item?.Value;
            if (item.@default) defaultTitle = item;
        }
        return defaultTitle?.Value;
    }
}