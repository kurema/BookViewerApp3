using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BookViewerApp.Views.TemplateSelectors;

public class LicenseControlTemplateSelector : DataTemplateSelector
{
    public DataTemplate TemplateLicense { get; set; }
    public DataTemplate TemplateTranslation { get; set; }
    public DataTemplate TemplatePerson { get; set; }

    public DataTemplate TemplatePackage { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        switch (item)
        {
            case Storages.Licenses.license _: return TemplateLicense;
            case Storages.Licenses.licensesTranslation _: return TemplateTranslation;
            case Storages.Licenses.person _: return TemplatePerson;
            case Storages.Licenses.package _: return TemplatePackage;
        }

        return base.SelectTemplateCore(item, container);
    }
}
