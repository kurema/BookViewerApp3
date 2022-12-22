using kurema.BrowserControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace kurema.BrowserControl.Views.TemplateSelectors;
public class SearchEngineEntrySelector : DataTemplateSelector
{
    //None, SearchEngine, Complition, URL,
    public DataTemplate TemplateNone { get; set; }
    public DataTemplate TemplateSearchEngine { get; set; }
    public DataTemplate TemplateComplition { get; set; }
    public DataTemplate TemplateURL { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not ISearchEngineEntry entry) return base.SelectTemplateCore(item, container);
        return entry.EntryGenre switch
        {
            ISearchEngineEntry.Genre.SearchEngine => TemplateSearchEngine,
            ISearchEngineEntry.Genre.Complition => TemplateComplition,
            ISearchEngineEntry.Genre.URL => TemplateURL,
            ISearchEngineEntry.Genre.None => TemplateNone,
            _ => base.SelectTemplateCore(item, container),
        };
    }

}
