using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace kurema.BrowserControl.ViewModels;

public interface IBookmarkItem
{
    string Title { get; }
    bool IsFolder { get; }
    bool IsReadOnly { get; }
    string Address { get; }
    Task<IEnumerable<IBookmarkItem>> GetChilderenAsync();

    void AddItem(IBookmarkItem content);
}

public class BookmarkItemGoUp : IBookmarkItem
{
    IEnumerable<IBookmarkItem> contents;

    public BookmarkItemGoUp(IEnumerable<IBookmarkItem> contents)
    {
        this.contents = contents ?? throw new ArgumentNullException(nameof(contents));
    }

    public string Title => "..";

    public bool IsFolder => true;

    public bool IsReadOnly => true;

    public string Address => string.Empty;

    public void AddItem(IBookmarkItem content)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IBookmarkItem>> GetChilderenAsync() => Task.FromResult(contents);
}

public class BookmarkItem : IBookmarkItem
{
    public BookmarkItem(string title, Action<IBookmarkItem> addItemDelegate, Func<Task<IEnumerable<IBookmarkItem>>> getChilderenDelegate)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        IsFolder = true;
        AddItemDelegate = addItemDelegate;
        GetChilderenDelegate = getChilderenDelegate;
    }

    public BookmarkItem(string title, string address)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        IsFolder = false;
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public BookmarkItem()
    {
    }

    public string Title { get; set; }

    public bool IsFolder { get; set; }

    public string Address { get; set; }

    public Action<IBookmarkItem> AddItemDelegate { get; set; }

    public void AddItem(IBookmarkItem content)
    {
        AddItemDelegate?.Invoke(content);
    }

    public Func<Task<IEnumerable<IBookmarkItem>>> GetChilderenDelegate { get; set; }

    public bool IsReadOnly { get; set; }

    public Task<IEnumerable<IBookmarkItem>> GetChilderenAsync()
    {
        return GetChilderenDelegate?.Invoke() ?? Task.FromResult<IEnumerable<IBookmarkItem>>(null);
    }

    public static async Task<(IEnumerable<IBookmarkItem>, IEnumerable<IBookmarkItem>)> SearchBookmark(IBookmarkItem bookmark, string word)
    {
        //new[] { ' ', '　', '&', '＆' }
        if (bookmark is null) return (Array.Empty<IBookmarkItem>(), Array.Empty<IBookmarkItem>());
        var words = word.Split(new string[0], StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
        var list1 = new List<IBookmarkItem>();
        var list2 = new List<IBookmarkItem>();
        await SearchRecursion(bookmark, list1, list2, words);
        return (list1.ToArray(), list2.ToArray());

        static async Task SearchRecursion(IBookmarkItem bookmark, IList<IBookmarkItem> hitAllList, IList<IBookmarkItem> hitSingleList, string[] words)
        {
            if (bookmark.IsFolder)
            {
                var items = await bookmark.GetChilderenAsync();
                foreach (var item in items)
                {
                    await SearchRecursion(item, hitAllList, hitSingleList, words);
                }
                return;
            }
            if (words.Length == 0)
            {
                hitAllList.Add(bookmark);
                return;
            }
            bool hitAll = true;
            bool hitSingle = false;
            foreach (var word in words)
            {
                var hit = bookmark.Title.Contains(word, StringComparison.CurrentCultureIgnoreCase) || bookmark.Title.Contains(word, StringComparison.CurrentCultureIgnoreCase);
                hitAll &= hit;
                hitSingle |= hit;
            }
            if (hitAll)
            {
                hitAllList.Add(bookmark);
                return;
            }
            if (hitSingle)
            {
                hitSingleList.Add(bookmark);
                return;
            }
            return;
        }
    }
}
