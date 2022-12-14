using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace kurema.BrowserControl.ViewModels;

#nullable enable
public interface ISearchEngineEntry : INotifyPropertyChanged
{
    string Title { get; }
    string Description { get; }
    string Word { get; set; }

    Task Open(Func<string, Task> openUrlCallback);
    Task<IEnumerable<ISearchEngineEntry>> GetCandidates();
}

public class SearchEngineEntryDelegate : ISearchEngineEntry
{

    #region INotifyPropertyChanged
    protected bool SetProperty<T>(ref T backingStore, T value,
        [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "",
        System.Action? onChanged = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
    #endregion

    Func<string, Func<string, Task>, Task> action;
    Func<string, Task<IEnumerable<ISearchEngineEntry>>>? funcCandidates;

    public SearchEngineEntryDelegate(string title, Func<string, Func<string, Task>, Task> action, Func<string, Task<IEnumerable<ISearchEngineEntry>>> funcCandidates = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        this.funcCandidates = funcCandidates;
    }


    private string _Title = string.Empty;
    public string Title { get => string.Format(_Title ?? string.Empty, Word ?? string.Empty); set => SetProperty(ref _Title, value); }


    private string _Description = string.Empty;
    public string Description { get => string.Format(_Description ?? string.Empty, Word ?? string.Empty); set => SetProperty(ref _Description, value); }

    private string _Word = string.Empty;
    public string Word
    {
        get => _Word;
        set
        {
            SetProperty(ref _Word, value);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
        }
    }

    public async Task<IEnumerable<ISearchEngineEntry>> GetCandidates()
    {
        if (funcCandidates is null) return Array.Empty<ISearchEngineEntry>();
        return await funcCandidates.Invoke(Word) ?? Array.Empty<ISearchEngineEntry>();
    }

    public async Task Open(Func<string, Task> openUrlCallback)
    {
        if (action is null) return;
        await action.Invoke(Word, openUrlCallback);
    }
}